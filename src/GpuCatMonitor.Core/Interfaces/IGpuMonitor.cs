using GpuCatMonitor.Core.Models;

namespace GpuCatMonitor.Core.Interfaces;

public interface IGpuMonitor : IDisposable
{
    string VendorName { get; }
    bool IsAvailable { get; }
    GpuInfo? GetGpuInfo();
    event EventHandler<GpuInfo>? OnGpuInfoUpdated;
}
