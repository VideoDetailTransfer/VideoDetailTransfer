namespace VideoDetailTransfer.Jobs;

public interface IJob
{
    string Name { get; }
    Task RunAsync(IProgress<double> progress, CancellationToken ct);
}