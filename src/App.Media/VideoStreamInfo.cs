using VideoDetailTransfer.Core;

namespace VideoDetailTransfer.Media;

public sealed class VideoStreamInfo
{
    public int Index { get; init; }
    public string Codec { get; init; }
    public int Width { get; init; }
    public int Height { get; init; }
    public string PixelFormat { get; init; }
    public string SampleAspectRatio { get; init; }
    public string DisplayAspectRatio { get; init; }
    public string FieldOrder { get; init; }
    public Rational AvgFrameRate { get; init; }
}
