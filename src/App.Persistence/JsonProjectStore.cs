using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using VideoDetailTransfer.Core;

namespace VideoDetailTransfer.Persistence;

public sealed class JsonProjectStore : IProjectStore
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters =
        {
            new JsonStringEnumConverter(JsonNamingPolicy.CamelCase),
            new RationalJsonConverter()
        }
    };

    public async Task SaveAsync(string path, Project project)
    {
        ArgumentNullException.ThrowIfNull(project);

        string? dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(dir))
            Directory.CreateDirectory(dir);

        string json = JsonSerializer.Serialize(project, Options);

        // Atomic-ish save: write temp then replace
        string tmp = path + ".tmp";
        await File.WriteAllTextAsync(tmp, json).ConfigureAwait(false);

        if (File.Exists(path))
            File.Replace(tmp, path, null);
        else
            File.Move(tmp, path);
    }

    public async Task<Project> LoadAsync(string path)
    {
        string json = await File.ReadAllTextAsync(path).ConfigureAwait(false);
        Project project = JsonSerializer.Deserialize<Project>(json, Options) ?? throw new InvalidDataException("Failed to deserialize project file.");

        // Schema version gate (simple for now)
        if (project.Info.SchemaVersion > CoreConstants.CurrentSchemaVersion)
        {
            throw new NotSupportedException(
                $"Project schema v{project.Info.SchemaVersion} is newer than this app supports (v{CoreConstants.CurrentSchemaVersion}).");
        }

        // Later: migration hooks if project.Info.SchemaVersion < CurrentSchemaVersion

        return project;
    }
}
