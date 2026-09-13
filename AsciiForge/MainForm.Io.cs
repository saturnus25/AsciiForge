using System.Text;

namespace AsciiForge;

internal sealed partial class MainForm
{
    private void CopyFrame()
    {
        try
        {
            string frame = CurrentAsciiFrame();
            Clipboard.SetText(frame);
            _status.Text = Localization.English ? "ASCII frame copied" : "Frame ASCII copiado";
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "AsciiForge", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void SaveFrame()
    {
        using var dialog = new SaveFileDialog
        {
            Filter = Localization.English ? "Text|*.txt" : "Texto|*.txt",
            FileName = "asciiforge-frame.txt"
        };

        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        File.WriteAllText(dialog.FileName, CurrentAsciiFrame(), new UTF8Encoding(false));
    }

    private string CurrentAsciiFrame()
    {
        if (_importView.Visible && _importFrames.Count > 0)
            return _importFrames[Math.Clamp(_importIndex, 0, _importFrames.Count - 1)];

        return _preview.CaptureCurrentAsciiFrame();
    }

    private async Task ExportDialogAsync()
    {
        if (_exporting) return;

        using var dialog = new SaveFileDialog
        {
            Filter = Localization.English
                ? "PowerShell|*.ps1|HTML|*.html|JSON|*.json|C# standalone|*.cs|ANSI|*.ans|Text|*.txt"
                : "PowerShell|*.ps1|HTML|*.html|JSON|*.json|C# standalone|*.cs|ANSI|*.ans|Texto|*.txt",
            FileName = _settings.Effect.Replace(' ', '_').ToLowerInvariant() + ".ps1"
        };

        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        _exporting = true;
        _left.Enabled = false;
        _exportButton.Enabled = false;
        UseWaitCursor = true;

        try
        {
            var exportSettings = _settings.Clone();
            List<string> frames;

            if (_importView.Visible && _importFrames.Count > 0)
            {
                frames = new List<string>(_importFrames);
                exportSettings.Fps = Math.Max(1, (int)Math.Round(_importFps));
            }
            else
            {
                int frameCount = Math.Max(1, (int)Math.Round(exportSettings.Duration * exportSettings.Fps));
                frames = new List<string>(frameCount);

                for (int i = 0; i < frameCount; i++)
                {
                    frames.Add(_preview.CaptureAsciiFrame(i / (double)exportSettings.Fps));

                    if (i % 8 == 0)
                    {
                        _status.Text = Localization.English
                            ? $"Exporting {i + 1}/{frameCount}"
                            : $"Exportando {i + 1}/{frameCount}";
                        await Task.Yield();
                    }
                }
            }

            ExportService.Save(dialog.FileName, exportSettings, frames);
            _status.Text = Localization.English
                ? $"Exported {Path.GetFileName(dialog.FileName)} · {frames.Count} frames"
                : $"Exportado {Path.GetFileName(dialog.FileName)} · {frames.Count} frames";
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                ex.Message,
                Localization.English ? "Export" : "Exportación",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        finally
        {
            UseWaitCursor = false;
            _left.Enabled = true;
            _exportButton.Enabled = true;
            _exporting = false;
        }
    }

    private void ImportPs()
    {
        using var dialog = new OpenFileDialog
        {
            Filter = Localization.English ? "PowerShell|*.ps1|All files|*.*" : "PowerShell|*.ps1|Todos|*.*"
        };

        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        try
        {
            var (frames, fps) = PowerShellImport.Parse(File.ReadAllText(dialog.FileName, Encoding.UTF8));
            EnterImportedMode(frames, fps, Path.GetFileName(dialog.FileName));
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Import PowerShell", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void EnterImportedMode(List<string> frames, double fps, string source)
    {
        _importFrames = frames.Select(PowerShellImport.StripAnsi).ToList();
        _importFps = Math.Max(1, fps);
        _importIndex = -1;
        _importClock.Restart();
        _pauseButton.Text = Localization.Text("button.pause");
        _preview.Visible = false;
        _importView.Visible = true;
        _importView.BringToFront();

        int width = _importFrames.Max(frame => frame.Split('\n').Max(line => line.TrimEnd('\r').Length));
        int height = _importFrames.Max(frame => frame.Split('\n').Length);
        _status.Text = $"Imported · {source} · {_importFrames.Count} frames · {_importFps:0.##} FPS · {width}×{height}";
        UpdateImportedFrame();
    }

    private void ExitImportedMode()
    {
        if (!_importView.Visible) return;

        _importClock.Stop();
        _importView.Visible = false;
        _preview.Visible = true;
        _pauseButton.Text = _preview.Paused ? Localization.Text("button.resume") : Localization.Text("button.pause");
        _preview.BringToFront();
        _importFrames = [];
        _importIndex = -1;
    }

    private void UpdateImportedFrame()
    {
        if (!_importView.Visible || _importFrames.Count == 0) return;

        int index = (int)(_importClock.Elapsed.TotalSeconds * _importFps) % _importFrames.Count;
        if (index == _importIndex) return;

        _importIndex = index;
        int selection = _importView.SelectionStart;
        _importView.Text = _importFrames[index];
        _importView.SelectionStart = Math.Min(selection, _importView.TextLength);
        _status.Text = Localization.English
            ? $"Imported frames · {_importFrames.Count} · {_importFps:0.##} FPS · frame {index + 1}"
            : $"Frames importados · {_importFrames.Count} · {_importFps:0.##} FPS · frame {index + 1}";
    }
}
