using System.Diagnostics;
using System.Text.Json;

namespace AsciiForge;

internal sealed partial class MainForm : Form
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
    private readonly CheckBox _invert = new(), _color = new(), _exportCredit = new();
    private readonly Button _exportButton = new();
    private readonly Panel _generalParams = new(), _specificParams = new(), _paletteStops = new();
    private readonly Dictionary<string, ParameterRow> _paramRows = new(StringComparer.OrdinalIgnoreCase);
    private bool _applying;
    private bool _exporting;
    private readonly Button _pauseButton = new();
    private readonly ToolTip _tips = new() { InitialDelay = 500, ReshowDelay = 100, AutoPopDelay = 10000, ShowAlways = true };

    public MainForm()
    {
        Text = "AsciiForge 5.2.0 · C# GPU Edition";
        Width = 1500;
        Height = 940;
        MinimumSize = new Size(1120, 720);
        BackColor = Theme.Bg;
        ForeColor = Theme.Text;
        StartPosition = FormStartPosition.CenterScreen;

        BuildUi();
        PopulateData();
        ApplyPreset(_preset.Items.Count > 0 ? _preset.Items[0]!.ToString()! : "");

        _preview.Settings = _settings;
        _preview.TargetFps = _settings.Fps;
        _preview.FrameStats += (fps, ms, gpu) => BeginInvoke((Action)(() =>
        {
            _status.Text = $"{_settings.Effect} / {_settings.Preset} · {_settings.Width}×{_settings.Height} · " +
                           $"target {_settings.Fps} FPS · real {fps:0.0} · frame submit {ms:0.00} ms · DIRECT · {gpu}";
        }));
        _preview.SdfCameraChanged += (yaw, pitch, depth) =>
        {
            _settings.Set("sdf_yaw", yaw);
            _settings.Set("sdf_pitch", pitch);
            _settings.Set("sdf_depth", depth);
            if (_paramRows.TryGetValue("sdf_yaw", out var yawRow)) yawRow.SetValue(yaw);
            if (_paramRows.TryGetValue("sdf_pitch", out var pitchRow)) pitchRow.SetValue(pitch);
            if (_paramRows.TryGetValue("sdf_depth", out var depthRow)) depthRow.SetValue(depth);
            MarkCustom();
        };
    }

    private void BuildUi()
    {
        var split = new SplitContainer
        {
            Dock = DockStyle.Fill,
            SplitterDistance = 445,
            FixedPanel = FixedPanel.Panel1,
            BackColor = Theme.Bg
        };
        Controls.Add(split);

        _left.Dock = DockStyle.Fill;
        _left.AutoScroll = true;
        _left.WrapContents = false;
        _left.FlowDirection = FlowDirection.TopDown;
        _left.BackColor = Theme.Bg;
        _left.Padding = new Padding(8);
        split.Panel1.Controls.Add(_left);

        var right = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 2,
            ColumnCount = 1,
            BackColor = Theme.Bg,
            Padding = new Padding(6)
        };
        right.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        right.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        split.Panel2.Controls.Add(right);

        _status.Dock = DockStyle.Fill;
        _status.TextAlign = ContentAlignment.MiddleRight;
        _status.ForeColor = Theme.Text;
        _status.Text = Localization.Text("status.init");
        right.Controls.Add(_status, 0, 0);

        _preview.Dock = DockStyle.Fill;
        right.Controls.Add(_preview, 0, 1);

        _importView.Dock = DockStyle.Fill;
        _importView.Visible = false;
        _importView.ReadOnly = true;
        _importView.WordWrap = false;
        _importView.BackColor = Color.Black;
        _importView.ForeColor = Color.Gainsboro;
        _importView.Font = new Font("Cascadia Mono", 10);
        _importView.BorderStyle = BorderStyle.None;
        right.Controls.Add(_importView, 0, 1);
        _importView.BringToFront();
        _importTimer.Tick += (_, _) => UpdateImportedFrame();
        _importTimer.Start();

        var languageGroup = Group(Localization.Text("group.language"), 62);
        languageGroup.Tag = "group.language";
        SetupCombo(_language);
        _language.Items.AddRange(["Español", "English"]);
        _language.SelectedIndex = 0;
        _language.SetBounds(10, 25, 395, 28);
        languageGroup.Controls.Add(_language);
        _left.Controls.Add(languageGroup);

        var effectGroup = Group(Localization.Text("group.effect"), 126);
        effectGroup.Tag = "group.effect";
        SetupCombo(_effect);
        SetupCombo(_preset);
        _effect.SetBounds(10, 25, 395, 28);
        _preset.SetBounds(10, 62, 395, 28);
        effectGroup.Controls.AddRange([_effect, _preset]);

        var randomButton = Btn(Localization.Text("button.random"), (_, _) =>
        {
            _seed.Value = Random.Shared.Next(0, int.MaxValue);
        });
        randomButton.Tag = "button.random";
        randomButton.SetBounds(10, 95, 92, 26);
        effectGroup.Controls.Add(randomButton);

        var restartButton = Btn(Localization.Text("button.restart"), (_, _) =>
        {
            _preview.RestartAnimation();
            if (_importView.Visible)
            {
                _importClock.Restart();
                _importIndex = -1;
            }
        });
        restartButton.Tag = "button.restart";
        restartButton.SetBounds(107, 95, 92, 26);
        effectGroup.Controls.Add(restartButton);

        _pauseButton.Text = Localization.Text("button.pause");
        Theme.Button(_pauseButton);
        _pauseButton.Tag = "button.pause";
        _pauseButton.SetBounds(204, 95, 92, 26);
        _pauseButton.Click += (_, _) => TogglePause();
        effectGroup.Controls.Add(_pauseButton);

        var benchmarkButton = Btn(Localization.Text("button.benchmark"), (_, _) => Benchmark());
        benchmarkButton.Tag = "button.benchmark";
        benchmarkButton.SetBounds(301, 95, 104, 26);
        effectGroup.Controls.Add(benchmarkButton);
        _left.Controls.Add(effectGroup);

        var outputGroup = Group(Localization.Text("group.output"), 244);
        outputGroup.Tag = "group.output";
        SetupNumeric(_width, 16, 1000, 178);
        SetupNumeric(_height, 8, 500, 50);
        SetupNumeric(_fps, 1, 240, 30);
        SetupNumeric(_duration, 0.1m, 600, 6, 1);
        SetupNumeric(_seed, 0, int.MaxValue, 1337);
        AddNumericRow(outputGroup, "label.width", _width, 25);
        AddNumericRow(outputGroup, "label.height", _height, 53);
        AddNumericRow(outputGroup, "FPS", _fps, 81);
        AddNumericRow(outputGroup, "label.duration", _duration, 109);
        AddNumericRow(outputGroup, "Seed", _seed, 137);

        _exportCredit.Text = Localization.Text("check.credit");
        _exportCredit.Checked = true;
        _exportCredit.ForeColor = Theme.Text;
        _exportCredit.BackColor = Theme.Panel;
        _exportCredit.Tag = "check.credit";
        _exportCredit.SetBounds(10, 166, 395, 24);
        outputGroup.Controls.Add(_exportCredit);

        _exportButton.Text = Localization.Text("button.export") + "  (Ctrl+E)";
        Theme.Button(_exportButton);
        _exportButton.Tag = "button.export";
        _exportButton.Font = new Font(Font, FontStyle.Bold);
        _exportButton.SetBounds(10, 198, 395, 34);
        _exportButton.Click += (_, _) => _ = ExportDialogAsync();
        outputGroup.Controls.Add(_exportButton);
        _left.Controls.Add(outputGroup);

        var generalGroup = Group(Localization.Text("group.general"), ParameterCatalog.General.Length * 34 + 30);
        generalGroup.Tag = "group.general";
        _generalParams.SetBounds(10, 23, 395, generalGroup.Height - 28);
        _generalParams.BackColor = Theme.Panel;
        generalGroup.Controls.Add(_generalParams);
        _left.Controls.Add(generalGroup);
        foreach (var description in ParameterCatalog.General) AddParamRow(_generalParams, description);

        var specificGroup = Group(Localization.Text("group.specific"), 80);
        specificGroup.Tag = "group.specific";
        specificGroup.Name = "specificGroup";
        _specificParams.SetBounds(10, 23, 395, 52);
        _specificParams.BackColor = Theme.Panel;
        specificGroup.Controls.Add(_specificParams);
        _left.Controls.Add(specificGroup);

        var charsetGroup = Group(Localization.Text("group.charset"), 142);
        charsetGroup.Tag = "group.charset";
        SetupCombo(_charsetPreset);
        _charsetPreset.SetBounds(10, 25, 395, 28);
        Theme.TextBox(_charset);
        _charset.SetBounds(10, 60, 395, 28);
        _invert.Text = Localization.Text("check.invert");
        _invert.ForeColor = Theme.Text;
        _invert.BackColor = Theme.Panel;
        _invert.SetBounds(10, 94, 170, 26);
        _invert.Tag = "check.invert";
        charsetGroup.Controls.AddRange([_charsetPreset, _charset, _invert]);

        var copyButton = Btn(Localization.Text("button.copy"), (_, _) => CopyFrame());
        copyButton.Tag = "button.copy";
        copyButton.SetBounds(270, 94, 135, 27);
        charsetGroup.Controls.Add(copyButton);
        _left.Controls.Add(charsetGroup);

        var colorGroup = Group(Localization.Text("group.color"), 198);
        colorGroup.Tag = "group.color";
        SetupCombo(_palette);
        _palette.SetBounds(10, 25, 395, 28);
        _color.Text = Localization.Text("check.color");
        _color.Checked = true;
        _color.ForeColor = Theme.Text;
        _color.BackColor = Theme.Panel;
        _color.Tag = "check.color";
        _color.SetBounds(10, 58, 210, 26);
        _paletteStops.SetBounds(10, 89, 395, 62);
        _paletteStops.BackColor = Theme.Panel;
        _paletteStops.AutoScroll = true;
        colorGroup.Controls.AddRange([_palette, _color, _paletteStops]);

        var addColorButton = Btn(Localization.Text("button.addcolor"), (_, _) => AddPaletteStop());
        addColorButton.Tag = "button.addcolor";
        addColorButton.SetBounds(10, 158, 105, 28);
        var importButton = Btn(Localization.Text("button.import"), (_, _) => ImportPs());
        importButton.Tag = "button.import";
        importButton.SetBounds(121, 158, 135, 28);
        var saveFrameButton = Btn(Localization.Text("button.save"), (_, _) => SaveFrame());
        saveFrameButton.Tag = "button.save";
        saveFrameButton.SetBounds(262, 158, 143, 28);
        colorGroup.Controls.AddRange([addColorButton, importButton, saveFrameButton]);
        _left.Controls.Add(colorGroup);

        var shapeGroup = Group(Localization.Text("group.shape"), 70);
        shapeGroup.Tag = "group.shape";
        SetupCombo(_shape);
        _shape.Items.AddRange(["Square", "Diamond", "Star", "Hex", "Cross"]);
        _shape.SelectedIndex = 0;
        _shape.SetBounds(10, 28, 395, 28);
        shapeGroup.Controls.Add(_shape);
        _left.Controls.Add(shapeGroup);

        _tips.SetToolTip(randomButton, Localization.Tip("button.random"));
        _tips.SetToolTip(restartButton, Localization.Tip("button.restart"));
        _tips.SetToolTip(_pauseButton, Localization.Tip("button.pause"));
        _tips.SetToolTip(benchmarkButton, Localization.Tip("button.benchmark"));
        _tips.SetToolTip(addColorButton, Localization.Tip("button.addcolor"));
        _tips.SetToolTip(importButton, Localization.Tip("button.import"));
        _tips.SetToolTip(saveFrameButton, Localization.Tip("button.save"));

        WireEvents();
        ApplyStaticTips();
        KeyPreview = true;
        KeyDown += HandleShortcutKeyDown;
    }

    private void HandleShortcutKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Space)
        {
            TogglePause();
            e.SuppressKeyPress = true;
        }
        else if (e.KeyCode == Keys.F5)
        {
            _preview.RestartAnimation();
        }
        else if (e.Control && e.KeyCode == Keys.E)
        {
            _ = ExportDialogAsync();
            e.SuppressKeyPress = true;
        }
    }

    private void PopulateData()
    {
        _effect.Items.AddRange(_data.Presets.Keys.Cast<object>().ToArray());
        if (_effect.Items.Count > 0) _effect.SelectedIndex = 0;
        UpdateEffectTip();

        _charsetPreset.Items.AddRange(_data.Charsets.Keys.Cast<object>().ToArray());
        if (_charsetPreset.Items.Count > 0) _charsetPreset.SelectedIndex = 0;

        _palette.Items.AddRange(_data.Palettes.Keys.Cast<object>().ToArray());
        if (_palette.Items.Count > 0) _palette.SelectedItem = "Plasma";
    }

    private void WireEvents()
    {
        _language.SelectedIndexChanged += (_, _) =>
        {
            Localization.English = _language.SelectedIndex == 1;
            ApplyLanguage();
        };

        _effect.SelectedIndexChanged += (_, _) =>
        {
            if (_applying) return;
            ExitImportedMode();
            _settings.Effect = _effect.Text;
            UpdateEffectTip();
            RebuildPresets();
            RebuildSpecific();
        };

        _preset.SelectedIndexChanged += (_, _) =>
        {
            if (!_applying && _preset.SelectedItem is not null) ApplyPreset(_preset.Text);
        };

        _charsetPreset.SelectedIndexChanged += (_, _) =>
        {
            if (_applying) return;
            if (!_data.Charsets.TryGetValue(_charsetPreset.Text, out string? ramp)) return;
            _charset.Text = ramp;
            _settings.CharsetName = _charsetPreset.Text;
            _settings.Charset = ramp;
            Push();
        };

        _charset.TextChanged += (_, _) =>
        {
            if (_applying) return;
            _settings.Charset = _charset.Text.Length == 0 ? " " : _charset.Text;
            Push();
        };

        _invert.CheckedChanged += (_, _) =>
        {
            _settings.Invert = _invert.Checked;
            Push();
        };
        _color.CheckedChanged += (_, _) =>
        {
            _settings.ColorEnabled = _color.Checked;
            Push();
        };
        _exportCredit.CheckedChanged += (_, _) =>
        {
            _settings.IncludeExportCredit = _exportCredit.Checked;
        };

        _palette.SelectedIndexChanged += (_, _) =>
        {
            if (_applying) return;
            ApplyPalette(_palette.Text);
        };
        _shape.SelectedIndexChanged += (_, _) =>
        {
            if (_applying) return;
            _settings.ShapeMode = _shape.Text;
            MarkCustom();
            Push();
        };

        _width.ValueChanged += (_, _) =>
        {
            _settings.Width = (int)_width.Value;
            Push();
        };
        _height.ValueChanged += (_, _) =>
        {
            _settings.Height = (int)_height.Value;
            Push();
        };
        _fps.ValueChanged += (_, _) =>
        {
            _settings.Fps = (int)_fps.Value;
            _preview.TargetFps = _settings.Fps;
            Push();
        };
        _duration.ValueChanged += (_, _) => _settings.Duration = (double)_duration.Value;
        _seed.ValueChanged += (_, _) =>
        {
            _settings.Seed = (int)_seed.Value;
            Push();
        };
    }

    private void TogglePause()
    {
        if (_importView.Visible)
        {
            if (_importClock.IsRunning)
            {
                _importClock.Stop();
                _pauseButton.Text = Localization.Text("button.resume");
            }
            else
            {
                _importClock.Start();
                _pauseButton.Text = Localization.Text("button.pause");
            }
            return;
        }

        _preview.TogglePause();
        _pauseButton.Text = _preview.Paused
            ? Localization.Text("button.resume")
            : Localization.Text("button.pause");
    }

    private void RebuildPresets()
    {
        _applying = true;
        _preset.Items.Clear();
        if (_data.Presets.TryGetValue(_settings.Effect, out var group))
            _preset.Items.AddRange(group.Keys.Cast<object>().ToArray());
        _applying = false;

        if (_preset.Items.Count > 0)
        {
            _preset.SelectedIndex = 0;
            ApplyPreset(_preset.Text);
        }
        RebuildSpecific();
    }

    private void ApplyPreset(string name)
    {
        if (string.IsNullOrEmpty(name) ||
            !_data.Presets.TryGetValue(_effect.Text, out var group) ||
            !group.TryGetValue(name, out var preset))
            return;

        _applying = true;
        _settings.Effect = _effect.Text;
        _settings.Preset = name;
        _settings.ResetValues();

        foreach (var item in preset)
        {
            if (item.Value.ValueKind == JsonValueKind.Number && item.Value.TryGetDouble(out double value))
            {
                _settings.Set(item.Key, value);
            }
            else if (item.Key.Equals("palette", StringComparison.OrdinalIgnoreCase) && item.Value.ValueKind == JsonValueKind.String)
            {
                _settings.PaletteName = item.Value.GetString() ?? "Monochrome";
            }
            else if (item.Key.Equals("shape_mode", StringComparison.OrdinalIgnoreCase) && item.Value.ValueKind == JsonValueKind.String)
            {
                _settings.ShapeMode = item.Value.GetString() ?? "Square";
            }
        }

        foreach (var row in _paramRows)
            row.Value.SetValue(_settings.Get(row.Key));

        ApplyPalette(_settings.PaletteName, false);
        _shape.SelectedItem = _settings.ShapeMode;
        _applying = false;
        RebuildSpecific();
        Push();
    }

    private void RebuildSpecific()
    {
        _specificParams.Controls.Clear();
        var specificKeys = ParameterCatalog.Specific.Values
            .SelectMany(group => group)
            .Select(parameter => parameter.Key)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (string key in _paramRows.Keys.Where(specificKeys.Contains).ToList())
            _paramRows.Remove(key);

        if (!ParameterCatalog.Specific.TryGetValue(_settings.Effect, out var descriptions))
        {
            var label = new Label
            {
                Text = Localization.Text("specific.none"),
                ForeColor = Theme.Muted,
                AutoSize = true,
                Top = 8,
                Left = 2
            };
            _specificParams.Controls.Add(label);
            ResizeSpecificGroup(75);
            return;
        }

        int y = 0;
        foreach (var description in descriptions)
        {
            var row = CreateParamRow(description);
            row.Top = y;
            _specificParams.Controls.Add(row);
            y += 34;
        }
        ResizeSpecificGroup(y + 30);
    }

    private void ResizeSpecificGroup(int height)
    {
        var group = _left.Controls.Cast<Control>().FirstOrDefault(control => control.Name == "specificGroup");
        if (group is null) return;
        group.Height = Math.Max(65, height);
        _specificParams.Height = group.Height - 28;
    }

    private void AddParamRow(Panel panel, ParamDesc description)
    {
        var row = CreateParamRow(description);
        row.Top = panel.Controls.Count * 34;
        panel.Controls.Add(row);
    }

    private ParameterRow CreateParamRow(ParamDesc description)
    {
        var row = new ParameterRow(description, _settings.Get(description.Key))
        {
            Left = 0,
            Width = 390,
            Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top
        };
        row.ValueChanged += value =>
        {
            _settings.Set(description.Key, value);
            MarkCustom();
            Push();
        };
        _paramRows[description.Key] = row;
        return row;
    }

    private void ApplyPalette(string name, bool push = true)
    {
        if (!_data.Palettes.TryGetValue(name, out var stops)) return;
        _settings.PaletteName = name;
        _settings.PaletteStops = new List<string>(stops);
        _applying = true;
        _palette.SelectedItem = name;
        _applying = false;
        RebuildPaletteStops();
        if (push) Push();
    }

    private void RebuildPaletteStops()
    {
        _paletteStops.Controls.Clear();
        int x = 2;
        for (int i = 0; i < _settings.PaletteStops.Count; i++)
        {
            int index = i;
            var button = new Button
            {
                Left = x,
                Top = 6,
                Width = 42,
                Height = 42,
                BackColor = ParseColor(_settings.PaletteStops[i]),
                FlatStyle = FlatStyle.Flat,
                Tag = index
            };
            button.FlatAppearance.BorderColor = Color.Gray;
            button.Click += (_, _) => EditPaletteStop(index);
            _tips.SetToolTip(
                button,
                Localization.English
                    ? "Click to change this gradient color."
                    : "Haz clic para cambiar este color del gradiente.");
            _paletteStops.Controls.Add(button);
            x += 46;
        }
    }

    private void EditPaletteStop(int index)
    {
        using var dialog = new ColorDialog
        {
            Color = ParseColor(_settings.PaletteStops[index]),
            FullOpen = true
        };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        EnsureCustomPalette();
        _settings.PaletteStops[index] = ColorTranslator.ToHtml(dialog.Color);
        RebuildPaletteStops();
        Push();
    }

    private void AddPaletteStop()
    {
        using var dialog = new ColorDialog { Color = Color.White, FullOpen = true };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        EnsureCustomPalette();
        _settings.PaletteStops.Add(ColorTranslator.ToHtml(dialog.Color));
        RebuildPaletteStops();
        Push();
    }

    private void EnsureCustomPalette()
    {
        if (_settings.PaletteName.Equals("Custom", StringComparison.OrdinalIgnoreCase)) return;
        _settings.PaletteName = "Custom";
        if (!_palette.Items.Contains("Custom")) _palette.Items.Add("Custom");
        _applying = true;
        _palette.SelectedItem = "Custom";
        _applying = false;
    }

    private void MarkCustom()
    {
        if (_applying) return;
        _settings.Preset = "Custom";
        if (!_preset.Items.Contains("Custom")) _preset.Items.Add("Custom");
        _applying = true;
        _preset.SelectedItem = "Custom";
        _applying = false;
    }

    private void Push()
    {
        _preview.Settings = _settings.Clone();
        _preview.TargetFps = _settings.Fps;
    }

    private void Benchmark()
    {
        try
        {
            double ms = _preview.BenchmarkGpu();
            string message = Localization.English
                ? $"GPU direct preview\n\n{_preview.GpuInfo}\n{_settings.Width}×{_settings.Height} ASCII over {_preview.Width}×{_preview.Height} px\n\n{ms:0.000} ms/frame GPU+driver\n~{1000.0 / ms:0} theoretical FPS (without target limit)"
                : $"GPU direct preview\n\n{_preview.GpuInfo}\n{_settings.Width}×{_settings.Height} ASCII sobre {_preview.Width}×{_preview.Height} px\n\n{ms:0.000} ms/frame GPU+driver\n~{1000.0 / ms:0} FPS teóricos (sin límite de target)";
            MessageBox.Show(message, "Benchmark", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Benchmark", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

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
            if (ReferenceEquals(c, _exportButton)) c.Text += "  (Ctrl+E)";
            string tip=Localization.Tip(key); if(!string.IsNullOrEmpty(tip)) _tips.SetToolTip(c,tip);
        }
        foreach (Control child in c.Controls) ApplyLanguageRecursive(child);
    }

    private void ApplyStaticTips()
    {
        bool english = Localization.English;
        _tips.SetToolTip(
            _effect,
            english ? "Choose the kind of animation you want to generate." : "Elige qué tipo de animación quieres generar.");
        _tips.SetToolTip(
            _preset,
            english ? "Choose a prepared setup for the current effect." : "Elige una configuración preparada para el efecto actual.");
        _tips.SetToolTip(
            _width,
            english ? "Changes how many characters are used in each row." : "Cambia cuántos caracteres hay en cada fila.");
        _tips.SetToolTip(
            _height,
            english ? "Changes how many rows of characters are used." : "Cambia cuántas filas de caracteres hay.");
        _tips.SetToolTip(
            _fps,
            english ? "Sets the target frames per second for the preview." : "Marca cuántos frames por segundo intenta mostrar la vista previa.");
        _tips.SetToolTip(
            _duration,
            english ? "Sets how long the exported animation lasts." : "Marca cuánto dura la animación al exportarla.");
        _tips.SetToolTip(
            _seed,
            english ? "Changes the random variation without changing the effect type." : "Cambia la variación aleatoria sin cambiar el tipo de efecto.");
        _tips.SetToolTip(
            _charsetPreset,
            english ? "Choose a prepared character ramp." : "Elige una rampa de caracteres preparada.");
        _tips.SetToolTip(
            _charset,
            english
                ? "Characters on the left represent dark areas and characters on the right represent bright areas."
                : "Los caracteres de la izquierda representan zonas oscuras y los de la derecha zonas claras.");
        _tips.SetToolTip(
            _invert,
            english ? "Swaps which characters are used for bright and dark areas." : "Intercambia qué caracteres se usan para zonas claras y oscuras.");
        _tips.SetToolTip(
            _palette,
            english ? "Choose the colors used from dark areas to bright areas." : "Elige los colores usados desde las zonas oscuras hasta las claras.");
        _tips.SetToolTip(
            _color,
            english ? "Turns color on or off without removing the ASCII art." : "Activa o desactiva el color sin quitar el arte ASCII.");
        _tips.SetToolTip(_exportCredit, Localization.Tip("check.credit"));
        _tips.SetToolTip(
            _exportButton,
            Localization.Tip("button.export") + (english ? " Shortcut: Ctrl+E." : " Atajo: Ctrl+E."));
        _tips.SetToolTip(
            _shape,
            english ? "Choose the shape used by Spinning Shapes." : "Elige la figura usada por Spinning Shapes.");
    }

    private void UpdateEffectTip()
    {
        _tips.SetToolTip(_effect, Localization.EffectHelp(_effect.Text));
    }

    private GroupBox Group(string title, int height)
    {
        return new GroupBox
        {
            Text = title,
            Width = 415,
            Height = height,
            ForeColor = Theme.Text,
            BackColor = Theme.Panel,
            Margin = new Padding(0, 0, 0, 7)
        };
    }

    private Button Btn(string text, EventHandler click)
    {
        var button = new Button { Text = text };
        Theme.Button(button);
        button.Click += click;
        return button;
    }

    private static void SetupCombo(ComboBox combo)
    {
        combo.DropDownStyle = ComboBoxStyle.DropDownList;
        combo.DrawMode = DrawMode.OwnerDrawFixed;
        combo.ItemHeight = 22;
        Theme.Combo(combo);
        combo.DrawItem += (_, e) =>
        {
            if (e.Index < 0) return;
            bool selected = (e.State & DrawItemState.Selected) != 0;
            using var background = new SolidBrush(selected ? Color.FromArgb(62, 62, 62) : Theme.Input);
            using var foreground = new SolidBrush(Theme.Text);
            e.Graphics.FillRectangle(background, e.Bounds);
            e.Graphics.DrawString(
                combo.Items[e.Index]?.ToString() ?? string.Empty,
                combo.Font,
                foreground,
                e.Bounds.Left + 3,
                e.Bounds.Top + 3);
        };
    }

    private static void SetupNumeric(
        NumericUpDown numeric,
        decimal min,
        decimal max,
        decimal value,
        int decimals = 0)
    {
        numeric.Minimum = min;
        numeric.Maximum = max;
        numeric.Value = Math.Clamp(value, min, max);
        numeric.DecimalPlaces = decimals;
        numeric.Increment = decimals > 0 ? 0.1m : 1m;
        Theme.Numeric(numeric);
    }

    private static void AddNumericRow(Control parent, string labelKey, NumericUpDown numeric, int y)
    {
        bool localized = labelKey.StartsWith("label.");
        var label = new Label
        {
            Text = localized ? Localization.Text(labelKey) : labelKey,
            Tag = localized ? labelKey : null,
            Left = 10,
            Top = y + 3,
            Width = 180,
            Height = 24,
            ForeColor = Theme.Text
        };
        numeric.SetBounds(280, y, 125, 25);
        parent.Controls.Add(label);
        parent.Controls.Add(numeric);
    }

    private static Color ParseColor(string value)
    {
        try { return ColorTranslator.FromHtml(value); }
        catch { return Color.White; }
    }

}
