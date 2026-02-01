namespace VideoDetailTransfer.Core;

public sealed record ProjectInfo(
    string Name,
    DateTime CreatedUtc,
    int SchemaVersion);