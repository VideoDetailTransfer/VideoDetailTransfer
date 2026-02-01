// MainViewModel.cs (minimal MVVM, no framework)
using Microsoft.Win32;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using VideoDetailTransfer.Core;
using VideoDetailTransfer.Persistence;
using VideoDetailTransfer.Media;

namespace VideoDetailTransfer.Ui.ViewModels;

public sealed class MainViewModel : INotifyPropertyChanged
{
    private readonly IVideoProbe _videoProbe;
    private readonly IProjectStore _projectStore;

    private Project _project;
    private string _statusText = "Ready.";

    public event PropertyChangedEventHandler? PropertyChanged;

    public MainViewModel(IVideoProbe videoProbe, IProjectStore projectStore)
    {
        _videoProbe = videoProbe;
        _projectStore = projectStore;

        _project = new Project
        {
            Info = new ProjectInfo("Untitled", DateTime.UtcNow, CoreConstants.CurrentSchemaVersion),
            Paths = new ProjectPaths()
        };

        OpenReferenceCommand = new AsyncRelayCommand(OpenReferenceAsync);
        OpenTargetCommand = new AsyncRelayCommand(OpenTargetAsync);
        SaveProjectCommand = new AsyncRelayCommand(SaveProjectAsync, () => CanSaveProject);
    }

    public string Title => "Video Detail Transfer";

    public ICommand OpenReferenceCommand { get; }
    public ICommand OpenTargetCommand { get; }
    public ICommand SaveProjectCommand { get; }

    public bool CanSaveProject =>
        !string.IsNullOrWhiteSpace(_project.Paths.ReferenceOriginalPath) &&
        !string.IsNullOrWhiteSpace(_project.Paths.TargetOriginalPath);

    public string StatusText
    {
        get => _statusText;
        private set { _statusText = value; OnPropertyChanged(); }
    }

    public string ReferencePath => _project.Paths.ReferenceOriginalPath;
    public string TargetPath => _project.Paths.TargetOriginalPath;

    public string ReferenceSummary => FormatDescriptor(_project.Videos.ReferenceOriginal, includeWarnings: CanSaveProject);
    public string TargetSummary => FormatDescriptor(_project.Videos.TargetOriginal, includeWarnings: CanSaveProject);

    private async Task OpenReferenceAsync()
    {
        string? path = PickVideoFile();
        if (path is null) return;

        StatusText = "Probing reference…";
        try
        {
            ProbeResult probe = await _videoProbe.ProbeAsync(path);
            VideoDescriptor descriptor = ProbeNormalizer.Normalize(path, probe);

            _project.Paths.ReferenceOriginalPath = path;
            _project.Videos.ReferenceOriginal = descriptor;

            OnPropertyChanged(nameof(ReferencePath));
            OnPropertyChanged(nameof(ReferenceSummary));
            OnPropertyChanged(nameof(CanSaveProject));
            (SaveProjectCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();

            StatusText = "Reference loaded.";
            RaiseCompatibilityWarningsIfReady();
        }
        catch (Exception ex)
        {
            StatusText = $"Reference probe failed: {ex.Message}";
        }
    }

    private async Task OpenTargetAsync()
    {
        string? path = PickVideoFile();
        if (path is null) return;

        StatusText = "Probing target…";
        try
        {
            ProbeResult probe = await _videoProbe.ProbeAsync(path);
            VideoDescriptor descriptor = ProbeNormalizer.Normalize(path, probe);

            _project.Paths.TargetOriginalPath = path;
            _project.Videos.TargetOriginal = descriptor;

            OnPropertyChanged(nameof(TargetPath));
            OnPropertyChanged(nameof(TargetSummary));
            OnPropertyChanged(nameof(CanSaveProject));
            (SaveProjectCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();

            StatusText = "Target loaded.";
            RaiseCompatibilityWarningsIfReady();
        }
        catch (Exception ex)
        {
            StatusText = $"Target probe failed: {ex.Message}";
        }
    }

    private async Task SaveProjectAsync()
    {
        // This is intentionally simple for commit #2:
        // Save alongside the reference file, under "<refname>.vdt.json"
        if (!CanSaveProject)
        {
            StatusText = "Open both videos first.";
            return;
        }

        string defaultPath = System.IO.Path.Combine(
            System.IO.Path.GetDirectoryName(_project.Paths.ReferenceOriginalPath) ?? Environment.CurrentDirectory,
            System.IO.Path.GetFileNameWithoutExtension(_project.Paths.ReferenceOriginalPath) + ".vdt.json");

        StatusText = "Saving project…";
        try
        {
            await _projectStore.SaveAsync(defaultPath, _project);
            StatusText = $"Saved: {defaultPath}";
        }
        catch (Exception ex)
        {
            StatusText = $"Save failed: {ex.Message}";
        }
    }

    private void RaiseCompatibilityWarningsIfReady()
    {
        if (_project.Videos.ReferenceOriginal is null || _project.Videos.TargetOriginal is null)
        {
            OnPropertyChanged(nameof(ReferenceSummary));
            OnPropertyChanged(nameof(TargetSummary));
            return;
        }

        // Refresh summaries so warnings show.
        OnPropertyChanged(nameof(ReferenceSummary));
        OnPropertyChanged(nameof(TargetSummary));
    }

    private string FormatDescriptor(VideoDescriptor? d, bool includeWarnings)
    {
        if (d is null) return "(not loaded)";

        StringBuilder sb = new StringBuilder();
        sb.AppendLine($"Path: {d.Path}");
        sb.AppendLine($"Stored: {d.StoredWidth}x{d.StoredHeight}");
        sb.AppendLine($"FPS: {d.FrameRate} ({d.FrameRate.ToDouble():0.###})");
        sb.AppendLine($"Interlaced: {(d.IsInterlaced ? "Yes" : "No")} {(!string.IsNullOrWhiteSpace(d.FieldOrder) ? $"({d.FieldOrder})" : "")}");
        sb.AppendLine($"SAR: {d.SampleAspectRatio}  DAR: {d.DisplayAspectRatio}");
        sb.AppendLine($"PixFmt: {d.PixelFormat}  BitDepth: {d.BitDepth}");
        if (!string.IsNullOrWhiteSpace(d.ColorSpace))
            sb.AppendLine($"Color: {d.ColorSpace} / {d.ColorPrimaries} / {d.ColorTransfer}");
        sb.AppendLine($"Duration: {d.Duration}");

        if (includeWarnings && _project.Videos.ReferenceOriginal is not null && _project.Videos.TargetOriginal is not null)
        {
            IReadOnlyList<string> warnings = VideoCompatibilityChecker.Check(_project.Videos.ReferenceOriginal, _project.Videos.TargetOriginal);
            if (warnings.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("Warnings:");
                foreach (string w in warnings)
                    sb.AppendLine($"  • {w}");
            }
        }

        return sb.ToString();
    }

    private static string? PickVideoFile()
    {
        OpenFileDialog dlg = new OpenFileDialog
        {
            Filter = "Video files|*.mkv;*.mp4;*.mov;*.m4v;*.avi;*.ts;*.m2ts|All files|*.*",
            Title = "Select a video file"
        };
        return dlg.ShowDialog() == true ? dlg.FileName : null;
    }

    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
