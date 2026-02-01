using System.Globalization;
using VideoDetailTransfer.Core;

namespace VideoDetailTransfer.Media;

public static class ProbeNormalizer
{
    public static VideoDescriptor Normalize(string path, ProbeResult probe)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("path is null/empty.", nameof(path));
        ArgumentNullException.ThrowIfNull(probe);

        StreamInfo video = PickBestVideoStream(probe)
            ?? throw new InvalidOperationException("ffprobe did not return a video stream.");

        // Stored raster (the actual coded frame dimensions)
        int storedW = video.Width ?? 0;
        int storedH = video.Height ?? 0;
        if (storedW <= 0 || storedH <= 0)
            throw new InvalidOperationException("Video stream width/height missing or invalid.");

        // Frame rate: prefer avg_frame_rate, fall back to r_frame_rate
        Rational fps = ParseRationalOrInvalid(video.AvgFrameRate)
                  .OrElse(ParseRationalOrInvalid(video.RFrameRate));
        if (!fps.IsValid)
            fps = new Rational(0, 0); // invalid; keep but don't crash

        // SAR & DAR
        Rational sar = ParseRationalOrDefault(video.SampleAspectRatio, defaultValue: new Rational(1, 1));
        Rational dar = ParseRationalOrInvalid(video.DisplayAspectRatio);
        if (!dar.IsValid)
        {
            // Derive DAR from stored dimensions and SAR: DAR = (W * SAR) / H
            // DAR = (W * sar.Num / sar.Den) / H = (W * sar.Num) / (H * sar.Den)
            dar = Rational.Reduce(storedW * sar.Numerator, storedH * sar.Denominator);
        }

        // Interlacing
        string? fieldOrder = video.FieldOrder?.Trim();
        bool isInterlaced =
            !string.IsNullOrWhiteSpace(fieldOrder) &&
            !fieldOrder.Equals("progressive", StringComparison.OrdinalIgnoreCase) &&
            !fieldOrder.Equals("unknown", StringComparison.OrdinalIgnoreCase);

        // Pixel format + bit depth
        string? pixFmt = video.PixFmt?.Trim();
        int bitDepth = InferBitDepthFromPixFmt(pixFmt);

        // Color metadata (may be null/missing)
        string? colorSpace = NullIfEmpty(video.ColorSpace);
        string? colorPrimaries = NullIfEmpty(video.ColorPrimaries);
        string? colorTransfer = NullIfEmpty(video.ColorTransfer);

        // Duration: prefer format.duration, but tolerate missing
        TimeSpan duration = TimeSpan.Zero;
        if (probe.Format?.DurationSeconds is double durS && durS > 0)
        {
            duration = TimeSpan.FromSeconds(durS);
        }

        return new VideoDescriptor(
            Path: path,

            StoredWidth: storedW,
            StoredHeight: storedH,

            FrameRate: fps,

            IsInterlaced: isInterlaced,
            FieldOrder: fieldOrder,

            SampleAspectRatio: sar,
            DisplayAspectRatio: dar,

            PixelFormat: pixFmt,
            BitDepth: bitDepth,

            ColorSpace: colorSpace,
            ColorPrimaries: colorPrimaries,
            ColorTransfer: colorTransfer,

            Duration: duration
        );
    }

    private static StreamInfo? PickBestVideoStream(ProbeResult probe)
    {
        // Basic heuristic:
        // 1) codec_type == "video"
        // 2) choose the stream with the largest pixel area (usually main video)
        // 3) tie-breaker: first
        return probe.Streams
            .Where(s => s.CodecType != null &&
                        s.CodecType.Equals("video", StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(s => (long)(s.Width ?? 0) * (long)(s.Height ?? 0))
            .FirstOrDefault();
    }

    private static Rational ParseRationalOrDefault(string? s, Rational defaultValue)
    {
        Rational r = ParseRationalOrInvalid(s);
        return r.IsValid ? r : defaultValue;
    }

    private static Rational ParseRationalOrInvalid(string? s)
    {
        if (string.IsNullOrWhiteSpace(s))
            return Rational.Invalid;

        // ffprobe uses "30000/1001" and also "8:9"
        return Rational.Parse(s);
    }

    private static int InferBitDepthFromPixFmt(string? pixFmt)
    {
        // Common ffmpeg pix_fmt patterns:
        // yuv420p       -> 8-bit
        // yuv420p10le   -> 10-bit
        // yuv422p12le   -> 12-bit
        // gbrp16le      -> 16-bit
        if (string.IsNullOrWhiteSpace(pixFmt))
            return 0; // unknown

        // Look for "...p10..." "...p12..." etc.
        // Prefer explicit numbers; otherwise assume 8 for typical planar YUV without a number.
        string s = pixFmt;

        // Find the first run of digits in the string (e.g., "10" in "yuv420p10le")
        int start = -1;
        for (int i = 0; i < s.Length; i++)
        {
            if (char.IsDigit(s[i]))
            {
                start = i;
                break;
            }
        }

        if (start >= 0)
        {
            int end = start;
            while (end < s.Length && char.IsDigit(s[end])) end++;

            string digits = s.Substring(start, end - start);

            // Many pix_fmts also contain the chroma subsampling digits "420" before "p10".
            // Example: "yuv420p10le" -> first digits = "420" (NOT bit depth).
            // So we need a better heuristic:
            // - If there's "p10"/"p12"/"p16" etc, parse digits after the 'p'.
            int pIndex = s.IndexOf('p');
            if (pIndex >= 0 && pIndex + 1 < s.Length && char.IsDigit(s[pIndex + 1]))
            {
                int j = pIndex + 1;
                int k = j;
                while (k < s.Length && char.IsDigit(s[k])) k++;
                string bd = s.Substring(j, k - j);
                if (int.TryParse(bd, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsedBd))
                    return parsedBd;
            }

            // Fall back: if the string contains "10" or "12" or "16" as a standalone-ish hint
            // (rare), parse first digits anyway.
            if (int.TryParse(digits, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed))
            {
                // If parsed looks like chroma subsampling (e.g. 420/422/444), treat as 8-bit default
                if (parsed is 420 or 422 or 444)
                    return 8;
            }
        }

        // Most common default
        return 8;
    }

    private static string? NullIfEmpty(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}
