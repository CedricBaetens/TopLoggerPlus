using PCLStorage;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using TopLoggerPlus.Logic.Model;

namespace TopLoggerPlus.Logic
{
    public class StorageService
    {
        public async Task<string> ReadUserId()
        {
            var rootFolder = FileSystem.Current.LocalStorage;
            var file = await rootFolder.CreateFileAsync("user.txt", CreationCollisionOption.OpenIfExists);

            try
            {
                return await file.ReadAllTextAsync();
            }
            catch (Exception)
            {
                await file.DeleteAsync();
                return null;
            }
        }
        public async Task WriteUserId(string userId)
        {
            var rootFolder = FileSystem.Current.LocalStorage;
            var file = await rootFolder.CreateFileAsync("user.txt", CreationCollisionOption.ReplaceExisting);
            await file.WriteAllTextAsync(userId);
        }

        public async Task<List<Route>> ReadRoutes()
        {
            var rootFolder = FileSystem.Current.LocalStorage;
            var file = await rootFolder.CreateFileAsync("routes.txt", CreationCollisionOption.OpenIfExists);
            try
            {
                var json = await file.ReadAllTextAsync();
                if (string.IsNullOrEmpty(json)) return null;
                return JsonSerializer.Deserialize<List<Route>>(json);
            }
            catch (Exception)
            {
                await file.DeleteAsync();
                return null;
            }
        }
        public async Task WriteRoutes(List<Route> routes)
        {
            var rootFolder = FileSystem.Current.LocalStorage;
            var file = await rootFolder.CreateFileAsync("routes.txt", CreationCollisionOption.ReplaceExisting);
            await file.WriteAllTextAsync(JsonSerializer.Serialize(routes, new JsonSerializerOptions { WriteIndented = true }));
        }
    }
}
