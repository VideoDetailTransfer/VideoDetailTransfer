namespace VideoDetailTransfer.Media;

public sealed class FormatInfo
{
    public string? Filename { get; init; }               // from ffprobe "filename"
    public string? FormatName { get; init; }             // "matroska,webm" / "mov,mp4,..."
    public string? FormatLongName { get; init; }         // friendly name

    public double? StartTimeSeconds { get; init; }       // "start_time"
    public double? DurationSeconds { get; init; }        // "duration"

    public long? SizeBytes { get; init; }                // "size"
    public long? BitRate { get; init; }                  // "bit_rate"

    public int? ProbeScore { get; init; }                // "probe_score"
    public int? StreamCount { get; init; }               // "nb_streams"

    public Dictionary<string, string>? Tags { get; init; } // "tags" (encoder, major_brand, etc.)
}
