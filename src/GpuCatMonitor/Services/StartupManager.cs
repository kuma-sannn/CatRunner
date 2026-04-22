using Microsoft.Win32;
using System.Windows;

namespace GpuCatMonitor.Services;

public static class StartupManager
{
    private const string AppName = "GpuCatMonitor";
    private const string RegistryPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
    
    public static bool IsStartupEnabled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RegistryPath, false);
            var value = key?.GetValue(AppName);
            return value != null;
        }
        catch
        {
            return false;
        }
    }
    
    public static void EnableStartup()
    {
        try
        {
            var exePath = System.IO.Path.Combine(AppContext.BaseDirectory, "GpuCatMonitor.exe");
            using var key = Registry.CurrentUser.OpenSubKey(RegistryPath, true);
            key?.SetValue(AppName, exePath);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to enable startup: {ex.Message}", "Error", 
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    
    public static void DisableStartup()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RegistryPath, true);
            key?.DeleteValue(AppName, false);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to disable startup: {ex.Message}", "Error", 
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
