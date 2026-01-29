namespace VideoDetailTransfer.Cv;

public interface ISpatialAligner
{
    AlignmentResult Align(FrameData reference, FrameData target);
}