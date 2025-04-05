using System.Text.Json;

namespace TopLoggerPlus.Contracts.Services;

public interface IStorageService
{
    (T data, DateTime lastModified) Read<T>(string key);
    void Write<T>(string key, T value);
    void Delete(string key);
    
    void ResetStorage();
}

public class StorageService : IStorageService
{
    private const string DataVersionFile = "v1.txt";
    private readonly string _directory;

    public StorageService()
    {
        _directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TopLoggerPlus");
        if (!File.Exists(Path.Combine(_directory, DataVersionFile)))
            ResetStorage();
    }

    public (T, DateTime) Read<T>(string key)
    {
        try
        {
            var path = Path.Combine(_directory, $"{key}.txt");
            if (!File.Exists(path))
                return (default, DateTime.Now);

            var data = JsonSerializer.Deserialize<T>(File.ReadAllText(path));
            var lastModified = File.GetLastWriteTimeUtc(path);
            return (data, lastModified);
        }
        catch (JsonException)
        {
            Delete(key);
            return default;
        }
    }
    public void Write<T>(string key, T value)
    {
        var path = Path.Combine(_directory, $"{key}.txt");
        File.WriteAllText(path, JsonSerializer.Serialize(value, new JsonSerializerOptions { WriteIndented = true }));
    }
    public void Delete(string key)
    {
        var path = Path.Combine(_directory, $"{key}.txt");
        if (File.Exists(path)) File.Delete(path);
    }

    public void ResetStorage()
    {
        if (Directory.Exists(_directory))
            Directory.Delete(_directory, true);
        
        Directory.CreateDirectory(_directory);
        File.WriteAllText(Path.Combine(_directory, DataVersionFile), "");
    }
}