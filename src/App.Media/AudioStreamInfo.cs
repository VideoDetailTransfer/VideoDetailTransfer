using VideoDetailTransfer.Core;

namespace VideoDetailTransfer.Media;

public sealed class AudioStreamInfo
{
    public int Index { get; init; }
    public string? CodecName { get; init; }          // "ac3", "aac"
    public string? CodecLongName { get; init; }

    public int SampleRate { get; init; }            // 48000
    public int Channels { get; init; }              // 2, 6
    public string? ChannelLayout { get; init; }     // "stereo", "5.1"

    public string? SampleFormat { get; init; }      // "fltp"
    public long? BitRate { get; init; }             // 192000, 224000
    public Rational TimeBase { get; init; }         // "1/48000" typically

    public double? StartTimeSeconds { get; init; }  // some containers have offsets
    public double? DurationSeconds { get; init; }   // if present
    public Dictionary<string, string>? Tags { get; init; } // language/title, etc.
}
