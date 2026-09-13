namespace AsciiForge;

internal sealed class SafeComboBox : ComboBox
{
    private const int WM_MOUSEWHEEL = 0x020A;
    protected override void WndProc(ref Message m)
    {
        if (m.Msg == WM_MOUSEWHEEL && !DroppedDown)
        {
            int delta = unchecked((short)((long)m.WParam >> 16));
            Control? c = Parent;
            while (c is not null && c is not ScrollableControl) c = c.Parent;
            if (c is ScrollableControl sc)
            {
                int current = -sc.AutoScrollPosition.Y;
                sc.AutoScrollPosition = new Point(0, Math.Max(0, current - Math.Sign(delta) * 80));
            }
            return;
        }
        base.WndProc(ref m);
    }
}

internal sealed class ParameterRow : UserControl
{
    private readonly TrackBar _track = new();
    private readonly TextBox _text = new();
    private readonly ParamDesc _desc;
    private readonly ToolTip _tip = new() { InitialDelay = 450, ReshowDelay = 100, AutoPopDelay = 10000, ShowAlways = true };
    private bool _sync;
    public event Action<double>? ValueChanged;
    public double Value { get; private set; }

    public ParameterRow(ParamDesc desc, double value)
    {
        desc = Localization.Param(desc);
        _desc = desc; Height = 34; Width = 390; Dock = DockStyle.None; BackColor = Theme.Panel; ForeColor = Theme.Text;
        var label = new Label { Text = desc.Label, AutoSize = false, TextAlign = ContentAlignment.MiddleLeft, Left = 0, Top = 4, Width = 125, Height = 26, ForeColor = Theme.Text };
        _track.Left = 127; _track.Top = 2; _track.Width = 180; _track.Height = 30; _track.Minimum = 0; _track.Maximum = 1000; _track.TickStyle = TickStyle.None; _track.BackColor = Theme.Panel;
        _text.Left = 312; _text.Top = 5; _text.Width = 78; _text.Height = 24; Theme.TextBox(_text);
        Controls.Add(label); Controls.Add(_track); Controls.Add(_text);
        if (!string.IsNullOrWhiteSpace(desc.Help))
        {
            _tip.SetToolTip(this, desc.Help);
            _tip.SetToolTip(label, desc.Help);
            _tip.SetToolTip(_track, desc.Help);
            _tip.SetToolTip(_text, desc.Help);
        }
        _track.ValueChanged += (_, _) => { if (_sync) return; double v = desc.Min + (_track.Value / 1000.0) * (desc.Max - desc.Min); SetInternal(v, true); };
        _text.KeyDown += (_, e) => { if (e.KeyCode == Keys.Enter) { CommitText(); e.SuppressKeyPress = true; } };
        _text.Leave += (_, _) => CommitText();
        Resize += (_, _) => { _text.Left = Width - 80; _track.Width = Math.Max(40, _text.Left - _track.Left - 5); };
        SetValue(value, false);
    }

    public void SetValue(double value, bool notify = false)
    {
        SetInternal(value, notify);
    }

    private void SetInternal(double value, bool notify)
    {
        if (!double.IsFinite(value)) return;
        _sync = true; Value = value;
        double clipped = Math.Clamp(value, _desc.Min, _desc.Max);
        _track.Value = (int)Math.Round((clipped - _desc.Min) / Math.Max(1e-12, _desc.Max - _desc.Min) * 1000.0);
        if (!_text.Focused) _text.Text = _desc.Digits == 0 ? value.ToString("0") : value.ToString("0." + new string('#', Math.Max(1, _desc.Digits)), System.Globalization.CultureInfo.InvariantCulture);
        _sync = false;
        if (notify) ValueChanged?.Invoke(value);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _tip.Dispose();
        base.Dispose(disposing);
    }

    private void CommitText()
    {
        string s = _text.Text.Trim().Replace(',', '.');
        if (double.TryParse(s, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double v) && double.IsFinite(v)) SetInternal(v, true);
        else { System.Media.SystemSounds.Beep.Play(); SetInternal(Value, false); }
    }
}

internal static class Theme
{
    public static readonly Color Bg = Color.FromArgb(15, 15, 15);
    public static readonly Color Panel = Color.FromArgb(24, 24, 24);
    public static readonly Color Input = Color.FromArgb(34, 34, 34);
    public static readonly Color Border = Color.FromArgb(58, 58, 58);
    public static readonly Color Text = Color.FromArgb(235, 235, 235);
    public static readonly Color Muted = Color.FromArgb(170, 170, 170);

    public static void TextBox(TextBox x) { x.BackColor = Input; x.ForeColor = Text; x.BorderStyle = BorderStyle.FixedSingle; }
    public static void Combo(ComboBox x) { x.BackColor = Input; x.ForeColor = Text; x.FlatStyle = FlatStyle.Flat; }
    public static void Button(Button b) { b.BackColor = Color.FromArgb(43,43,43); b.ForeColor = Text; b.FlatStyle = FlatStyle.Flat; b.FlatAppearance.BorderColor = Border; }
    public static void Numeric(NumericUpDown n) { n.BackColor = Input; n.ForeColor = Text; n.BorderStyle = BorderStyle.FixedSingle; }
}
