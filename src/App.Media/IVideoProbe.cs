namespace VideoDetailTransfer.Media;

public interface IVideoProbe
{
    Task<ProbeResult> ProbeAsync(string path);
}
