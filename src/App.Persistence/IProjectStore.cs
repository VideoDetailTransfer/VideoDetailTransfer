namespace VideoDetailTransfer.Persistence;

public interface IProjectStore
{
    Task SaveAsync(string path, object project);
    Task<T> LoadAsync<T>(string path);
}