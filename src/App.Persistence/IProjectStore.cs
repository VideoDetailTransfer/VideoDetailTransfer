using VideoDetailTransfer.Core;

namespace VideoDetailTransfer.Persistence;

public interface IProjectStore
{
    Task SaveAsync(string path, Project project);
    Task<Project> LoadAsync(string path);
}