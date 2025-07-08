using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace ImportadorModelo2.Services
{
    public interface ILocalStorageService
    {
        Task SetItemAsync<T>(string key, T value);
        Task<T?> GetItemAsync<T>(string key);
        Task RemoveItemAsync(string key);
        Task ClearAsync();
    }

    public class LocalStorageService : ILocalStorageService
    {
        private readonly string _storageDirectory;
        private readonly JsonSerializerOptions _jsonOptions;

        public LocalStorageService()
        {
            // Criar diretório na pasta do usuário
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            _storageDirectory = Path.Combine(appDataPath, "ImportadorModelo2");
            
            // Criar diretório se não existir
            Directory.CreateDirectory(_storageDirectory);

            _jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
        }

        public async Task SetItemAsync<T>(string key, T value)
        {
            try
            {
                var filePath = GetFilePath(key);
                var json = JsonSerializer.Serialize(value, _jsonOptions);
                await File.WriteAllTextAsync(filePath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erro ao salvar {key}: {ex.Message}");
                throw;
            }
        }

        public async Task<T?> GetItemAsync<T>(string key)
        {
            try
            {
                var filePath = GetFilePath(key);
                
                if (!File.Exists(filePath))
                    return default(T);

                var json = await File.ReadAllTextAsync(filePath);
                
                if (string.IsNullOrWhiteSpace(json))
                    return default(T);

                return JsonSerializer.Deserialize<T>(json, _jsonOptions);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erro ao carregar {key}: {ex.Message}");
                return default(T);
            }
        }

        public async Task RemoveItemAsync(string key)
        {
            try
            {
                var filePath = GetFilePath(key);
                
                if (File.Exists(filePath))
                {
                    await Task.Run(() => File.Delete(filePath));
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erro ao remover {key}: {ex.Message}");
            }
        }

        public async Task ClearAsync()
        {
            try
            {
                await Task.Run(() =>
                {
                    if (Directory.Exists(_storageDirectory))
                    {
                        foreach (var file in Directory.GetFiles(_storageDirectory))
                        {
                            File.Delete(file);
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erro ao limpar storage: {ex.Message}");
            }
        }

        private string GetFilePath(string key)
        {
            // Sanitizar o nome do arquivo
            var fileName = string.Join("_", key.Split(Path.GetInvalidFileNameChars()));
            return Path.Combine(_storageDirectory, $"{fileName}.json");
        }
    }
}