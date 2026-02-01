using Microsoft.Win32;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows.Input;
using VideoDetailTransfer.Core;
using VideoDetailTransfer.Media;
using VideoDetailTransfer.Persistence;
using VideoDetailTransfer.Ui.Infrastructure;

namespace VideoDetailTransfer.Ui.ViewModels;

public sealed class MainViewModel : INotifyPropertyChanged
{
    private readonly IProjectStore _projectStore;

    private IVideoProbe? _videoProbe;
    private Project _project;

    private string _statusText = "Select ffprobe.exe to begin.";
    private string _ffprobePath = "";

    public event PropertyChangedEventHandler? PropertyChanged;

    public MainViewModel(IProjectStore projectStore)
    {
        _projectStore = projectStore;

        _project = new Project
        {
            Info = new ProjectInfo("Untitled", DateTime.UtcNow, CoreConstants.CurrentSchemaVersion),
            Paths = new ProjectPaths()
        };

        BrowseFfmpegCommand = new RelayCommand(BrowseForFfprobe);

        OpenReferenceCommand = new AsyncRelayCommand(OpenReferenceAsync, () => CanProbe);
        OpenTargetCommand = new AsyncRelayCommand(OpenTargetAsync, () => CanProbe);
        SaveProjectCommand = new AsyncRelayCommand(SaveProjectAsync, () => CanSaveProject);
    }

    public string Title => "Video Detail Transfer";

    public ICommand BrowseFfmpegCommand { get; }
    public ICommand OpenReferenceCommand { get; }
    public ICommand OpenTargetCommand { get; }
    public ICommand SaveProjectCommand { get; }

    public string FfprobePath
    {
        get => _ffprobePath;
        set
        {
            if (_ffprobePath == value) return;
            _ffprobePath = value ?? "";
            OnPropertyChanged();

            // If user types/pastes a path, update probe availability.
            TryUpdateProbeFromPath(_ffprobePath);

            OnPropertyChanged(nameof(CanProbe));
            RaiseCommandCanExecuteChanges();

            if (CanProbe)
                StatusText = "Ready.";
            else if (!string.IsNullOrWhiteSpace(_ffprobePath))
                StatusText = "ffprobe.exe not found at that path.";
        }
    }

    public bool CanProbe => _videoProbe is not null;

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

    private void BrowseForFfprobe()
    {
        OpenFileDialog dlg = new OpenFileDialog
        {
            Title = "Select ffprobe.exe",
            Filter = "ffprobe.exe|ffprobe.exe|Executables|*.exe|All files|*.*"
        };

        if (dlg.ShowDialog() == true)
        {
            FfprobePath = dlg.FileName; // setter calls TryUpdateProbeFromPath
        }
    }

    private void TryUpdateProbeFromPath(string path)
    {
        if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
        {
            _videoProbe = new FfprobeVideoProbe(path);
        }
        else
        {
            _videoProbe = null;
        }
    }

    private async Task OpenReferenceAsync()
    {
        if (!CanProbe || _videoProbe is null)
        {
            StatusText = "Select ffprobe.exe first.";
            return;
        }

        string path = PickVideoFile();
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
            RaiseCommandCanExecuteChanges();

            StatusText = "Reference loaded.";
            RefreshWarnings();
        }
        catch (Exception ex)
        {
            StatusText = $"Reference probe failed: {ex.Message}";
        }
    }

    private async Task OpenTargetAsync()
    {
        if (!CanProbe || _videoProbe is null)
        {
            StatusText = "Select ffprobe.exe first.";
            return;
        }

        string path = PickVideoFile();
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
            RaiseCommandCanExecuteChanges();

            StatusText = "Target loaded.";
            RefreshWarnings();
        }
        catch (Exception ex)
        {
            StatusText = $"Target probe failed: {ex.Message}";
        }
    }

    private async Task SaveProjectAsync()
    {
        if (!CanSaveProject)
        {
            StatusText = "Open both videos first.";
            return;
        }

        string defaultPath = Path.Combine(
            Path.GetDirectoryName(_project.Paths.ReferenceOriginalPath) ?? Environment.CurrentDirectory,
            Path.GetFileNameWithoutExtension(_project.Paths.ReferenceOriginalPath) + ".vdt.json");

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

    private void RefreshWarnings()
    {
        OnPropertyChanged(nameof(ReferenceSummary));
        OnPropertyChanged(nameof(TargetSummary));
    }

    private void RaiseCommandCanExecuteChanges()
    {
        (OpenReferenceCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
        (OpenTargetCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
        (SaveProjectCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
        (BrowseFfmpegCommand as RelayCommand)?.RaiseCanExecuteChanged();
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

    private void OnPropertyChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
