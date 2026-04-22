using GpuCatMonitor.Core.Models;
using System.Diagnostics;

namespace GpuCatMonitor.Core.Services;

public class CpuMonitor : IDisposable
{
    private readonly PerformanceCounter _cpuCounter;
    private readonly System.Timers.Timer _updateTimer;
    
    public float CurrentUsage { get; private set; }
    public event EventHandler<float>? OnCpuUsageUpdated;
    
    public CpuMonitor(int updateIntervalMs = 500)
    {
        _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
        // First call returns 0, so we warm it up
        _cpuCounter.NextValue();
        
        _updateTimer = new System.Timers.Timer(updateIntervalMs);
        _updateTimer.Elapsed += OnTimerElapsed;
    }
    
    public void Start() => _updateTimer.Start();
    public void Stop() => _updateTimer.Stop();
    
    private void OnTimerElapsed(object? sender, System.Timers.ElapsedEventArgs e)
    {
        CurrentUsage = _cpuCounter.NextValue();
        OnCpuUsageUpdated?.Invoke(this, CurrentUsage);
    }
    
    public void Dispose()
    {
        _updateTimer?.Dispose();
        _cpuCounter?.Dispose();
    }
}
