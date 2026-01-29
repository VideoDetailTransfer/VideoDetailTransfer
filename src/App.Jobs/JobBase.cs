using VideoDetailTransfer.Jobs;

public abstract class JobBase : IJob
{
    public abstract string Name { get; }

    public async Task RunAsync(
        IProgress<double> progress,
        CancellationToken ct)
    {
        await ExecuteAsync(progress, ct);
    }

    protected abstract Task ExecuteAsync(
        IProgress<double> progress,
        CancellationToken ct);
}