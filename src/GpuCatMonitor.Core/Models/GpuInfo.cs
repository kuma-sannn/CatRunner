namespace GpuCatMonitor.Core.Models;

public class GpuInfo
{
    public string Name { get; set; } = "Unknown GPU";
    public string Vendor { get; set; } = "Unknown";
    public float GpuUsage { get; set; }
    public float MemoryUsage { get; set; }
    public float Temperature { get; set; }
    public float PowerDraw { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.Now;
}
