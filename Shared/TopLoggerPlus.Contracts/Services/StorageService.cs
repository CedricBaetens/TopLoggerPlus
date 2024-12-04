using System.Text.Json;

namespace TopLoggerPlus.Contracts.Services;

public interface IStorageService
{
    T? Read<T>(string key);
    void Write<T>(string key, T value);
    void Delete(string key);
}

public class StorageService : IStorageService
{
    private readonly string _directory;

    public StorageService()
    {
        _directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TopLoggerPlus");
        if (!Directory.Exists(_directory))
            Directory.CreateDirectory(_directory);
        
        var dataVersionFile = Path.Combine(_directory, "v1.txt");
        if (File.Exists(dataVersionFile)) return;
        
        Directory.Delete(_directory, true);
        Directory.CreateDirectory(_directory);
        File.WriteAllText(dataVersionFile, "");
    }

    public T? Read<T>(string key)
    {
        var path = Path.Combine(_directory, $"{key}.txt");
        return File.Exists(path)
            ? JsonSerializer.Deserialize<T>(File.ReadAllText(path))
            : default;
    }
    public void Write<T>(string key, T value)
    {
        var path = Path.Combine(_directory, $"{key}.txt");
        File.WriteAllText(path, JsonSerializer.Serialize(value));
    }
    public void Delete(string key)
    {
        var path = Path.Combine(_directory, $"{key}.txt");
        if (File.Exists(path)) File.Delete(path);
    }
}