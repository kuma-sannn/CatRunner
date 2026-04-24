using System;
using System.IO;
using System.Text.Json;

namespace GpuCatMonitor.Services;

public enum MonitoringMode
{
    Gpu,
    Cpu,
    Adaptive
}

public class AppSettings
{
    public double Left { get; set; } = -1;
    public double Top { get; set; } = -1;
    public MonitoringMode Mode { get; set; } = MonitoringMode.Adaptive;
    public bool IsFloatingVisible { get; set; } = false;
    public bool RunOnStartup { get; set; } = false;
}

public static class SettingsManager
{
    private static string GetSettingsPath()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(appData, "GpuCatMonitor", "settings.json");
    }

    public static AppSettings Load()
    {
        try
        {
            var path = GetSettingsPath();
            if (File.Exists(path))
            {
                var json = File.ReadAllText(path);
                var settings = JsonSerializer.Deserialize<AppSettings>(json);
                return settings ?? new AppSettings();
            }
        }
        catch { }
        
        return new AppSettings();
    }

    public static void Save(AppSettings settings)
    {
        try
        {
            var path = GetSettingsPath();
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(path, json);
        }
        catch { }
    }
}
