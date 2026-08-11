using SampleTodoApp.Models;

namespace SampleTodoApp.Services;

public interface ITodoStore
{
    string DataDirectory { get; }

    string DataFilePath { get; }

    Task SetDataDirectoryAsync(string directory);

    Task<ProjectData> LoadAsync();

    Task SaveAsync(ProjectData data);

    Task ExportAsync(ProjectData data, string path);

    Task<ProjectData> ImportAsync(string path);
}
