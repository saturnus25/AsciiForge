using System.ComponentModel;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace AsciiForge;

internal sealed class GlPreviewControl : Control
{
    private const int WM_ERASEBKGND = 0x0014;
    private const int CS_OWNDC = 0x0020;
    private const int WGL_CONTEXT_MAJOR_VERSION_ARB = 0x2091;
    private const int WGL_CONTEXT_MINOR_VERSION_ARB = 0x2092;
    private const int WGL_CONTEXT_PROFILE_MASK_ARB = 0x9126;
    private const int WGL_CONTEXT_CORE_PROFILE_BIT_ARB = 0x00000001;
    private const uint GL_RENDERER = 0x1F01;
    private const uint GL_VERSION = 0x1F02;

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate IntPtr WglCreateContextAttribs(IntPtr hdc, IntPtr share, int[] attribs);
    [DllImport("opengl32.dll")] private static extern IntPtr wglGetProcAddress(string name);

    private IntPtr _dc;
    private IntPtr _rc;
    private uint _previewProgram;
    private uint _captureProgram;
    private uint _vao;
    private uint _atlas;
    private uint _intensityTex;
    private uint _intensityFbo;
    private int _intensityW;
    private int _intensityH;
    private int _atlasCols = 1, _atlasRows = 1;
    private string _atlasRamp = "";
    private readonly Stopwatch _clock = Stopwatch.StartNew();
    private readonly Stopwatch _fpsClock = Stopwatch.StartNew();
    private int _frames;
    private double _actualFps;
    private System.Threading.CancellationTokenSource? _schedulerCts;
    private System.Threading.Thread? _schedulerThread;
    private int _paintQueued;
    private volatile bool _loaded;
    private string _gpuInfo = "OpenGL no inicializado";
    private EffectSettings _settings = new();
    private int _targetFps = 30;
    private volatile bool _paused;
    private double _pauseAt;
    private double _pausedAccum;
    // Preview clocks are integrated instead of recomputed as elapsed*slider.
    // This prevents live speed/time-frequency edits from teleporting stateful-looking effects.
    private double _animationTime;
    private double _temporalTime;
    private double _lastTimelineTime;
    private bool _previewClockInitialized;

    public event Action<double, double, string>? FrameStats;
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public EffectSettings Settings
    {
        get => _settings;
        set
        {
            _settings = value;
            SyncAtlasIfNeeded();
            // While running, the precision scheduler owns repaint cadence.
            // This prevents slider ValueChanged events from bypassing TargetFps.
            if (_paused) Invalidate();
        }
    }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int TargetFps { get => System.Threading.Volatile.Read(ref _targetFps); set => System.Threading.Volatile.Write(ref _targetFps, Math.Max(1, value)); }
    public string GpuInfo => _gpuInfo;
    public double CurrentTimeSeconds => _paused ? _pauseAt : _clock.Elapsed.TotalSeconds - _pausedAccum;
    public bool Paused => _paused;
    public void TogglePause() { if (!_paused) { _pauseAt = _clock.Elapsed.TotalSeconds - _pausedAccum; _paused = true; } else { _pausedAccum = _clock.Elapsed.TotalSeconds - _pauseAt; _paused = false; } Invalidate(); }
    public void RestartAnimation()
    {
        _clock.Restart(); _paused = false; _pauseAt = 0; _pausedAccum = 0;
        _animationTime = 0; _temporalTime = 0; _lastTimelineTime = 0; _previewClockInitialized = false;
        Invalidate();
    }

    public GlPreviewControl()
    {
        SetStyle(ControlStyles.Opaque | ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint, true);
        BackColor = Color.Black;
    }

    protected override CreateParams CreateParams
    {
        get { var cp = base.CreateParams; cp.ClassStyle |= CS_OWNDC; return cp; }
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        try { InitializeOpenGl(); StartScheduler(); }
        catch (Exception ex) { _gpuInfo = "OpenGL error: " + ex.Message; Invalidate(); }
    }

    protected override void OnHandleDestroyed(EventArgs e)
    {
        StopScheduler();
        if (_dc != IntPtr.Zero && _rc != IntPtr.Zero)
        {
            NativeGl.wglMakeCurrent(_dc, _rc);
            if (_previewProgram != 0) NativeGl.DeleteProgram(_previewProgram);
            if (_captureProgram != 0) NativeGl.DeleteProgram(_captureProgram);
            if (_atlas != 0) NativeGl.DeleteTextures(1, ref _atlas);
            if (_intensityFbo != 0) NativeGl.DeleteFramebuffers(1, ref _intensityFbo);
            if (_intensityTex != 0) NativeGl.DeleteTextures(1, ref _intensityTex);
            NativeGl.wglMakeCurrent(IntPtr.Zero, IntPtr.Zero);
            NativeGl.wglDeleteContext(_rc); _rc = IntPtr.Zero;
        }
        if (_dc != IntPtr.Zero) { NativeGl.ReleaseDC(Handle, _dc); _dc = IntPtr.Zero; }
        base.OnHandleDestroyed(e);
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == WM_ERASEBKGND) { m.Result = new IntPtr(1); return; }
        base.WndProc(ref m);
    }

    private void StartScheduler()
    {
        StopScheduler();
        _schedulerCts = new System.Threading.CancellationTokenSource();
        var token = _schedulerCts.Token;
        _schedulerThread = new System.Threading.Thread(() => SchedulerLoop(token))
        {
            IsBackground = true,
            Name = "AsciiForge frame scheduler",
            Priority = System.Threading.ThreadPriority.AboveNormal
        };
        _schedulerThread.Start();
    }

    private void StopScheduler()
    {
        var cts = _schedulerCts;
        _schedulerCts = null;
        if (cts is null) return;
        try { cts.Cancel(); } catch { }
        try
        {
            if (_schedulerThread is { IsAlive: true } thread && thread != System.Threading.Thread.CurrentThread)
                thread.Join(250);
        }
        catch { }
        _schedulerThread = null;
        cts.Dispose();
        System.Threading.Interlocked.Exchange(ref _paintQueued, 0);
    }

    private void SchedulerLoop(System.Threading.CancellationToken token)
    {
        long next = Stopwatch.GetTimestamp();
        while (!token.IsCancellationRequested)
        {
            if (!_loaded || _paused || !IsHandleCreated || IsDisposed)
            {
                System.Threading.Thread.Sleep(5);
                next = Stopwatch.GetTimestamp();
                continue;
            }

            int fps = Math.Max(1, System.Threading.Volatile.Read(ref _targetFps));
            long period = Math.Max(1L, Stopwatch.Frequency / fps);
            long now = Stopwatch.GetTimestamp();
            if (now >= next)
            {
                // Keep a stable cadence, but do not try to replay a backlog after a stall.
                next = now - next > period * 4 ? now + period : next + period;
                QueueFrame();
                continue;
            }

            double remainingMs = (next - now) * 1000.0 / Stopwatch.Frequency;
            if (remainingMs > 2.0)
                System.Threading.Thread.Sleep(Math.Max(1, (int)Math.Floor(remainingMs - 1.0)));
            else
                System.Threading.Thread.Yield();
        }
    }

    private void QueueFrame()
    {
        if (System.Threading.Interlocked.Exchange(ref _paintQueued, 1) != 0) return;
        try
        {
            BeginInvoke((Action)(() =>
            {
                if (!IsDisposed && IsHandleCreated) Invalidate();
                else System.Threading.Interlocked.Exchange(ref _paintQueued, 0);
            }));
        }
        catch (ObjectDisposedException) { System.Threading.Interlocked.Exchange(ref _paintQueued, 0); }
        catch (InvalidOperationException) { System.Threading.Interlocked.Exchange(ref _paintQueued, 0); }
    }

    private unsafe void InitializeOpenGl()
    {
        _dc = NativeGl.GetDC(Handle);
        if (_dc == IntPtr.Zero) throw new InvalidOperationException("GetDC falló");
        var pfd = NativeGl.DefaultPfd();
        int pf = NativeGl.ChoosePixelFormat(_dc, ref pfd);
        if (pf == 0 || !NativeGl.SetPixelFormat(_dc, pf, ref pfd)) throw new InvalidOperationException("No se pudo configurar el pixel format OpenGL");

        IntPtr temp = NativeGl.wglCreateContext(_dc);
        if (temp == IntPtr.Zero || !NativeGl.wglMakeCurrent(_dc, temp)) throw new InvalidOperationException("No se pudo crear el contexto OpenGL temporal");

        IntPtr createPtr = wglGetProcAddress("wglCreateContextAttribsARB");
        if (createPtr != IntPtr.Zero)
        {
            var create = Marshal.GetDelegateForFunctionPointer<WglCreateContextAttribs>(createPtr);
            int[] attrs = [WGL_CONTEXT_MAJOR_VERSION_ARB, 3, WGL_CONTEXT_MINOR_VERSION_ARB, 3, WGL_CONTEXT_PROFILE_MASK_ARB, WGL_CONTEXT_CORE_PROFILE_BIT_ARB, 0];
            IntPtr modern = create(_dc, IntPtr.Zero, attrs);
            if (modern != IntPtr.Zero)
            {
                NativeGl.wglMakeCurrent(IntPtr.Zero, IntPtr.Zero);
                NativeGl.wglDeleteContext(temp);
                temp = modern;
                NativeGl.wglMakeCurrent(_dc, temp);
            }
        }
        _rc = temp;
        NativeGl.Load();

        string vert = ReadResource("fullscreen.vert.glsl");
        string effects = ReadResource("effects.glsl");
        string preview = ReadResource("preview.frag.glsl");
        string intensity = ReadResource("intensity.frag.glsl").Replace("/*__EFFECTS__*/", effects);
        _previewProgram = NativeGl.CompileProgram(vert, preview);
        _captureProgram = NativeGl.CompileProgram(vert, intensity);
        uint vao;
        NativeGl.GenVertexArrays(1, &vao);
        _vao = vao;
        NativeGl.BindVertexArray(_vao);
        BuildGlyphAtlas(_settings.Charset);
        _gpuInfo = GetGlString(GL_RENDERER) + " · OpenGL " + GetGlString(GL_VERSION);
        _loaded = true;
    }

    private static string ReadResource(string suffix)
    {
        var asm = Assembly.GetExecutingAssembly();
        var name = asm.GetManifestResourceNames().First(n => n.EndsWith(suffix, StringComparison.OrdinalIgnoreCase));
        using var sr = new StreamReader(asm.GetManifestResourceStream(name)!);
        return sr.ReadToEnd();
    }

    private static string GetGlString(uint name)
    {
        var p = NativeGl.GetString(name);
        return p == IntPtr.Zero ? "?" : Marshal.PtrToStringAnsi(p) ?? "?";
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        System.Threading.Interlocked.Exchange(ref _paintQueued, 0);
        if (!_loaded)
        {
            e.Graphics.Clear(Color.Black);
            using var b = new SolidBrush(Color.Gainsboro);
            e.Graphics.DrawString(_gpuInfo, Font, b, 12, 12);
            return;
        }
        var sw = Stopwatch.StartNew();
        try
        {
            MakeCurrent();
            SyncAtlasIfNeeded();
            var previewTimes = AdvancePreviewClocks();
            RenderIntensity(_settings, (float)previewTimes.Animation, (float)previewTimes.Temporal);
            NativeGl.BindFramebuffer(NativeGl.GL_FRAMEBUFFER, 0);
            NativeGl.Viewport(0, 0, Math.Max(1, Width), Math.Max(1, Height));
            NativeGl.glClearColor(0, 0, 0, 1); NativeGl.glClear(NativeGl.GL_COLOR_BUFFER_BIT);
            NativeGl.UseProgram(_previewProgram);
            NativeGl.Uniform2i(NativeGl.GetUniformLocation(_previewProgram, "u_grid"), Math.Max(2,_settings.Width), Math.Max(2,_settings.Height));
            U2(_previewProgram, "u_view", Width, Height);
            U1(_previewProgram, "u_gamma", _settings.F("gamma"));
            Ui(_previewProgram, "u_invert", _settings.Invert ? 1 : 0);
            Ui(_previewProgram, "u_color_enabled", _settings.ColorEnabled ? 1 : 0);
            Ui(_previewProgram, "u_glyph_count", Math.Max(1, _settings.Charset.EnumerateRunes().Count()));
            NativeGl.Uniform2i(NativeGl.GetUniformLocation(_previewProgram, "u_atlas_grid"), _atlasCols, _atlasRows);
            SetPaletteUniforms(_previewProgram, _settings.PaletteStops);
            NativeGl.ActiveTexture(NativeGl.GL_TEXTURE0); NativeGl.BindTexture(NativeGl.GL_TEXTURE_2D, _atlas); Ui(_previewProgram, "u_atlas", 0);
            NativeGl.ActiveTexture(NativeGl.GL_TEXTURE0+1); NativeGl.BindTexture(NativeGl.GL_TEXTURE_2D, _intensityTex); Ui(_previewProgram, "u_intensity", 1);
            NativeGl.BindVertexArray(_vao); NativeGl.DrawArrays(NativeGl.GL_TRIANGLES, 0, 3);
            NativeGl.SwapBuffers(_dc);
        }
        catch (Exception ex) { _gpuInfo = "Render error: " + ex.Message; }
        sw.Stop();
        _frames++;
        if (_fpsClock.ElapsedMilliseconds >= 500)
        {
            _actualFps = _frames / _fpsClock.Elapsed.TotalSeconds; _frames = 0; _fpsClock.Restart();
            FrameStats?.Invoke(_actualFps, sw.Elapsed.TotalMilliseconds, _gpuInfo);
        }
    }

    public string CaptureAsciiFrame(double timeSeconds)
    {
        double animation = timeSeconds * _settings.Get("speed");
        double temporal = animation * _settings.Get("time_freq");
        return CaptureAsciiFrameInternal(animation, temporal);
    }

    public string CaptureCurrentAsciiFrame()
    {
        var times = AdvancePreviewClocks();
        return CaptureAsciiFrameInternal(times.Animation, times.Temporal);
    }

    private unsafe string CaptureAsciiFrameInternal(double animationTime, double temporalTime)
    {
        if (!_loaded) return "OpenGL no disponible";
        MakeCurrent();
        int w = Math.Max(2, _settings.Width), h = Math.Max(2, _settings.Height);
        RenderIntensity(_settings, (float)animationTime, (float)temporalTime);
        NativeGl.BindFramebuffer(NativeGl.GL_FRAMEBUFFER, _intensityFbo);
        NativeGl.Finish();
        byte[] pixels = new byte[w * h * 4];
        fixed (byte* p = pixels) NativeGl.ReadPixels(0, 0, w, h, NativeGl.GL_RGBA, NativeGl.GL_UNSIGNED_BYTE, (IntPtr)p);
        NativeGl.BindFramebuffer(NativeGl.GL_FRAMEBUFFER, 0);

        var runes = _settings.Charset.EnumerateRunes().ToArray(); if (runes.Length == 0) runes = " ".EnumerateRunes().ToArray();
        var sb = new StringBuilder((w + 1) * h); double gamma = Math.Max(.05, _settings.Get("gamma"));
        for (int y = h - 1; y >= 0; y--)
        {
            for (int x = 0; x < w; x++)
            {
                double v = pixels[(y * w + x) * 4] / 255.0; v = Math.Pow(Math.Clamp(v, 0, 1), 1.0 / gamma); if (_settings.Invert) v = 1 - v;
                int idx = Math.Clamp((int)Math.Round(v * (runes.Length - 1)), 0, runes.Length - 1); sb.Append(runes[idx].ToString());
            }
            if (y > 0) sb.AppendLine();
        }
        return sb.ToString();
    }

    public double BenchmarkGpu(int frames = 240)
    {
        if (!_loaded) return double.NaN;
        MakeCurrent();
        var sw = Stopwatch.StartNew();
        for (int i = 0; i < frames; i++)
        {
            double sourceTime = i / 120.0;
            double animationTime = sourceTime * _settings.Get("speed");
            RenderIntensity(_settings, (float)animationTime, (float)(animationTime * _settings.Get("time_freq")));
            NativeGl.BindFramebuffer(NativeGl.GL_FRAMEBUFFER, 0);
            NativeGl.Viewport(0,0,Math.Max(1,Width),Math.Max(1,Height));
            NativeGl.UseProgram(_previewProgram);
            NativeGl.Uniform2i(NativeGl.GetUniformLocation(_previewProgram,"u_grid"),Math.Max(2,_settings.Width),Math.Max(2,_settings.Height));
            U2(_previewProgram,"u_view",Math.Max(1,Width),Math.Max(1,Height)); U1(_previewProgram,"u_gamma",_settings.F("gamma")); Ui(_previewProgram,"u_invert",_settings.Invert?1:0); Ui(_previewProgram,"u_color_enabled",_settings.ColorEnabled?1:0);
            Ui(_previewProgram,"u_glyph_count",Math.Max(1,_settings.Charset.EnumerateRunes().Count())); NativeGl.Uniform2i(NativeGl.GetUniformLocation(_previewProgram,"u_atlas_grid"),_atlasCols,_atlasRows); SetPaletteUniforms(_previewProgram,_settings.PaletteStops);
            NativeGl.ActiveTexture(NativeGl.GL_TEXTURE0);NativeGl.BindTexture(NativeGl.GL_TEXTURE_2D,_atlas);Ui(_previewProgram,"u_atlas",0);NativeGl.ActiveTexture(NativeGl.GL_TEXTURE0+1);NativeGl.BindTexture(NativeGl.GL_TEXTURE_2D,_intensityTex);Ui(_previewProgram,"u_intensity",1);
            NativeGl.DrawArrays(NativeGl.GL_TRIANGLES,0,3);
        }
        NativeGl.Finish(); sw.Stop(); Invalidate(); return sw.Elapsed.TotalMilliseconds / frames;
    }

    private unsafe void EnsureIntensityTarget(int w,int h)
    {
        if(_intensityTex!=0 && _intensityW==w && _intensityH==h) return;
        if(_intensityFbo!=0) NativeGl.DeleteFramebuffers(1,ref _intensityFbo);
        if(_intensityTex!=0) NativeGl.DeleteTextures(1,ref _intensityTex);
        _intensityTex=0;_intensityFbo=0;
        uint intensityTex;
        NativeGl.GenTextures(1, &intensityTex);
        _intensityTex = intensityTex;
        NativeGl.BindTexture(NativeGl.GL_TEXTURE_2D, _intensityTex);
        NativeGl.TexParameteri(NativeGl.GL_TEXTURE_2D,NativeGl.GL_TEXTURE_MIN_FILTER,(int)NativeGl.GL_NEAREST);NativeGl.TexParameteri(NativeGl.GL_TEXTURE_2D,NativeGl.GL_TEXTURE_MAG_FILTER,(int)NativeGl.GL_NEAREST);
        NativeGl.TexImage2D(NativeGl.GL_TEXTURE_2D,0,(int)NativeGl.GL_RGBA8,w,h,0,NativeGl.GL_RGBA,NativeGl.GL_UNSIGNED_BYTE,IntPtr.Zero);
        uint intensityFbo;
        NativeGl.GenFramebuffers(1, &intensityFbo);
        _intensityFbo = intensityFbo;
        NativeGl.BindFramebuffer(NativeGl.GL_FRAMEBUFFER, _intensityFbo);
        NativeGl.FramebufferTexture2D(NativeGl.GL_FRAMEBUFFER, NativeGl.GL_COLOR_ATTACHMENT0, NativeGl.GL_TEXTURE_2D, _intensityTex, 0);
        if(NativeGl.CheckFramebufferStatus(NativeGl.GL_FRAMEBUFFER)!=NativeGl.GL_FRAMEBUFFER_COMPLETE) throw new InvalidOperationException("FBO de intensidad incompleto");
        _intensityW=w;_intensityH=h;
    }

    private (double Animation, double Temporal) AdvancePreviewClocks()
    {
        double timeline = CurrentTimeSeconds;
        if (!_previewClockInitialized)
        {
            _previewClockInitialized = true;
            _lastTimelineTime = timeline;
            return (_animationTime, _temporalTime);
        }

        double dt = Math.Clamp(timeline - _lastTimelineTime, 0.0, 0.25);
        _lastTimelineTime = timeline;
        double speed = _settings.Get("speed");
        _animationTime += dt * speed;
        _temporalTime += dt * speed * _settings.Get("time_freq");
        return (_animationTime, _temporalTime);
    }

    private void RenderIntensity(EffectSettings settings,float animationTime,float temporalTime)
    {
        int w=Math.Max(2,settings.Width),h=Math.Max(2,settings.Height);EnsureIntensityTarget(w,h);NativeGl.BindFramebuffer(NativeGl.GL_FRAMEBUFFER,_intensityFbo);NativeGl.Viewport(0,0,w,h);NativeGl.UseProgram(_captureProgram);SetCommonUniforms(_captureProgram,settings,animationTime,temporalTime);NativeGl.BindVertexArray(_vao);NativeGl.DrawArrays(NativeGl.GL_TRIANGLES,0,3);
    }

    private void MakeCurrent()
    {
        if (!NativeGl.wglMakeCurrent(_dc, _rc)) throw new InvalidOperationException("wglMakeCurrent falló");
    }

    private void SyncAtlasIfNeeded()
    {
        if (!_loaded && _rc == IntPtr.Zero) return;
        var ramp = string.IsNullOrEmpty(_settings.Charset) ? " " : _settings.Charset;
        if (ramp == _atlasRamp) return;
        if (_rc != IntPtr.Zero) MakeCurrent();
        BuildGlyphAtlas(ramp);
    }

    private unsafe void BuildGlyphAtlas(string ramp)
    {
        var runes = ramp.EnumerateRunes().ToArray(); if (runes.Length == 0) runes = " ".EnumerateRunes().ToArray();
        const int cw = 32, ch = 48, cols = 16; int rows = (int)Math.Ceiling(runes.Length / (double)cols);
        using var bmp = new Bitmap(cols * cw, rows * ch, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(bmp))
        {
            g.Clear(Color.Black); g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
            using var font = new Font("Cascadia Mono", 26, FontStyle.Regular, GraphicsUnit.Pixel);
            for (int i = 0; i < runes.Length; i++)
            {
                int x = (i % cols) * cw, y = (i / cols) * ch;
                var rect = new Rectangle(x, y, cw, ch);
                TextRenderer.DrawText(g, runes[i].ToString(), font, rect, Color.White, Color.Black, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
            }
        }
        var data = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
        try
        {
            if (_atlas == 0)
            {
                uint atlas;
                NativeGl.GenTextures(1, &atlas);
                _atlas = atlas;
            }
            NativeGl.BindTexture(NativeGl.GL_TEXTURE_2D, _atlas); NativeGl.PixelStorei(NativeGl.GL_UNPACK_ALIGNMENT, 1);
            NativeGl.TexParameteri(NativeGl.GL_TEXTURE_2D, NativeGl.GL_TEXTURE_MIN_FILTER, (int)NativeGl.GL_LINEAR); NativeGl.TexParameteri(NativeGl.GL_TEXTURE_2D, NativeGl.GL_TEXTURE_MAG_FILTER, (int)NativeGl.GL_LINEAR);
            NativeGl.TexImage2D(NativeGl.GL_TEXTURE_2D, 0, (int)NativeGl.GL_RGBA8, bmp.Width, bmp.Height, 0, NativeGl.GL_BGRA, NativeGl.GL_UNSIGNED_BYTE, data.Scan0);
        }
        finally { bmp.UnlockBits(data); }
        _atlasCols = cols; _atlasRows = rows; _atlasRamp = ramp;
    }

    private static readonly Dictionary<string, int> EffectIds = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Plasma"]=1,["Mandelbrot Zoom"]=2,["Julia"]=3,["Burning Ship"]=4,["Value Noise"]=5,["FBM Noise"]=6,["Tunnel"]=7,["Starfield"]=8,
        ["Interference"]=9,["Metaballs"]=10,["Kaleidoscope"]=11,["Ripples"]=12,["Fire"]=13,["Matrix Rain"]=14,["XOR Pattern"]=15,["Moire"]=16,
        ["Fireworks"]=17,["Radio Waves"]=18,["Rain Drops"]=19,["Rotating Galaxy"]=20,["Spinning Donut"]=21,["Spinning Shapes"]=22,["Horizon"]=23,["Bouncing Balls"]=24,["Cellular Automaton"]=25,["Wave Field"]=26,["Ocean Waves"]=27,["Ripple Tank"]=28,["Oscilloscope"]=29,["Water Caustics"]=30,["Aurora"]=31,["3D Shapes"]=32,["3D Terrain"]=33,["SDF Lab"]=34,["Flow Field"]=35,["Lightning"]=36,["Black Hole"]=37,["Strange Attractor"]=38,["Voronoi Cells"]=39,["Snowstorm"]=40,["DNA Helix"]=41,["Warp Grid 3D"]=42
    };

    private static readonly Dictionary<string, int> ShapeIds = new(StringComparer.OrdinalIgnoreCase) { ["Square"]=0,["Diamond"]=1,["Star"]=2,["Hex"]=3,["Cross"]=4 };

    private static void SetCommonUniforms(uint p, EffectSettings s, float animationTime, float temporalTime)
    {
        Ui(p,"u_effect",EffectIds.GetValueOrDefault(s.Effect,1)); Ui(p,"u_seed",s.Seed); NativeGl.Uniform2i(NativeGl.GetUniformLocation(p,"u_grid"),Math.Max(2,s.Width),Math.Max(2,s.Height));
        U1(p,"u_time",animationTime); U1(p,"u_temporal_time",temporalTime); U1(p,"u_scale",s.F("scale"));U1(p,"u_amp",s.F("osc_amp"));U1(p,"u_fx",s.F("freq_x"));U1(p,"u_fy",s.F("freq_y"));U1(p,"u_fd",s.F("freq_diag"));U1(p,"u_fr",s.F("freq_radial"));U1(p,"u_tf",s.F("time_freq"));U1(p,"u_phase",s.F("phase_deg"));U1(p,"u_turb",s.F("turbulence"));U1(p,"u_warp",s.F("warp"));U1(p,"u_dx",s.F("drift_x"));U1(p,"u_dy",s.F("drift_y"));U1(p,"u_pulse",s.F("pulse"));U1(p,"u_density",s.F("density"));U1(p,"u_aspect",s.F("aspect"));U1(p,"u_iterations",s.F("iterations"));
        string[] keys={"fire_height","fire_width","fire_wind","fire_particles","fire_particle_size","fire_particle_lift","ball_count","ball_radius","ball_gravity","ball_bounce","ball_speed","ball_trails","firework_count","firework_size","firework_sparks","firework_gravity","firework_decay","rain_count","rain_ring_size","rain_ring_width","rain_decay","galaxy_arms","galaxy_core","galaxy_twist","galaxy_halo","donut_major","donut_minor","donut_spin_x","donut_spin_y","donut_detail","star_amount","star_size","star_depth","matrix_trail","matrix_spacing","matrix_head","tunnel_rings","tunnel_twist","tunnel_depth","horizon_height","horizon_fov","horizon_wave","radio_thickness","radio_decay","radio_expand","ca_rule","ca_step_rate","ca_history","ca_seed_density","ca_alive","ca_scroll","wave_count","wave_height","wave_length","wave_direction","wave_spread","wave_sharpness","ocean_height","ocean_length","ocean_layers","ocean_direction","ocean_choppiness","ocean_foam","tank_sources","tank_frequency","tank_speed","tank_damping","tank_motion","tank_interference","scope_waveform","scope_frequency","scope_amplitude","scope_thickness","scope_dual","scope_phase","caustic_scale","caustic_distortion","caustic_speed","caustic_sharpness","caustic_layers","aurora_bands","aurora_width","aurora_flow","aurora_curl","aurora_shimmer","aurora_height","shape3d_type","shape3d_spin_x","shape3d_spin_y","shape3d_spin_z","shape3d_camera","shape3d_fov","shape3d_light","shape3d_mode","terrain_height","terrain_detail","terrain_speed","terrain_camera","terrain_water","terrain_grid","sdf_shape","sdf_repeat","sdf_twist","sdf_smooth","sdf_spin","sdf_depth","flow_particles","flow_scale","flow_strength","flow_trails","flow_curl","flow_speed","lightning_branches","lightning_width","lightning_jitter","lightning_forks","lightning_flash","lightning_speed","blackhole_size","blackhole_disk","blackhole_spin","blackhole_lens","blackhole_jets","blackhole_stars","attractor_type","attractor_points","attractor_zoom","attractor_rotation","attractor_trail","attractor_glow","voronoi_cells","voronoi_speed","voronoi_edges","voronoi_fill","voronoi_warp","voronoi_pulse","snow_amount","snow_size","snow_wind","snow_depth","snow_gust","snow_twinkle","dna_turns","dna_radius","dna_speed","dna_rungs","dna_tilt","dna_depth","warpgrid_density","warpgrid_depth","warpgrid_twist","warpgrid_wave","warpgrid_speed","warpgrid_horizon"};
        string[] uniforms={"u_fire_h","u_fire_w","u_fire_wind","u_fire_particles","u_fire_psize","u_fire_lift","u_ball_count","u_ball_radius","u_ball_gravity","u_ball_bounce","u_ball_speed","u_ball_trails","u_firework_count","u_firework_size","u_firework_sparks","u_firework_gravity","u_firework_decay","u_rain_count","u_rain_ring_size","u_rain_ring_width","u_rain_decay","u_galaxy_arms","u_galaxy_core","u_galaxy_twist","u_galaxy_halo","u_donut_major","u_donut_minor","u_donut_spin_x","u_donut_spin_y","u_donut_detail","u_star_amount","u_star_size","u_star_depth","u_matrix_trail","u_matrix_spacing","u_matrix_head","u_tunnel_rings","u_tunnel_twist","u_tunnel_depth","u_horizon_height","u_horizon_fov","u_horizon_wave","u_radio_thickness","u_radio_decay","u_radio_expand","u_ca_rule","u_ca_step_rate","u_ca_history","u_ca_seed_density","u_ca_alive","u_ca_scroll","u_wave_count","u_wave_height","u_wave_length","u_wave_direction","u_wave_spread","u_wave_sharpness","u_ocean_height","u_ocean_length","u_ocean_layers","u_ocean_direction","u_ocean_choppiness","u_ocean_foam","u_tank_sources","u_tank_frequency","u_tank_speed","u_tank_damping","u_tank_motion","u_tank_interference","u_scope_waveform","u_scope_frequency","u_scope_amplitude","u_scope_thickness","u_scope_dual","u_scope_phase","u_caustic_scale","u_caustic_distortion","u_caustic_speed","u_caustic_sharpness","u_caustic_layers","u_aurora_bands","u_aurora_width","u_aurora_flow","u_aurora_curl","u_aurora_shimmer","u_aurora_height","u_shape3d_type","u_shape3d_spin_x","u_shape3d_spin_y","u_shape3d_spin_z","u_shape3d_camera","u_shape3d_fov","u_shape3d_light","u_shape3d_mode","u_terrain_height","u_terrain_detail","u_terrain_speed","u_terrain_camera","u_terrain_water","u_terrain_grid","u_sdf_shape","u_sdf_repeat","u_sdf_twist","u_sdf_smooth","u_sdf_spin","u_sdf_depth","u_flow_particles","u_flow_scale","u_flow_strength","u_flow_trails","u_flow_curl","u_flow_speed","u_lightning_branches","u_lightning_width","u_lightning_jitter","u_lightning_forks","u_lightning_flash","u_lightning_speed","u_blackhole_size","u_blackhole_disk","u_blackhole_spin","u_blackhole_lens","u_blackhole_jets","u_blackhole_stars","u_attractor_type","u_attractor_points","u_attractor_zoom","u_attractor_rotation","u_attractor_trail","u_attractor_glow","u_voronoi_cells","u_voronoi_speed","u_voronoi_edges","u_voronoi_fill","u_voronoi_warp","u_voronoi_pulse","u_snow_amount","u_snow_size","u_snow_wind","u_snow_depth","u_snow_gust","u_snow_twinkle","u_dna_turns","u_dna_radius","u_dna_speed","u_dna_rungs","u_dna_tilt","u_dna_depth","u_warpgrid_density","u_warpgrid_depth","u_warpgrid_twist","u_warpgrid_wave","u_warpgrid_speed","u_warpgrid_horizon"};
        for(int i=0;i<keys.Length;i++)U1(p,uniforms[i],s.F(keys[i])); Ui(p,"u_shape_mode",ShapeIds.GetValueOrDefault(s.ShapeMode,0));
    }

    private static void SetPaletteUniforms(uint p, List<string> stops)
    {
        var list = stops.Count >= 2 ? stops.Take(8).ToList() : new List<string>{"#cccccc","#ffffff"}; Ui(p,"u_palette_count",list.Count);
        for(int i=0;i<8;i++) { var c=ParseColor(list[Math.Min(i,list.Count-1)]); int loc=NativeGl.GetUniformLocation(p,$"u_palette{i}"); if(loc>=0) NativeGl.Uniform3f(loc,c.R/255f,c.G/255f,c.B/255f); }
    }

    private static Color ParseColor(string s)
    {
        try { return ColorTranslator.FromHtml(s); } catch { return Color.White; }
    }

    private static void U1(uint p,string n,float v){int l=NativeGl.GetUniformLocation(p,n);if(l>=0)NativeGl.Uniform1f(l,v);} 
    private static void Ui(uint p,string n,int v){int l=NativeGl.GetUniformLocation(p,n);if(l>=0)NativeGl.Uniform1i(l,v);} 
    private static void U2(uint p,string n,float a,float b){int l=NativeGl.GetUniformLocation(p,n);if(l>=0)NativeGl.Uniform2f(l,a,b);} 
}
