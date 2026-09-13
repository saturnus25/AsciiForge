using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;

namespace AsciiForge;

internal static class NativeGl
{
    public const uint GL_VERTEX_SHADER = 0x8B31;
    public const uint GL_FRAGMENT_SHADER = 0x8B30;
    public const uint GL_COMPILE_STATUS = 0x8B81;
    public const uint GL_LINK_STATUS = 0x8B82;
    public const uint GL_INFO_LOG_LENGTH = 0x8B84;
    public const uint GL_TRIANGLES = 0x0004;
    public const uint GL_TEXTURE_2D = 0x0DE1;
    public const uint GL_TEXTURE0 = 0x84C0;
    public const uint GL_TEXTURE_MIN_FILTER = 0x2801;
    public const uint GL_TEXTURE_MAG_FILTER = 0x2800;
    public const uint GL_NEAREST = 0x2600;
    public const uint GL_LINEAR = 0x2601;
    public const uint GL_RGBA = 0x1908;
    public const uint GL_BGRA = 0x80E1;
    public const uint GL_UNSIGNED_BYTE = 0x1401;
    public const uint GL_RGBA8 = 0x8058;
    public const uint GL_COLOR_BUFFER_BIT = 0x00004000;
    public const uint GL_FRAMEBUFFER = 0x8D40;
    public const uint GL_COLOR_ATTACHMENT0 = 0x8CE0;
    public const uint GL_FRAMEBUFFER_COMPLETE = 0x8CD5;
    public const uint GL_PACK_ALIGNMENT = 0x0D05;
    public const uint GL_UNPACK_ALIGNMENT = 0x0CF5;
    public const int PFD_DRAW_TO_WINDOW = 0x00000004;
    public const int PFD_SUPPORT_OPENGL = 0x00000020;
    public const int PFD_DOUBLEBUFFER = 0x00000001;
    public const byte PFD_TYPE_RGBA = 0;
    public const sbyte PFD_MAIN_PLANE = 0;

    [StructLayout(LayoutKind.Sequential)]
    internal struct PIXELFORMATDESCRIPTOR
    {
        public ushort nSize, nVersion;
        public uint dwFlags;
        public byte iPixelType, cColorBits, cRedBits, cRedShift, cGreenBits, cGreenShift, cBlueBits, cBlueShift,
            cAlphaBits, cAlphaShift, cAccumBits, cAccumRedBits, cAccumGreenBits, cAccumBlueBits, cAccumAlphaBits,
            cDepthBits, cStencilBits, cAuxBuffers, iLayerType, bReserved;
        public uint dwLayerMask, dwVisibleMask, dwDamageMask;
    }

    [DllImport("user32.dll")] public static extern IntPtr GetDC(IntPtr hWnd);
    [DllImport("user32.dll")] public static extern int ReleaseDC(IntPtr hWnd, IntPtr hDc);
    [DllImport("gdi32.dll", SetLastError = true)] public static extern int ChoosePixelFormat(IntPtr hdc, ref PIXELFORMATDESCRIPTOR ppfd);
    [DllImport("gdi32.dll", SetLastError = true)] public static extern bool SetPixelFormat(IntPtr hdc, int format, ref PIXELFORMATDESCRIPTOR ppfd);
    [DllImport("gdi32.dll")] public static extern bool SwapBuffers(IntPtr hdc);
    [DllImport("opengl32.dll", SetLastError = true)] public static extern IntPtr wglCreateContext(IntPtr hdc);
    [DllImport("opengl32.dll")] public static extern void glClearColor(float r, float g, float b, float a);
    [DllImport("opengl32.dll")] public static extern void glClear(uint mask);
    [DllImport("opengl32.dll")] public static extern bool wglDeleteContext(IntPtr hglrc);
    [DllImport("opengl32.dll")] public static extern bool wglMakeCurrent(IntPtr hdc, IntPtr hglrc);
    [DllImport("opengl32.dll")] private static extern IntPtr wglGetProcAddress(string name);
    [DllImport("kernel32.dll", CharSet = CharSet.Ansi, SetLastError = true)] private static extern IntPtr GetProcAddress(IntPtr hModule, string procName);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)] private static extern IntPtr GetModuleHandle(string lpModuleName);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)] public delegate uint GlCreateShader(uint shaderType);
    [UnmanagedFunctionPointer(CallingConvention.StdCall)] public unsafe delegate void GlShaderSource(uint shader, int count, byte** strings, int* lengths);
    [UnmanagedFunctionPointer(CallingConvention.StdCall)] public delegate void GlCompileShader(uint shader);
    [UnmanagedFunctionPointer(CallingConvention.StdCall)] public delegate void GlGetShaderiv(uint shader, uint pname, out int param);
    [UnmanagedFunctionPointer(CallingConvention.StdCall)] public unsafe delegate void GlGetShaderInfoLog(uint shader, int maxLength, out int length, byte* infoLog);
    [UnmanagedFunctionPointer(CallingConvention.StdCall)] public delegate uint GlCreateProgram();
    [UnmanagedFunctionPointer(CallingConvention.StdCall)] public delegate void GlAttachShader(uint program, uint shader);
    [UnmanagedFunctionPointer(CallingConvention.StdCall)] public delegate void GlLinkProgram(uint program);
    [UnmanagedFunctionPointer(CallingConvention.StdCall)] public delegate void GlGetProgramiv(uint program, uint pname, out int param);
    [UnmanagedFunctionPointer(CallingConvention.StdCall)] public unsafe delegate void GlGetProgramInfoLog(uint program, int maxLength, out int length, byte* infoLog);
    [UnmanagedFunctionPointer(CallingConvention.StdCall)] public delegate void GlDeleteShader(uint shader);
    [UnmanagedFunctionPointer(CallingConvention.StdCall)] public delegate void GlDeleteProgram(uint program);
    [UnmanagedFunctionPointer(CallingConvention.StdCall)] public delegate void GlUseProgram(uint program);
    [UnmanagedFunctionPointer(CallingConvention.StdCall)] public unsafe delegate void GlGenVertexArrays(int n, uint* arrays);
    [UnmanagedFunctionPointer(CallingConvention.StdCall)] public delegate void GlBindVertexArray(uint array);
    [UnmanagedFunctionPointer(CallingConvention.StdCall)] public delegate int GlGetUniformLocation(uint program, [MarshalAs(UnmanagedType.LPStr)] string name);
    [UnmanagedFunctionPointer(CallingConvention.StdCall)] public delegate void GlUniform1f(int loc, float v0);
    [UnmanagedFunctionPointer(CallingConvention.StdCall)] public delegate void GlUniform1i(int loc, int v0);
    [UnmanagedFunctionPointer(CallingConvention.StdCall)] public delegate void GlUniform2f(int loc, float v0, float v1);
    [UnmanagedFunctionPointer(CallingConvention.StdCall)] public delegate void GlUniform2i(int loc, int v0, int v1);
    [UnmanagedFunctionPointer(CallingConvention.StdCall)] public delegate void GlUniform3f(int loc, float v0, float v1, float v2);
    [UnmanagedFunctionPointer(CallingConvention.StdCall)] public unsafe delegate void GlGenTextures(int n, uint* textures);
    [UnmanagedFunctionPointer(CallingConvention.StdCall)] public delegate void GlBindTexture(uint target, uint texture);
    [UnmanagedFunctionPointer(CallingConvention.StdCall)] public delegate void GlActiveTexture(uint texture);
    [UnmanagedFunctionPointer(CallingConvention.StdCall)] public delegate void GlTexParameteri(uint target, uint pname, int param);
    [UnmanagedFunctionPointer(CallingConvention.StdCall)] public delegate void GlPixelStorei(uint pname, int param);
    [UnmanagedFunctionPointer(CallingConvention.StdCall)] public delegate void GlTexImage2D(uint target, int level, int internalFormat, int width, int height, int border, uint format, uint type, IntPtr pixels);
    [UnmanagedFunctionPointer(CallingConvention.StdCall)] public unsafe delegate void GlGenFramebuffers(int n, uint* framebuffers);
    [UnmanagedFunctionPointer(CallingConvention.StdCall)] public delegate void GlBindFramebuffer(uint target, uint framebuffer);
    [UnmanagedFunctionPointer(CallingConvention.StdCall)] public delegate void GlFramebufferTexture2D(uint target, uint attachment, uint textarget, uint texture, int level);
    [UnmanagedFunctionPointer(CallingConvention.StdCall)] public delegate uint GlCheckFramebufferStatus(uint target);
    [UnmanagedFunctionPointer(CallingConvention.StdCall)] public delegate void GlDeleteFramebuffers(int n, ref uint framebuffers);
    [UnmanagedFunctionPointer(CallingConvention.StdCall)] public delegate void GlDeleteTextures(int n, ref uint textures);
    [UnmanagedFunctionPointer(CallingConvention.StdCall)] public delegate void GlViewport(int x, int y, int width, int height);
    [UnmanagedFunctionPointer(CallingConvention.StdCall)] public delegate void GlDrawArrays(uint mode, int first, int count);
    [UnmanagedFunctionPointer(CallingConvention.StdCall)] public delegate void GlReadPixels(int x, int y, int width, int height, uint format, uint type, IntPtr data);
    [UnmanagedFunctionPointer(CallingConvention.StdCall)] public delegate void GlFinish();
    [UnmanagedFunctionPointer(CallingConvention.StdCall)] public delegate IntPtr GlGetString(uint name);

    public static GlCreateShader CreateShader = null!;
    public static GlShaderSource ShaderSource = null!;
    public static GlCompileShader CompileShader = null!;
    public static GlGetShaderiv GetShaderiv = null!;
    public static GlGetShaderInfoLog GetShaderInfoLog = null!;
    public static GlCreateProgram CreateProgram = null!;
    public static GlAttachShader AttachShader = null!;
    public static GlLinkProgram LinkProgram = null!;
    public static GlGetProgramiv GetProgramiv = null!;
    public static GlGetProgramInfoLog GetProgramInfoLog = null!;
    public static GlDeleteShader DeleteShader = null!;
    public static GlDeleteProgram DeleteProgram = null!;
    public static GlUseProgram UseProgram = null!;
    public static GlGenVertexArrays GenVertexArrays = null!;
    public static GlBindVertexArray BindVertexArray = null!;
    public static GlGetUniformLocation GetUniformLocation = null!;
    public static GlUniform1f Uniform1f = null!;
    public static GlUniform1i Uniform1i = null!;
    public static GlUniform2f Uniform2f = null!;
    public static GlUniform2i Uniform2i = null!;
    public static GlUniform3f Uniform3f = null!;
    public static GlGenTextures GenTextures = null!;
    public static GlBindTexture BindTexture = null!;
    public static GlActiveTexture ActiveTexture = null!;
    public static GlTexParameteri TexParameteri = null!;
    public static GlPixelStorei PixelStorei = null!;
    public static GlTexImage2D TexImage2D = null!;
    public static GlGenFramebuffers GenFramebuffers = null!;
    public static GlBindFramebuffer BindFramebuffer = null!;
    public static GlFramebufferTexture2D FramebufferTexture2D = null!;
    public static GlCheckFramebufferStatus CheckFramebufferStatus = null!;
    public static GlDeleteFramebuffers DeleteFramebuffers = null!;
    public static GlDeleteTextures DeleteTextures = null!;
    public static GlViewport Viewport = null!;
    public static GlDrawArrays DrawArrays = null!;
    public static GlReadPixels ReadPixels = null!;
    public static GlFinish Finish = null!;
    public static GlGetString GetString = null!;

    public static void Load()
    {
        CreateShader = LoadProc<GlCreateShader>("glCreateShader");
        ShaderSource = LoadProc<GlShaderSource>("glShaderSource");
        CompileShader = LoadProc<GlCompileShader>("glCompileShader");
        GetShaderiv = LoadProc<GlGetShaderiv>("glGetShaderiv");
        GetShaderInfoLog = LoadProc<GlGetShaderInfoLog>("glGetShaderInfoLog");
        CreateProgram = LoadProc<GlCreateProgram>("glCreateProgram");
        AttachShader = LoadProc<GlAttachShader>("glAttachShader");
        LinkProgram = LoadProc<GlLinkProgram>("glLinkProgram");
        GetProgramiv = LoadProc<GlGetProgramiv>("glGetProgramiv");
        GetProgramInfoLog = LoadProc<GlGetProgramInfoLog>("glGetProgramInfoLog");
        DeleteShader = LoadProc<GlDeleteShader>("glDeleteShader");
        DeleteProgram = LoadProc<GlDeleteProgram>("glDeleteProgram");
        UseProgram = LoadProc<GlUseProgram>("glUseProgram");
        GenVertexArrays = LoadProc<GlGenVertexArrays>("glGenVertexArrays");
        BindVertexArray = LoadProc<GlBindVertexArray>("glBindVertexArray");
        GetUniformLocation = LoadProc<GlGetUniformLocation>("glGetUniformLocation");
        Uniform1f = LoadProc<GlUniform1f>("glUniform1f");
        Uniform1i = LoadProc<GlUniform1i>("glUniform1i");
        Uniform2f = LoadProc<GlUniform2f>("glUniform2f");
        Uniform2i = LoadProc<GlUniform2i>("glUniform2i");
        Uniform3f = LoadProc<GlUniform3f>("glUniform3f");
        GenTextures = LoadProc<GlGenTextures>("glGenTextures");
        BindTexture = LoadProc<GlBindTexture>("glBindTexture");
        ActiveTexture = LoadProc<GlActiveTexture>("glActiveTexture");
        TexParameteri = LoadProc<GlTexParameteri>("glTexParameteri");
        PixelStorei = LoadProc<GlPixelStorei>("glPixelStorei");
        TexImage2D = LoadProc<GlTexImage2D>("glTexImage2D");
        GenFramebuffers = LoadProc<GlGenFramebuffers>("glGenFramebuffers");
        BindFramebuffer = LoadProc<GlBindFramebuffer>("glBindFramebuffer");
        FramebufferTexture2D = LoadProc<GlFramebufferTexture2D>("glFramebufferTexture2D");
        CheckFramebufferStatus = LoadProc<GlCheckFramebufferStatus>("glCheckFramebufferStatus");
        DeleteFramebuffers = LoadProc<GlDeleteFramebuffers>("glDeleteFramebuffers");
        DeleteTextures = LoadProc<GlDeleteTextures>("glDeleteTextures");
        Viewport = LoadProc<GlViewport>("glViewport");
        DrawArrays = LoadProc<GlDrawArrays>("glDrawArrays");
        ReadPixels = LoadProc<GlReadPixels>("glReadPixels");
        Finish = LoadProc<GlFinish>("glFinish");
        GetString = LoadProc<GlGetString>("glGetString");
    }

    private static T LoadProc<T>(string name) where T : Delegate
    {
        var ptr = wglGetProcAddress(name);
        if (ptr == IntPtr.Zero || ptr == new IntPtr(1) || ptr == new IntPtr(2) || ptr == new IntPtr(3) || ptr == new IntPtr(-1))
        {
            var mod = GetModuleHandle("opengl32.dll");
            ptr = GetProcAddress(mod, name);
        }
        if (ptr == IntPtr.Zero) throw new EntryPointNotFoundException(name);
        return Marshal.GetDelegateForFunctionPointer<T>(ptr);
    }

    public static PIXELFORMATDESCRIPTOR DefaultPfd() => new()
    {
        nSize = (ushort)Marshal.SizeOf<PIXELFORMATDESCRIPTOR>(),
        nVersion = 1,
        dwFlags = PFD_DRAW_TO_WINDOW | PFD_SUPPORT_OPENGL | PFD_DOUBLEBUFFER,
        iPixelType = PFD_TYPE_RGBA,
        cColorBits = 32,
        cAlphaBits = 8,
        cDepthBits = 24,
        cStencilBits = 8,
        iLayerType = unchecked((byte)PFD_MAIN_PLANE)
    };

    public static unsafe uint CompileProgram(string vertex, string fragment)
    {
        uint vs = Compile(GL_VERTEX_SHADER, vertex);
        uint fs = Compile(GL_FRAGMENT_SHADER, fragment);
        uint p = CreateProgram();
        AttachShader(p, vs); AttachShader(p, fs); LinkProgram(p);
        GetProgramiv(p, GL_LINK_STATUS, out var ok);
        if (ok == 0)
        {
            GetProgramiv(p, GL_INFO_LOG_LENGTH, out var len);
            var buf = new byte[Math.Max(1, len)];
            fixed (byte* ptr = buf) GetProgramInfoLog(p, buf.Length, out _, ptr);
            DeleteShader(vs); DeleteShader(fs); DeleteProgram(p);
            throw new InvalidOperationException("OpenGL program link failed: " + Encoding.UTF8.GetString(buf).TrimEnd('\0'));
        }
        DeleteShader(vs); DeleteShader(fs);
        return p;
    }

    private static unsafe uint Compile(uint type, string source)
    {
        uint shader = CreateShader(type);
        byte[] bytes = Encoding.UTF8.GetBytes(source);
        fixed (byte* p = bytes)
        {
            byte* q = p;
            int len = bytes.Length;
            ShaderSource(shader, 1, &q, &len);
        }
        CompileShader(shader);
        GetShaderiv(shader, GL_COMPILE_STATUS, out var ok);
        if (ok == 0)
        {
            GetShaderiv(shader, GL_INFO_LOG_LENGTH, out var len);
            var buf = new byte[Math.Max(1, len)];
            fixed (byte* ptr = buf) GetShaderInfoLog(shader, buf.Length, out _, ptr);
            DeleteShader(shader);
            throw new InvalidOperationException("OpenGL shader compile failed: " + Encoding.UTF8.GetString(buf).TrimEnd('\0'));
        }
        return shader;
    }
}
