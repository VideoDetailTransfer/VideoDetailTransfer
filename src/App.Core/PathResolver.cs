namespace VideoDetailTransfer.Core;

public sealed class PathResolver
{
    private readonly string _projectRoot; // directory containing the project json

    public PathResolver(string projectFilePath)
    {
        _projectRoot = Path.GetDirectoryName(projectFilePath) ?? throw new ArgumentException("Invalid project file path.");
    }

    public string Resolve(string path) => Path.IsPathRooted(path) ? path : Path.GetFullPath(Path.Combine(_projectRoot, path));
}
