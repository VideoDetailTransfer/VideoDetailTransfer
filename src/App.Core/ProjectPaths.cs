namespace VideoDetailTransfer.Core;

public sealed class ProjectPaths
{
    // User-selected inputs (can be absolute)
    public string ReferenceOriginalPath { get; set; } = "";
    public string TargetOriginalPath { get; set; } = "";

    // Project root / working directory
    // Convention: workDir lives next to the project file, e.g. <projectRoot>\work
    public string WorkDir { get; set; } = "work";

    // Derived artifacts (usually relative to WorkDir)
    public string ReferenceCanonicalPath { get; set; } = @"work\ref_canonical.mkv";

    public string ReferenceMonoAudioPath { get; set; } = @"work\audio\ref_mono.wav";
    public string TargetMonoAudioPath { get; set; } = @"work\audio\tgt_mono.wav";

    public string MapsDir { get; set; } = @"work\maps";
    public string ShotCacheDir { get; set; } = @"work\shotcache";
    public string ThumbsDir { get; set; } = @"work\thumbs";

    // Output
    public string OutputVideoPath { get; set; } = @"output\combined.mp4";
}
