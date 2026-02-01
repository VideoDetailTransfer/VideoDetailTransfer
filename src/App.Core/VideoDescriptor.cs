namespace VideoDetailTransfer.Core;

public sealed record VideoDescriptor
(
    string Path,

    int StoredWidth,
    int StoredHeight,

    Rational FrameRate,

    bool IsInterlaced,
    string? FieldOrder,

    Rational SampleAspectRatio,
    Rational DisplayAspectRatio,

    string? PixelFormat,
    int BitDepth,

    string? ColorSpace,
    string? ColorPrimaries,
    string? ColorTransfer,

    TimeSpan Duration
);
