using System.IO;
using System.Text.Json;

namespace Melissa.DesktopAvaloniaClient;

public class SetupSettings(string settingsFilePath)
{
    public void CreateSettingsFileIfNotExist()
    {
        if (!File.Exists(settingsFilePath)) 
            File.WriteAllText(settingsFilePath, _defaultSettings);
    }

    public void SaveNewServerAddress(string serverAddress)
    {
        if (string.IsNullOrWhiteSpace(serverAddress)) 
            return;
        
        var settings = File.ReadAllText(settingsFilePath);
        var settingsObj = JsonSerializer.Deserialize<Settings>(settings);
        settingsObj!.ServerAddress = serverAddress;
        File.WriteAllText(settingsFilePath, JsonSerializer.Serialize(settingsObj));
    }

    public static string ReadServerAddress(string? settingsFilePath = null)
    {  
        var settings = File.ReadAllText(settingsFilePath ?? "settings.json");
        var settingsObj = JsonSerializer.Deserialize<Settings>(settings);
        return settingsObj?.ServerAddress ?? string.Empty;
    }

    private readonly string _defaultSettings = JsonSerializer.Serialize(new Settings()); 
}