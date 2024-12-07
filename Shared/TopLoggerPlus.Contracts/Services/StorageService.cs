using System.Text.Json;

namespace TopLoggerPlus.Contracts.Services;

public interface IStorageService
{
    T Read<T>(string key);
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

    public T Read<T>(string key)
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

    public void ResetStorage()
    {
        if (Directory.Exists(_directory))
            Directory.Delete(_directory, true);
        
        Directory.CreateDirectory(_directory);
        File.WriteAllText(Path.Combine(_directory, DataVersionFile), "");
    }
}