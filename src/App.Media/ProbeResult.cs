using System.Text.Json.Serialization;

namespace VideoDetailTransfer.Media;

public sealed class ProbeResult
{
    [JsonPropertyName("streams")]
    public List<StreamInfo> Streams { get; init; } = new();

    [JsonPropertyName("format")]
    public FormatInfo? Format { get; init; }
}

public sealed class StreamInfo
{
    [JsonPropertyName("index")]
    public int Index { get; init; }

    [JsonPropertyName("codec_type")]
    public string? CodecType { get; init; } // "video" or "audio"

    // Video-ish
    [JsonPropertyName("codec_name")]
    public string? CodecName { get; init; }

    [JsonPropertyName("width")]
    public int? Width { get; init; }

    [JsonPropertyName("height")]
    public int? Height { get; init; }

    [JsonPropertyName("pix_fmt")]
    public string? PixFmt { get; init; }

    [JsonPropertyName("sample_aspect_ratio")]
    public string? SampleAspectRatio { get; init; }

    [JsonPropertyName("display_aspect_ratio")]
    public string? DisplayAspectRatio { get; init; }

    [JsonPropertyName("field_order")]
    public string? FieldOrder { get; init; }

    [JsonPropertyName("avg_frame_rate")]
    public string? AvgFrameRate { get; init; }

    [JsonPropertyName("r_frame_rate")]
    public string? RFrameRate { get; init; }

    [JsonPropertyName("color_space")]
    public string? ColorSpace { get; init; }

    [JsonPropertyName("color_transfer")]
    public string? ColorTransfer { get; init; }

    [JsonPropertyName("color_primaries")]
    public string? ColorPrimaries { get; init; }

    // Audio-ish
    [JsonPropertyName("sample_rate")]
    public string? SampleRate { get; init; } // ffprobe often provides as string

    [JsonPropertyName("channels")]
    public int? Channels { get; init; }

    [JsonPropertyName("channel_layout")]
    public string? ChannelLayout { get; init; }

    [JsonPropertyName("bit_rate")]
    public string? BitRate { get; init; }
}
