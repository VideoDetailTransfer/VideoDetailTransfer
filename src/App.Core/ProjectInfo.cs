namespace VideoDetailTransfer.Core;

public sealed record ProjectInfo(
    string Name,
    string CreatedUtc,
    int SchemaVersion);