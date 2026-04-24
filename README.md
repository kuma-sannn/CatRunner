# GPU Cat Monitor 🐱

A desktop pet that monitors your GPU in real-time! The cat runs faster when your GPU is working hard, and slows down when it's idle.

![App Screenshot](docs/screenshots/screenshot.png)

🎥 **[Watch Demo Video](docs/videos/demo.mp4)**

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

## ⚠️ Security Note (Windows SmartScreen)

Since this application is not digitally signed with a commercial certificate, Windows Defender SmartScreen may display a warning saying **"Windows protected your PC"**.

To run the app:
1. Click **"More info"**.
2. Click **"Run anyway"**.

This alert appears because the executable is new and has not yet built up "reputation" with Microsoft's servers. The app is safe and only monitors system performance.

## Troubleshooting

- **App doesn't start**: Ensure you have the [.NET 8.0 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/8.0) installed.
- **Cat is frozen**: Check if your GPU drivers are up to date.
- **Settings not saving**: The app saves settings in `%LOCALAPPDATA%\GpuCatMonitor`. Ensure the app has permission to write to this folder.

## Support 💖

Love the app? Help keep the cat running!

- 🎁 **[Sponsor on GitHub](docs/SPONSORS.html)** - Support development
- ⭐ **Star the repo** - Show your appreciation
- 🐛 **Report bugs** - Help improve the app
- 💡 **Suggest features** - Share your ideas
