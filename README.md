# GPU Cat Monitor

A desktop pet that monitors your GPU in real-time! The cat runs faster when your GPU is working hard, and slows down when it's idle.

## Features

- **Real-time GPU Monitoring**: Supports NVIDIA, AMD, and Intel GPUs
- **Always Visible**: Floats on your desktop like a battery indicator
- **Variable Speed Animation**: Cat runs at 5-30 FPS based on GPU load
- **Hover for Stats**: See exact GPU usage, temperature, and name
- **Draggable**: Move it anywhere on your screen
- **System Tray**: Minimize to tray and access settings
- **Auto-start**: Optional startup with Windows

## How It Works

1. Download and run `GpuCatMonitor.exe`
2. The cat appears on your desktop (default: bottom-right corner)
3. Drag it anywhere you want
4. Watch the cat's speed change with your GPU usage:
   - **Slow (5 FPS)**: GPU idle (< 30%)
   - **Medium (15 FPS)**: GPU working (30-70%)
   - **Fast (30 FPS)**: GPU at full load (> 70%)

## Steam Integration

- Free to download
- Steam Workshop support for custom characters
- Cloud sync for settings
- Achievements for monitoring milestones

## Technical Details

- Built with C# WPF
- Uses NVAPI for NVIDIA, ADL for AMD, WMI for Intel
- Single-file executable, no installation required
- ~10MB download, minimal CPU/memory usage

## Development

```bash
# Build
cd src
dotnet build

# Run
dotnet run --project GpuCatMonitor

# Publish for Steam
dotnet publish GpuCatMonitor -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```
