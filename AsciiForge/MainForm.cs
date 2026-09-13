using System.Diagnostics;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace AsciiForge;

internal sealed class MainForm : Form
{
    private readonly AppData _data = AppData.Load();
    private readonly EffectSettings _settings = new();
    private readonly FlowLayoutPanel _left = new();
    private readonly GlPreviewControl _preview = new();
    private readonly Label _status = new();
    private readonly RichTextBox _importView = new();
    private readonly System.Windows.Forms.Timer _importTimer = new() { Interval = 5 };
    private readonly Stopwatch _importClock = new();
    private List<string> _importFrames = [];
    private double _importFps = 20;
    private int _importIndex = -1;
    private readonly SafeComboBox _language = new();
    private readonly SafeComboBox _effect = new();
    private readonly SafeComboBox _preset = new();
    private readonly SafeComboBox _charsetPreset = new();
    private readonly SafeComboBox _palette = new();
    private readonly SafeComboBox _shape = new();
    private readonly TextBox _charset = new();
    private readonly NumericUpDown _width = new(), _height = new(), _fps = new(), _duration = new(), _seed = new();
    private readonly CheckBox _invert = new(), _color = new();
    private readonly Panel _generalParams = new(), _specificParams = new(), _paletteStops = new();
    private readonly Dictionary<string, ParameterRow> _paramRows = new(StringComparer.OrdinalIgnoreCase);
    private bool _applying;
    private double _lastActualFps;
    private readonly Button _pauseButton = new();
    private readonly ToolTip _tips = new() { InitialDelay = 500, ReshowDelay = 100, AutoPopDelay = 10000, ShowAlways = true };

    private static readonly Dictionary<string,string> EffectHelp = new(StringComparer.OrdinalIgnoreCase)
    {
        ["3D Shapes"]="Renderiza figuras tridimensionales con giro, perspectiva y luz.", ["3D Terrain"]="Genera un paisaje 3D procedural que avanza bajo la cámara.",
        ["SDF Lab"]="Experimenta con sólidos 3D, repeticiones, torsión y fusiones.", ["Flow Field"]="Dibuja trazas que siguen remolinos y corrientes invisibles.",
        ["Lightning"]="Genera rayos ramificados y destellos eléctricos.", ["Black Hole"]="Crea un agujero negro con disco, lente, estrellas y jets.",
        ["Strange Attractor"]="Dibuja sistemas caóticos que forman curvas y nubes matemáticas.", ["Voronoi Cells"]="Crea mosaicos de células móviles y sus fronteras.",
        ["Snowstorm"]="Simula nieve con profundidad, viento y ráfagas.", ["DNA Helix"]="Dibuja una doble hélice animada con sensación de profundidad.",
        ["Warp Grid 3D"]="Muestra una rejilla en perspectiva que se ondula y retuerce.",
        ["Fire"]="Genera llamas, chispas y fuego movido por viento.", ["Fireworks"]="Lanza cohetes y explosiones de chispas.",
        ["Cellular Automaton"]="Crea patrones de celdas donde cada fila nace de la anterior siguiendo una regla.",
        ["Wave Field"]="Mezcla varias ondas que viajan en distintas direcciones.", ["Ocean Waves"]="Simula oleaje irregular con varias capas y espuma.",
        ["Ripple Tank"]="Genera ondas circulares desde varios puntos y muestra cómo interfieren.", ["Oscilloscope"]="Dibuja señales como seno, cuadrada, triangular o diente de sierra.",
        ["Water Caustics"]="Imita las líneas de luz que se ven bajo agua en movimiento.", ["Aurora"]="Crea cortinas ondulantes de luz como una aurora boreal.",
        ["Horizon"]="Dibuja una rejilla en perspectiva que se pierde en el horizonte.", ["Ripples"]="Cruza ondas circulares suaves.", ["Radio Waves"]="Muestra pulsos que salen de un centro y se expanden."
    };

    public MainForm()
    {
        Text = "AsciiForge 5.1.1 · C# GPU Edition";
        Width = 1500; Height = 940; MinimumSize = new Size(1120, 720); BackColor = Theme.Bg; ForeColor = Theme.Text;
        StartPosition = FormStartPosition.CenterScreen;
        BuildUi();
        PopulateData();
        ApplyPreset(_preset.Items.Count > 0 ? _preset.Items[0]!.ToString()! : "");
        _preview.Settings = _settings;
        _preview.TargetFps = _settings.Fps;
        _preview.FrameStats += (fps, ms, gpu) => BeginInvoke((Action)(() => { _lastActualFps=fps; _status.Text=$"{_settings.Effect} / {_settings.Preset} · {_settings.Width}×{_settings.Height} · target {_settings.Fps} FPS · real {fps:0.0} · frame submit {ms:0.00} ms · DIRECT · {gpu}"; }));
    }

    private void BuildUi()
    {
        var split = new SplitContainer { Dock = DockStyle.Fill, SplitterDistance = 445, FixedPanel = FixedPanel.Panel1, BackColor = Theme.Bg };
        Controls.Add(split);
        _left.Dock = DockStyle.Fill; _left.AutoScroll = true; _left.WrapContents = false; _left.FlowDirection = FlowDirection.TopDown; _left.BackColor = Theme.Bg; _left.Padding = new Padding(8); split.Panel1.Controls.Add(_left);

        var right = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1, BackColor = Theme.Bg, Padding = new Padding(6) };
        right.RowStyles.Add(new RowStyle(SizeType.Absolute, 34)); right.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); split.Panel2.Controls.Add(right);
        _status.Dock = DockStyle.Fill; _status.TextAlign = ContentAlignment.MiddleRight; _status.ForeColor = Theme.Text; _status.Text = Localization.Text("status.init"); right.Controls.Add(_status,0,0);
        _preview.Dock = DockStyle.Fill; right.Controls.Add(_preview,0,1);
        _importView.Dock=DockStyle.Fill;_importView.Visible=false;_importView.ReadOnly=true;_importView.WordWrap=false;_importView.BackColor=Color.Black;_importView.ForeColor=Color.Gainsboro;_importView.Font=new Font("Cascadia Mono",10);_importView.BorderStyle=BorderStyle.None;right.Controls.Add(_importView,0,1);_importView.BringToFront();
        _importTimer.Tick+=(_,_)=>UpdateImportedFrame();_importTimer.Start();

        var lang = Group(Localization.Text("group.language"), 62); lang.Tag="group.language"; SetupCombo(_language); _language.Items.AddRange(["Español","English"]); _language.SelectedIndex=0; _language.SetBounds(10,25,395,28); lang.Controls.Add(_language); _left.Controls.Add(lang);

        var ep = Group(Localization.Text("group.effect"), 126); ep.Tag="group.effect";
        SetupCombo(_effect); SetupCombo(_preset); _effect.SetBounds(10,25,395,28); _preset.SetBounds(10,62,395,28); ep.Controls.AddRange([_effect,_preset]);
        var rnd = Btn(Localization.Text("button.random"), (_,_) => { _seed.Value = Random.Shared.Next(0, int.MaxValue); }); rnd.Tag="button.random"; rnd.SetBounds(10,95,92,26); ep.Controls.Add(rnd);
        var restart=Btn(Localization.Text("button.restart"),(_,_)=>{_preview.RestartAnimation();if(_importView.Visible){_importClock.Restart();_importIndex=-1;}});restart.Tag="button.restart";restart.SetBounds(107,95,92,26);ep.Controls.Add(restart);
        _pauseButton.Text=Localization.Text("button.pause");Theme.Button(_pauseButton);_pauseButton.Tag="button.pause";_pauseButton.SetBounds(204,95,92,26);_pauseButton.Click+=(_,_)=>TogglePause();ep.Controls.Add(_pauseButton);
        var bench=Btn(Localization.Text("button.benchmark"),(_,_)=>Benchmark());bench.Tag="button.benchmark";bench.SetBounds(301,95,104,26);ep.Controls.Add(bench); _left.Controls.Add(ep);

        var output=Group(Localization.Text("group.output"),170); output.Tag="group.output"; SetupNumeric(_width,16,1000,178);SetupNumeric(_height,8,500,50);SetupNumeric(_fps,1,240,30);SetupNumeric(_duration,0.1m,600,6,1);SetupNumeric(_seed,0,int.MaxValue,1337);
        AddNumericRow(output,"label.width",_width,25);AddNumericRow(output,"label.height",_height,53);AddNumericRow(output,"FPS",_fps,81);AddNumericRow(output,"label.duration",_duration,109);AddNumericRow(output,"Seed",_seed,137);_left.Controls.Add(output);

        var gp=Group(Localization.Text("group.general"), ParameterCatalog.General.Length*34+30); gp.Tag="group.general";_generalParams.SetBounds(10,23,395,gp.Height-28);_generalParams.BackColor=Theme.Panel;gp.Controls.Add(_generalParams);_left.Controls.Add(gp);
        foreach(var d in ParameterCatalog.General) AddParamRow(_generalParams,d);

        var sp=Group(Localization.Text("group.specific"),80); sp.Tag="group.specific"; sp.Name="specificGroup";_specificParams.SetBounds(10,23,395,52);_specificParams.BackColor=Theme.Panel;sp.Controls.Add(_specificParams);_left.Controls.Add(sp);

        var chars=Group(Localization.Text("group.charset"),142); chars.Tag="group.charset";SetupCombo(_charsetPreset);_charsetPreset.SetBounds(10,25,395,28);Theme.TextBox(_charset);_charset.SetBounds(10,60,395,28);_invert.Text=Localization.Text("check.invert");_invert.ForeColor=Theme.Text;_invert.BackColor=Theme.Panel;_invert.SetBounds(10,94,170,26);_invert.Tag="check.invert";chars.Controls.AddRange([_charsetPreset,_charset,_invert]);var copy=Btn(Localization.Text("button.copy"),(_,_)=>CopyFrame());copy.Tag="button.copy";copy.SetBounds(270,94,135,27);chars.Controls.Add(copy);_left.Controls.Add(chars);

        var col=Group(Localization.Text("group.color"),198); col.Tag="group.color";SetupCombo(_palette);_palette.SetBounds(10,25,395,28);_color.Text=Localization.Text("check.color");_color.Checked=true;_color.ForeColor=Theme.Text;_color.BackColor=Theme.Panel;_color.Tag="check.color";_color.SetBounds(10,58,210,26);_paletteStops.SetBounds(10,89,395,62);_paletteStops.BackColor=Theme.Panel;_paletteStops.AutoScroll=true;col.Controls.AddRange([_palette,_color,_paletteStops]);var add=Btn(Localization.Text("button.addcolor"),(_,_)=>AddPaletteStop());add.Tag="button.addcolor";add.SetBounds(10,158,85,28);var exp=Btn(Localization.Text("button.export"),(_,_)=>ExportDialog());exp.Tag="button.export";exp.SetBounds(101,158,95,28);var imp=Btn(Localization.Text("button.import"),(_,_)=>ImportPs());imp.Tag="button.import";imp.SetBounds(202,158,100,28);var txt=Btn(Localization.Text("button.save"),(_,_)=>SaveFrame());txt.Tag="button.save";txt.SetBounds(308,158,97,28);col.Controls.AddRange([add,exp,imp,txt]);_left.Controls.Add(col);

        var shape=Group(Localization.Text("group.shape"),70); shape.Tag="group.shape";SetupCombo(_shape);_shape.Items.AddRange(["Square","Diamond","Star","Hex","Cross"]);_shape.SelectedIndex=0;_shape.SetBounds(10,28,395,28);shape.Controls.Add(_shape);_left.Controls.Add(shape);

        _tips.SetToolTip(_effect,"Elige qué tipo de animación quieres generar.");
        _tips.SetToolTip(_preset,"Elige una configuración preparada para el efecto actual.");
        _tips.SetToolTip(rnd,"Cambia la semilla para obtener otra variación del mismo efecto.");
        _tips.SetToolTip(restart,"Vuelve a empezar la animación desde el principio.");
        _tips.SetToolTip(_pauseButton,"Congela o continúa la animación.");
        _tips.SetToolTip(bench,"Mide cuánto tarda en dibujarse el efecto con la GPU.");
        _tips.SetToolTip(_width,"Cambia cuántos caracteres hay en cada fila.");
        _tips.SetToolTip(_height,"Cambia cuántas filas de caracteres hay.");
        _tips.SetToolTip(_fps,"Marca cuántos frames por segundo intenta mostrar la vista previa.");
        _tips.SetToolTip(_duration,"Marca cuánto dura la animación al exportarla.");
        _tips.SetToolTip(_seed,"Cambia la variación aleatoria sin cambiar el tipo de efecto.");
        _tips.SetToolTip(_charsetPreset,"Elige una rampa de caracteres preparada.");
        _tips.SetToolTip(_charset,"Los caracteres de la izquierda representan zonas oscuras y los de la derecha zonas claras.");
        _tips.SetToolTip(_invert,"Intercambia qué caracteres se usan para zonas claras y oscuras.");
        _tips.SetToolTip(_palette,"Elige los colores usados desde las zonas oscuras hasta las claras.");
        _tips.SetToolTip(_color,"Activa o desactiva el color sin quitar el arte ASCII.");
        _tips.SetToolTip(add,"Añade otro color al gradiente.");
        _tips.SetToolTip(exp,"Guarda la animación en PowerShell, HTML, JSON, C#, ANSI o texto.");
        _tips.SetToolTip(imp,"Carga frames de un script PowerShell reconocido sin ejecutar el script.");
        _tips.SetToolTip(txt,"Guarda el frame visible como texto ASCII.");
        _tips.SetToolTip(_shape,"Elige la figura usada por Spinning Shapes.");

        WireEvents();
        ApplyStaticTips();
        KeyPreview=true;KeyDown+=(_,e)=>{if(e.KeyCode==Keys.Space){TogglePause();e.SuppressKeyPress=true;}else if(e.KeyCode==Keys.F5){_preview.RestartAnimation();}else if(e.Control&&e.KeyCode==Keys.E){ExportDialog();e.SuppressKeyPress=true;}};
    }

    private void PopulateData()
    {
        _effect.Items.AddRange(_data.Presets.Keys.Cast<object>().ToArray());
        if(_effect.Items.Count>0)_effect.SelectedIndex=0;UpdateEffectTip();
        _charsetPreset.Items.AddRange(_data.Charsets.Keys.Cast<object>().ToArray());if(_charsetPreset.Items.Count>0)_charsetPreset.SelectedIndex=0;
        _palette.Items.AddRange(_data.Palettes.Keys.Cast<object>().ToArray());if(_palette.Items.Count>0)_palette.SelectedItem="Plasma";
    }

    private void WireEvents()
    {
        _language.SelectedIndexChanged += (_,_) => { Localization.English = _language.SelectedIndex == 1; ApplyLanguage(); };
        _effect.SelectedIndexChanged += (_,_) => { if(_applying)return; ExitImportedMode(); _settings.Effect=_effect.Text; UpdateEffectTip(); RebuildPresets(); RebuildSpecific(); };
        _preset.SelectedIndexChanged += (_,_) => { if(!_applying && _preset.SelectedItem is not null) ApplyPreset(_preset.Text); };
        _charsetPreset.SelectedIndexChanged += (_,_) => { if(_applying)return;if(_data.Charsets.TryGetValue(_charsetPreset.Text,out var r)){_charset.Text=r;_settings.CharsetName=_charsetPreset.Text;_settings.Charset=r;Push();}};
        _charset.TextChanged += (_,_)=>{if(_applying)return;_settings.Charset=_charset.Text.Length==0?" ":_charset.Text;Push();};
        _invert.CheckedChanged += (_,_)=>{_settings.Invert=_invert.Checked;Push();};_color.CheckedChanged+=(_,_)=>{_settings.ColorEnabled=_color.Checked;Push();};
        _palette.SelectedIndexChanged += (_,_)=>{if(_applying)return;ApplyPalette(_palette.Text);};_shape.SelectedIndexChanged+=(_,_)=>{if(!_applying){_settings.ShapeMode=_shape.Text;MarkCustom();Push();}};
        _width.ValueChanged+=(_,_)=>{_settings.Width=(int)_width.Value;Push();};_height.ValueChanged+=(_,_)=>{_settings.Height=(int)_height.Value;Push();};_fps.ValueChanged+=(_,_)=>{_settings.Fps=(int)_fps.Value;_preview.TargetFps=_settings.Fps;Push();};_duration.ValueChanged+=(_,_)=>_settings.Duration=(double)_duration.Value;_seed.ValueChanged+=(_,_)=>{_settings.Seed=(int)_seed.Value;Push();};
    }


    private void TogglePause(){
        if(_importView.Visible){if(_importClock.IsRunning){_importClock.Stop();_pauseButton.Text=Localization.Text("button.resume");}else{_importClock.Start();_pauseButton.Text=Localization.Text("button.pause");}return;}
        _preview.TogglePause();_pauseButton.Text=_preview.Paused?Localization.Text("button.resume"):Localization.Text("button.pause");
    }

    private void RebuildPresets()
    {
        _applying=true;_preset.Items.Clear();if(_data.Presets.TryGetValue(_settings.Effect,out var g))_preset.Items.AddRange(g.Keys.Cast<object>().ToArray());_applying=false;if(_preset.Items.Count>0){_preset.SelectedIndex=0;ApplyPreset(_preset.Text);}RebuildSpecific();
    }

    private void ApplyPreset(string name)
    {
        if(string.IsNullOrEmpty(name)||!_data.Presets.TryGetValue(_effect.Text,out var group)||!group.TryGetValue(name,out var p))return;
        _applying=true;_settings.Effect=_effect.Text;_settings.Preset=name;_settings.ResetValues();
        foreach(var kv in p){if(kv.Value.ValueKind==JsonValueKind.Number&&kv.Value.TryGetDouble(out var d))_settings.Set(kv.Key,d);else if(kv.Key.Equals("palette",StringComparison.OrdinalIgnoreCase)&&kv.Value.ValueKind==JsonValueKind.String)_settings.PaletteName=kv.Value.GetString()??"Monochrome";else if(kv.Key.Equals("shape_mode",StringComparison.OrdinalIgnoreCase)&&kv.Value.ValueKind==JsonValueKind.String)_settings.ShapeMode=kv.Value.GetString()??"Square";}
        foreach(var row in _paramRows)row.Value.SetValue(_settings.Get(row.Key));
        ApplyPalette(_settings.PaletteName,false);_shape.SelectedItem=_settings.ShapeMode;_applying=false;RebuildSpecific();Push();
    }

    private void RebuildSpecific()
    {
        _specificParams.Controls.Clear();
        foreach(var key in _paramRows.Keys.Where(k=>ParameterCatalog.Specific.Values.SelectMany(x=>x).Any(p=>p.Key.Equals(k,StringComparison.OrdinalIgnoreCase))).ToList())_paramRows.Remove(key);
        if(!ParameterCatalog.Specific.TryGetValue(_settings.Effect,out var arr)) { var l=new Label{Text=Localization.Text("specific.none"),ForeColor=Theme.Muted,AutoSize=true,Top=8,Left=2};_specificParams.Controls.Add(l);ResizeSpecificGroup(75);return; }
        int y=0;foreach(var d in arr){var row=CreateParamRow(d);row.Top=y;_specificParams.Controls.Add(row);y+=34;}ResizeSpecificGroup(y+30);
    }

    private void ResizeSpecificGroup(int height)
    {
        var group=_left.Controls.Cast<Control>().FirstOrDefault(c=>c.Name=="specificGroup");if(group is null)return;group.Height=Math.Max(65,height);_specificParams.Height=group.Height-28;
    }

    private void AddParamRow(Panel panel,ParamDesc d){var row=CreateParamRow(d);row.Top=panel.Controls.Count*34;panel.Controls.Add(row);}
    private ParameterRow CreateParamRow(ParamDesc d){var row=new ParameterRow(d,_settings.Get(d.Key)){Left=0,Width=390,Anchor=AnchorStyles.Left|AnchorStyles.Right|AnchorStyles.Top};row.ValueChanged+=v=>{_settings.Set(d.Key,v);MarkCustom();Push();};_paramRows[d.Key]=row;return row;}

    private void ApplyPalette(string name,bool push=true)
    {
        if(!_data.Palettes.TryGetValue(name,out var stops))return;_settings.PaletteName=name;_settings.PaletteStops=new List<string>(stops);_applying=true;_palette.SelectedItem=name;_applying=false;RebuildPaletteStops();if(push)Push();
    }

    private void RebuildPaletteStops()
    {
        _paletteStops.Controls.Clear();int x=2;for(int i=0;i<_settings.PaletteStops.Count;i++){int idx=i;var b=new Button{Left=x,Top=6,Width=42,Height=42,BackColor=ParseColor(_settings.PaletteStops[i]),FlatStyle=FlatStyle.Flat,Tag=idx};b.FlatAppearance.BorderColor=Color.Gray;b.Click+=(_,_)=>EditPaletteStop(idx);_tips.SetToolTip(b,Localization.English?"Click to change this gradient color.":"Haz clic para cambiar este color del gradiente.");_paletteStops.Controls.Add(b);x+=46;}
    }

    private void EditPaletteStop(int index){using var dlg=new ColorDialog{Color=ParseColor(_settings.PaletteStops[index]),FullOpen=true};if(dlg.ShowDialog(this)!=DialogResult.OK)return;EnsureCustomPalette();_settings.PaletteStops[index]=ColorTranslator.ToHtml(dlg.Color);RebuildPaletteStops();Push();}
    private void AddPaletteStop(){using var dlg=new ColorDialog{Color=Color.White,FullOpen=true};if(dlg.ShowDialog(this)!=DialogResult.OK)return;EnsureCustomPalette();_settings.PaletteStops.Add(ColorTranslator.ToHtml(dlg.Color));RebuildPaletteStops();Push();}
    private void EnsureCustomPalette(){if(!_settings.PaletteName.Equals("Custom",StringComparison.OrdinalIgnoreCase)){_settings.PaletteName="Custom";if(!_palette.Items.Contains("Custom"))_palette.Items.Add("Custom");_applying=true;_palette.SelectedItem="Custom";_applying=false;}}

    private void MarkCustom(){if(_applying)return;_settings.Preset="Custom";if(!_preset.Items.Contains("Custom"))_preset.Items.Add("Custom");_applying=true;_preset.SelectedItem="Custom";_applying=false;}
    private void Push(){_preview.Settings=_settings.Clone();_preview.TargetFps=_settings.Fps;}

    private void CopyFrame(){try{string f=_importView.Visible&&_importFrames.Count>0?_importFrames[Math.Max(0,_importIndex)]:_preview.CaptureCurrentAsciiFrame();Clipboard.SetText(f);_status.Text=Localization.English?"ASCII frame copied":"Frame ASCII copiado";}catch(Exception ex){MessageBox.Show(ex.Message,"AsciiForge",MessageBoxButtons.OK,MessageBoxIcon.Error);}}
    private void SaveFrame(){using var s=new SaveFileDialog{Filter=Localization.English?"Text|*.txt":"Texto|*.txt",FileName="asciiforge-frame.txt"};if(s.ShowDialog(this)==DialogResult.OK){string f=_importView.Visible&&_importFrames.Count>0?_importFrames[Math.Max(0,_importIndex)]:_preview.CaptureCurrentAsciiFrame();File.WriteAllText(s.FileName,f,Encoding.UTF8);}}
    private void Benchmark(){try{double ms=_preview.BenchmarkGpu();MessageBox.Show(Localization.English?$"GPU direct preview\n\n{_preview.GpuInfo}\n{_settings.Width}×{_settings.Height} ASCII over {_preview.Width}×{_preview.Height} px\n\n{ms:0.000} ms/frame GPU+driver\n~{1000.0/ms:0} theoretical FPS (without target limit)":$"GPU direct preview\n\n{_preview.GpuInfo}\n{_settings.Width}×{_settings.Height} ASCII sobre {_preview.Width}×{_preview.Height} px\n\n{ms:0.000} ms/frame GPU+driver\n~{1000.0/ms:0} FPS teóricos (sin límite de target)","Benchmark",MessageBoxButtons.OK,MessageBoxIcon.Information);}catch(Exception ex){MessageBox.Show(ex.Message);}}

    private void ExportDialog()
    {
        using var s=new SaveFileDialog{Filter=Localization.English?"PowerShell|*.ps1|HTML|*.html|JSON|*.json|C# standalone|*.cs|ANSI|*.ans|Text|*.txt":"PowerShell|*.ps1|HTML|*.html|JSON|*.json|C# standalone|*.cs|ANSI|*.ans|Texto|*.txt",FileName=_settings.Effect.Replace(' ','_').ToLowerInvariant()+".ps1"};if(s.ShowDialog(this)!=DialogResult.OK)return;
        try{List<string> frames;int count;if(_importView.Visible&&_importFrames.Count>0){frames=new List<string>(_importFrames);count=frames.Count;_settings.Fps=Math.Max(1,(int)Math.Round(_importFps));}else{count=Math.Max(1,(int)Math.Round(_settings.Duration*_settings.Fps));frames=new List<string>(count);UseWaitCursor=true;for(int i=0;i<count;i++){frames.Add(_preview.CaptureAsciiFrame(i/(double)_settings.Fps));if(i%12==0){_status.Text=Localization.English?$"Exporting {i+1}/{count}":$"Exportando {i+1}/{count}";Application.DoEvents();}}}ExportService.Save(s.FileName,_settings,frames);_status.Text=Localization.English?$"Exported {Path.GetFileName(s.FileName)} · {count} frames":$"Exportado {Path.GetFileName(s.FileName)} · {count} frames";}catch(Exception ex){MessageBox.Show(ex.Message,Localization.English?"Export":"Exportación",MessageBoxButtons.OK,MessageBoxIcon.Error);}finally{UseWaitCursor=false;}
    }

    private void ImportPs(){using var o=new OpenFileDialog{Filter=Localization.English?"PowerShell|*.ps1|All files|*.*":"PowerShell|*.ps1|Todos|*.*"};if(o.ShowDialog(this)!=DialogResult.OK)return;try{var(frames,fps)=PowerShellImport.Parse(File.ReadAllText(o.FileName,Encoding.UTF8));EnterImportedMode(frames,fps,Path.GetFileName(o.FileName));}catch(Exception ex){MessageBox.Show(ex.Message,"Import PowerShell",MessageBoxButtons.OK,MessageBoxIcon.Error);}}

    private void EnterImportedMode(List<string> frames,double fps,string source){_importFrames=frames.Select(PowerShellImport.StripAnsi).ToList();_importFps=Math.Max(1,fps);_importIndex=-1;_importClock.Restart();_pauseButton.Text=Localization.Text("button.pause");_preview.Visible=false;_importView.Visible=true;_importView.BringToFront();int w=_importFrames.Max(f=>f.Split('\n').Max(l=>l.TrimEnd('\r').Length));int h=_importFrames.Max(f=>f.Split('\n').Length);_status.Text=$"Imported · {source} · {_importFrames.Count} frames · {_importFps:0.##} FPS · {w}×{h}";UpdateImportedFrame();}
    private void ExitImportedMode(){if(!_importView.Visible)return;_importClock.Stop();_importView.Visible=false;_preview.Visible=true;_pauseButton.Text=_preview.Paused?Localization.Text("button.resume"):Localization.Text("button.pause");_preview.BringToFront();_importFrames=[];_importIndex=-1;}
    private void UpdateImportedFrame(){if(!_importView.Visible||_importFrames.Count==0)return;int i=(int)(_importClock.Elapsed.TotalSeconds*_importFps)%_importFrames.Count;if(i==_importIndex)return;_importIndex=i;int sel=_importView.SelectionStart;_importView.Text=_importFrames[i];_importView.SelectionStart=Math.Min(sel,_importView.TextLength);_status.Text=$"Imported Frames · {_importFrames.Count} · {_importFps:0.##} FPS · frame {i+1}";}

    private void ApplyLanguage()
    {
        foreach (Control c in Controls) ApplyLanguageRecursive(c);
        _generalParams.Controls.Clear();
        foreach (var key in ParameterCatalog.General.Select(x=>x.Key)) _paramRows.Remove(key);
        foreach (var d in ParameterCatalog.General) AddParamRow(_generalParams,d);
        RebuildSpecific();
        RebuildPaletteStops();
        UpdateEffectTip();
        ApplyStaticTips();
        _pauseButton.Text = (_importView.Visible ? !_importClock.IsRunning : _preview.Paused) ? Localization.Text("button.resume") : Localization.Text("button.pause");
    }

    private void ApplyLanguageRecursive(Control c)
    {
        if (c.Tag is string key && (key.StartsWith("group.") || key.StartsWith("button.") || key.StartsWith("check.") || key.StartsWith("label.")))
        {
            c.Text = Localization.Text(key);
            string tip=Localization.Tip(key); if(!string.IsNullOrEmpty(tip)) _tips.SetToolTip(c,tip);
        }
        foreach (Control child in c.Controls) ApplyLanguageRecursive(child);
    }

    private void ApplyStaticTips()
    {
        bool en=Localization.English;
        _tips.SetToolTip(_effect,en?"Choose the kind of animation you want to generate.":"Elige qué tipo de animación quieres generar.");
        _tips.SetToolTip(_preset,en?"Choose a prepared setup for the current effect.":"Elige una configuración preparada para el efecto actual.");
        _tips.SetToolTip(_width,en?"Changes how many characters are used in each row.":"Cambia cuántos caracteres hay en cada fila.");
        _tips.SetToolTip(_height,en?"Changes how many rows of characters are used.":"Cambia cuántas filas de caracteres hay.");
        _tips.SetToolTip(_fps,en?"Sets the target frames per second for the preview.":"Marca cuántos frames por segundo intenta mostrar la vista previa.");
        _tips.SetToolTip(_duration,en?"Sets how long the exported animation lasts.":"Marca cuánto dura la animación al exportarla.");
        _tips.SetToolTip(_seed,en?"Changes the random variation without changing the effect type.":"Cambia la variación aleatoria sin cambiar el tipo de efecto.");
        _tips.SetToolTip(_charsetPreset,en?"Choose a prepared character ramp.":"Elige una rampa de caracteres preparada.");
        _tips.SetToolTip(_charset,en?"Characters on the left represent dark areas and characters on the right represent bright areas.":"Los caracteres de la izquierda representan zonas oscuras y los de la derecha zonas claras.");
        _tips.SetToolTip(_invert,en?"Swaps which characters are used for bright and dark areas.":"Intercambia qué caracteres se usan para zonas claras y oscuras.");
        _tips.SetToolTip(_palette,en?"Choose the colors used from dark areas to bright areas.":"Elige los colores usados desde las zonas oscuras hasta las claras.");
        _tips.SetToolTip(_color,en?"Turns color on or off without removing the ASCII art.":"Activa o desactiva el color sin quitar el arte ASCII.");
        _tips.SetToolTip(_shape,en?"Choose the shape used by Spinning Shapes.":"Elige la figura usada por Spinning Shapes.");
    }

    private void UpdateEffectTip()
    {
        string text=Localization.English ? Localization.EffectHelp(_effect.Text) : EffectHelp.GetValueOrDefault(_effect.Text,"Genera este tipo de animación. Cambia los controles y observa el resultado en directo.");
        _tips.SetToolTip(_effect,text);
    }

    private GroupBox Group(string title,int h)=>new(){Text=title,Width=415,Height=h,ForeColor=Theme.Text,BackColor=Theme.Panel,Margin=new Padding(0,0,0,7)};
    private Button Btn(string text,EventHandler click){var b=new Button{Text=text};Theme.Button(b);b.Click+=click;return b;}
    private static void SetupCombo(ComboBox c){c.DropDownStyle=ComboBoxStyle.DropDownList;c.DrawMode=DrawMode.OwnerDrawFixed;c.ItemHeight=22;Theme.Combo(c);c.DrawItem+=(_,e)=>{if(e.Index<0)return;var selected=(e.State&DrawItemState.Selected)!=0;using var bg=new SolidBrush(selected?Color.FromArgb(62,62,62):Theme.Input);using var fg=new SolidBrush(Theme.Text);e.Graphics.FillRectangle(bg,e.Bounds);e.Graphics.DrawString(c.Items[e.Index]?.ToString()??"",c.Font,fg,e.Bounds.Left+3,e.Bounds.Top+3);};}
    private static void SetupNumeric(NumericUpDown n,decimal min,decimal max,decimal val,int dec=0){n.Minimum=min;n.Maximum=max;n.Value=Math.Clamp(val,min,max);n.DecimalPlaces=dec;n.Increment=dec>0?0.1m:1m;Theme.Numeric(n);}
    private static void AddNumericRow(Control p,string label,NumericUpDown n,int y){string text=label.StartsWith("label.")?Localization.Text(label):label;var l=new Label{Text=text,Tag=label.StartsWith("label.")?label:null,Left=10,Top=y+3,Width=180,Height=24,ForeColor=Theme.Text};n.SetBounds(280,y,125,25);p.Controls.Add(l);p.Controls.Add(n);}
    private static Color ParseColor(string s){try{return ColorTranslator.FromHtml(s);}catch{return Color.White;}}
}

internal static class PowerShellImport
{
    private static readonly Regex Ansi = new("\\x1B\\[[0-?]*[ -/]*[@-~]", RegexOptions.Compiled);
    public static string StripAnsi(string text) => Ansi.Replace(text, "");
    public static (List<string> Frames,double Fps) Parse(string text)
    {
        var m=Regex.Match(text,@"\$payload\s*=\s*@'\s*(.*?)\s*'@",RegexOptions.Singleline|RegexOptions.IgnoreCase);if(m.Success){byte[] packed=Convert.FromBase64String(Regex.Replace(m.Groups[1].Value,@"\s+",""));using var ms=new MemoryStream(packed);using var gz=new GZipStream(ms,CompressionMode.Decompress);using var sr=new StreamReader(gz,Encoding.UTF8);var frames=JsonSerializer.Deserialize<List<string>>(sr.ReadToEnd())??[];return(frames,DetectFps(text));}
        var a=Regex.Match(text,@"\$frames\s*=\s*@\((.*?)\)",RegexOptions.Singleline|RegexOptions.IgnoreCase);if(a.Success){var list=new List<string>();foreach(Match x in Regex.Matches(a.Groups[1].Value,"'((?:''|[^'])*)'|\"((?:`.|[^\"])*)\"",RegexOptions.Singleline)){list.Add(x.Groups[1].Success?x.Groups[1].Value.Replace("''","'"):x.Groups[2].Value.Replace("`n","\n").Replace("`r","\r").Replace("`t","\t"));}if(list.Count>0)return(list,DetectFps(text));}
        throw new InvalidDataException("No encontré frames estáticos reconocibles. No se ejecuta PowerShell arbitrario.");
    }
    private static double DetectFps(string t){var m=Regex.Match(t,@"\$frameMs\s*=\s*1000(?:\.0)?\s*/\s*([0-9.]+)",RegexOptions.IgnoreCase);return m.Success&&double.TryParse(m.Groups[1].Value,System.Globalization.CultureInfo.InvariantCulture,out var f)?f:20;}
}
