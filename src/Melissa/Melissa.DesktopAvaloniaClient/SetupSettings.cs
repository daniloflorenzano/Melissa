using System.IO;
using System.Text.Json;

namespace Melissa.DesktopAvaloniaClient;

public class SetupSettings(string? settingsFilePath = null)
{
    private readonly string _settingsFilePath = settingsFilePath ?? DefaultSettingsFilePath;

    public void CreateSettingsFileIfNotExist()
    {
        if (!File.Exists(_settingsFilePath)) 
            File.WriteAllText(_settingsFilePath, _defaultSettings);
    }

    public void SaveNewServerAddress(string serverAddress)
    {
        if (string.IsNullOrWhiteSpace(serverAddress)) 
            return;
        
        var settings = File.ReadAllText(_settingsFilePath);
        var settingsObj = JsonSerializer.Deserialize<Settings>(settings);
        settingsObj!.ServerAddress = serverAddress;
        File.WriteAllText(_settingsFilePath, JsonSerializer.Serialize(settingsObj));
    }

    public string ReadServerAddress(string? settingsFilePath = null)
    {  
        CreateSettingsFileIfNotExist();
        
        var settings = File.ReadAllText(settingsFilePath ?? DefaultSettingsFilePath);
        var settingsObj = JsonSerializer.Deserialize<Settings>(settings);
        return settingsObj?.ServerAddress ?? string.Empty;
    }

    private readonly string _defaultSettings = JsonSerializer.Serialize(new Settings());
    private const string DefaultSettingsFilePath = "settings.json";
}