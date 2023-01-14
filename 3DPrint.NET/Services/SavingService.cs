using System.Text.Json;
using System.Text.Json.Nodes;

using _3DPrint.NET.Saving;
using Microsoft.AspNetCore.Http;

namespace _3DPrint.NET.Services;
public class SavingService {
    private const string s_saveFile = "config.json";

    private readonly ILogger<SavingService> _logger;

    public static SavingService Current { get; private set; }

    public SavingService(ILogger<SavingService> logger) {
        _logger = logger;

        Current = this;
    }

    public async Task SaveAsync() {
        _logger.LogInformation("Saving to {}", s_saveFile);
        try {
            IReadOnlyDictionary<string, object> properties = Config.Current.GetProperties();
            string json = JsonSerializer.Serialize(properties);
            await File.WriteAllTextAsync(s_saveFile, json);
            _logger.LogInformation("Saving complete");
        } catch (Exception ex) {
            _logger.LogError("Saving config threw an exception: {}", ex.Message);
        }
    }

    public async Task LoadAsync() {
        Config newConfig = await LoadConfigAsync();
        Config.Current = newConfig;
    }

    private async Task<Config> LoadConfigAsync() {
        if (!File.Exists(s_saveFile))
            return new Config();

        try {
            string json = await File.ReadAllTextAsync(s_saveFile);
            IReadOnlyDictionary<string, JsonObject>? serializeProperties = JsonSerializer.Deserialize<IReadOnlyDictionary<string, JsonObject>>(json);

            if(serializeProperties == null) {
                _logger.LogError("Config file was in invalid format");
                return new Config();
            }

            var properties = new Dictionary<string, object>();
            foreach(KeyValuePair<string, JsonObject> property in serializeProperties) {
                Type type = ExtensionMethods.GetType(property.Key);
                properties.Add(property.Key, property.Value.Deserialize(type));
            }

            return new Config(properties);
        } catch (Exception ex) {
            _logger.LogError("Loading config threw an exception: {}", ex.Message);
        }
        return new Config();
    }

    public T GetConfig<T>() where T : new()
        => Config.Current.Get<T>();
}
