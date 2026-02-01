namespace VideoDetailTransfer.Core;

public sealed class Project
{
    public ProjectInfo Info { get; init; } = default!;
    public ProjectPaths Paths { get; set; } = new();
    public ProjectVideoInfo Videos { get; set; } = new();

    // later: Normalize segments, time model, shots, render settings...
}
