using System.IO;
using System.Text.Json;
using SampleTodoApp.Models;

namespace SampleTodoApp.Services;

public sealed class JsonTodoStore : ITodoStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    private readonly string _settingsPath;
    private string _dataDirectory;

    public JsonTodoStore()
    {
        var appRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "GraftSampleTodo");
        _settingsPath = Path.Combine(appRoot, "settings.json");
        _dataDirectory = ResolveInitialDataDirectory(appRoot);
    }

    public string DataDirectory => _dataDirectory;

    public string DataFilePath => Path.Combine(_dataDirectory, "todos.json");

    public async Task SetDataDirectoryAsync(string directory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);
        _dataDirectory = Path.GetFullPath(directory);
        Directory.CreateDirectory(_dataDirectory);
        await SaveSettingsAsync(new AppSettings { DataDirectory = _dataDirectory }).ConfigureAwait(false);
    }

    public async Task<ProjectData> LoadAsync()
    {
        Directory.CreateDirectory(_dataDirectory);
        if (!File.Exists(DataFilePath))
        {
            return new ProjectData();
        }

        await using var stream = File.OpenRead(DataFilePath);
        var data = await JsonSerializer.DeserializeAsync<ProjectData>(stream, JsonOptions).ConfigureAwait(false);
        return data ?? new ProjectData();
    }

    public async Task SaveAsync(ProjectData data)
    {
        ArgumentNullException.ThrowIfNull(data);
        Directory.CreateDirectory(_dataDirectory);
        await using var stream = File.Create(DataFilePath);
        await JsonSerializer.SerializeAsync(stream, data, JsonOptions).ConfigureAwait(false);
    }

    public async Task ExportAsync(ProjectData data, string path)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(dir))
        {
            Directory.CreateDirectory(dir);
        }

        await using var stream = File.Create(path);
        await JsonSerializer.SerializeAsync(stream, data, JsonOptions).ConfigureAwait(false);
    }

    public async Task<ProjectData> ImportAsync(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        await using var stream = File.OpenRead(path);
        var data = await JsonSerializer.DeserializeAsync<ProjectData>(stream, JsonOptions).ConfigureAwait(false);
        return data ?? new ProjectData();
    }

    private string ResolveInitialDataDirectory(string appRoot)
    {
        var settings = LoadSettings();
        if (!string.IsNullOrWhiteSpace(settings.DataDirectory))
        {
            return Path.GetFullPath(settings.DataDirectory);
        }

        return Path.Combine(appRoot, "Data");
    }

    private AppSettings LoadSettings()
    {
        if (!File.Exists(_settingsPath))
        {
            return new AppSettings();
        }

        try
        {
            var json = File.ReadAllText(_settingsPath);
            return JsonSerializer.Deserialize<AppSettings>(json, JsonOptions) ?? new AppSettings();
        }
        catch
        {
            return new AppSettings();
        }
    }

    private async Task SaveSettingsAsync(AppSettings settings)
    {
        var dir = Path.GetDirectoryName(_settingsPath);
        if (!string.IsNullOrWhiteSpace(dir))
        {
            Directory.CreateDirectory(dir);
        }

        await using var stream = File.Create(_settingsPath);
        await JsonSerializer.SerializeAsync(stream, settings, JsonOptions).ConfigureAwait(false);
    }
}
