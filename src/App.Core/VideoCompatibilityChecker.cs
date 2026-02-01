namespace VideoDetailTransfer.Core;

public static class VideoCompatibilityChecker
{
    public static IReadOnlyList<string> Check(VideoDescriptor reference, VideoDescriptor target)
    {
        List<string> warnings = new List<string>();

        // Interlace / IVTC
        if (reference.IsInterlaced)
        {
            warnings.Add("Reference is interlaced; IVTC/detelecine (or deinterlace) is required before reliable frame matching.");
        }

        // Frame rate mismatch
        if (reference.FrameRate.IsValid && target.FrameRate.IsValid)
        {
            double refFps = reference.FrameRate.ToDouble();
            double tgtFps = target.FrameRate.ToDouble();

            // Tolerance: 0.01 fps is plenty for common exact rationals.
            if (Math.Abs(refFps - tgtFps) > 0.01)
            {
                warnings.Add($"Frame rate mismatch (reference {refFps:0.###} fps vs target {tgtFps:0.###} fps). Expect non-1:1 mapping without normalization.");
            }
        }

        // Duration mismatch (container-level symptom of edits)
        double durDiff = Math.Abs((reference.Duration - target.Duration).TotalSeconds);
        if (durDiff > 0.5) // small tolerance
        {
            warnings.Add($"Duration differs by ~{durDiff:0.###}s. Expect edits/extra frames; use piecewise time alignment.");
        }

        // Bit depth mismatch
        if (reference.BitDepth != 0 && target.BitDepth != 0 && reference.BitDepth != target.BitDepth)
        {
            warnings.Add($"Bit depth mismatch (reference {reference.BitDepth}-bit vs target {target.BitDepth}-bit). Use float/linear pipeline; output encode should be ≥10-bit to avoid banding.");
        }

        // Color space hints (not always present on all files)
        if (!string.IsNullOrWhiteSpace(reference.ColorSpace) &&
            !string.IsNullOrWhiteSpace(target.ColorSpace) &&
            !StringEqualsIgnoreCase(reference.ColorSpace, target.ColorSpace))
        {
            warnings.Add($"Color space differs (reference {reference.ColorSpace} vs target {target.ColorSpace}). Expect different luma/chroma behavior; match in linear light carefully.");
        }

        if (!string.IsNullOrWhiteSpace(reference.ColorTransfer) &&
            !string.IsNullOrWhiteSpace(target.ColorTransfer) &&
            !StringEqualsIgnoreCase(reference.ColorTransfer, target.ColorTransfer))
        {
            warnings.Add($"Transfer characteristics differ (reference {reference.ColorTransfer} vs target {target.ColorTransfer}). Gamma mismatch may affect matching/transfer if not linearized correctly.");
        }

        if (!string.IsNullOrWhiteSpace(reference.ColorPrimaries) &&
            !string.IsNullOrWhiteSpace(target.ColorPrimaries) &&
            !StringEqualsIgnoreCase(reference.ColorPrimaries, target.ColorPrimaries))
        {
            warnings.Add($"Color primaries differ (reference {reference.ColorPrimaries} vs target {target.ColorPrimaries}). Consider color management if you later do chroma operations.");
        }

        // Aspect / geometry hint (SAR/DAR)
        if (reference.SampleAspectRatio.IsValid && target.SampleAspectRatio.IsValid)
        {
            // Reference might be anamorphic 8:9; target likely 1:1.
            if (!reference.SampleAspectRatio.Equals(target.SampleAspectRatio))
            {
                warnings.Add($"Sample aspect ratio differs (reference {reference.SampleAspectRatio} vs target {target.SampleAspectRatio}). Treat reference as anamorphic; do alignment in stored raster then compose with output scaling.");
            }
        }

        return warnings;
    }

    private static bool StringEqualsIgnoreCase(string a, string b) => string.Equals(a.Trim(), b.Trim(), StringComparison.OrdinalIgnoreCase);
}
