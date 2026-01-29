namespace VideoDetailTransfer.Media;

public interface IVideoReader : IDisposable
{
    int Width { get; }
    int Height { get; }
    double FrameRate { get; }

    VideoFrame ReadNext();
    VideoFrame GetFrame(long index);
}