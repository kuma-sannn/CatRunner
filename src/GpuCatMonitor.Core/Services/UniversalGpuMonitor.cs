using GpuCatMonitor.Core.Models;
using System.Diagnostics;
using System.Management;

namespace GpuCatMonitor.Core.Services;

public class UniversalGpuMonitor : IDisposable
{
    private readonly System.Timers.Timer _updateTimer;
    private PerformanceCounter? _gpuUsageCounter;
    private PerformanceCounter? _gpuMemoryUsageCounter;
    private PerformanceCounter? _gpuTemperatureCounter;
    private PerformanceCounter? _gpuPowerDrawCounter;
    private string _gpuName = "Unknown GPU";
    private string _gpuVendor = "Unknown";
    
    public GpuInfo? CurrentGpuInfo { get; private set; }
    public event EventHandler<GpuInfo>? OnGpuInfoUpdated;
    
    public UniversalGpuMonitor(int updateIntervalMs = 500)
    {
        _updateTimer = new System.Timers.Timer(updateIntervalMs);
        _updateTimer.Elapsed += OnTimerElapsed;
        
        InitializeGpuInfo();
        SetupPerformanceCounters();
    }
    
    private void InitializeGpuInfo()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher(
                "SELECT * FROM Win32_VideoController");
            
            foreach (ManagementObject obj in searcher.Get())
            {
                var name = obj["Name"]?.ToString();
                if (!string.IsNullOrEmpty(name))
                {
                    _gpuName = name;
                    _gpuVendor = DetectVendor(name);
                    break;
                }
            }
        }
        catch { }
    }
    
    private string DetectVendor(string gpuName)
    {
        var nameLower = gpuName.ToLower();
        if (nameLower.Contains("nvidia") || nameLower.Contains("geforce") || nameLower.Contains("rtx") || nameLower.Contains("gtx"))
            return "NVIDIA";
        if (nameLower.Contains("amd") || nameLower.Contains("radeon") || nameLower.Contains("ati"))
            return "AMD";
        if (nameLower.Contains("intel") || nameLower.Contains("arc") || nameLower.Contains("iris") || nameLower.Contains("hd graphics"))
            return "Intel";
        return "Unknown";
    }
    
    private void SetupPerformanceCounters()
    {
        try
        {
            // Try to find GPU engine performance counter
            if (PerformanceCounterCategory.Exists("GPU Engine"))
            {
                var category = new PerformanceCounterCategory("GPU Engine");
                var instances = category.GetInstanceNames();
                
                // Look for "engtype_3D" instance which represents 3D engine (main GPU usage)
                var targetInstance = instances.FirstOrDefault(i => 
                    i.Contains("engtype_3D") && !i.Contains("pid"));
                
                if (targetInstance != null)
                {
                    _gpuUsageCounter = new PerformanceCounter("GPU Engine", 
                        "Utilization Percentage", targetInstance);
                    _gpuMemoryUsageCounter = new PerformanceCounter("GPU Engine", 
                        "Memory Used", targetInstance);
                    _gpuTemperatureCounter = new PerformanceCounter("GPU Engine", 
                        "Temperature", targetInstance);
                    _gpuPowerDrawCounter = new PerformanceCounter("GPU Engine", 
                        "Power", targetInstance);
                }
                else if (instances.Length > 0)
                {
                    // Fallback to first instance
                    _gpuUsageCounter = new PerformanceCounter("GPU Engine", 
                        "Utilization Percentage", instances[0]);
                    _gpuMemoryUsageCounter = new PerformanceCounter("GPU Engine", 
                        "Memory Used", instances[0]);
                    _gpuTemperatureCounter = new PerformanceCounter("GPU Engine", 
                        "Temperature", instances[0]);
                    _gpuPowerDrawCounter = new PerformanceCounter("GPU Engine", 
                        "Power", instances[0]);
                }
            }
        }
        catch { }
    }
    
    public void Start() => _updateTimer.Start();
    public void Stop() => _updateTimer.Stop();
    
    private void OnTimerElapsed(object? sender, System.Timers.ElapsedEventArgs e)
    {
        var info = GetGpuInfo();
        if (info != null)
        {
            CurrentGpuInfo = info;
            OnGpuInfoUpdated?.Invoke(this, info);
        }
    }
    
    public GpuInfo GetGpuInfo()
    {
        float usage = 0;
        float memoryUsage = 0;
        float temperature = 0;
        float powerDraw = 0;
        
        if (_gpuUsageCounter != null)
        {
            try
            {
                usage = _gpuUsageCounter.NextValue();
                memoryUsage = _gpuMemoryUsageCounter?.NextValue() ?? 0;
                temperature = _gpuTemperatureCounter?.NextValue() ?? 0;
                powerDraw = _gpuPowerDrawCounter?.NextValue() ?? 0;
            }
            catch { }
        }
        
        return new GpuInfo
        {
            Name = _gpuName,
            Vendor = _gpuVendor,
            GpuUsage = Math.Min(usage, 100f),
            MemoryUsage = memoryUsage,
            Temperature = temperature,
            PowerDraw = powerDraw
        };
    }
    
    public void Dispose()
    {
        _updateTimer?.Dispose();
        _gpuUsageCounter?.Dispose();
        _gpuMemoryUsageCounter?.Dispose();
        _gpuTemperatureCounter?.Dispose();
        _gpuPowerDrawCounter?.Dispose();
    }
}
