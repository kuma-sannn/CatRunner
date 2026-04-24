# GPU Cat Monitor 🐱

A lightweight desktop pet that monitors your GPU in real-time. The cat runs faster as your GPU usage increases, providing a fun and visual way to track performance!

![App Screenshot](docs/screenshots/screenshot.png)

## 🚀 Download & Run

To get the cat running on your desktop:

1. **[Download CatRunner_Release.zip](CatRunner_Release.zip)** from this repository.
2. **Extract** the ZIP file to a folder of your choice.
3. Run **`GpuCatMonitor.exe`**.

### ⚠️ Security Note (Windows SmartScreen)
Windows may show a "Windows protected your PC" alert because the app is not digitally signed. 
- Click **"More info"**
- Click **"Run anyway"**

## ✨ Features

- **Real-time Monitoring**: Supports NVIDIA, AMD, and Intel GPUs.
- **Adaptive Mode**: Can track the highest usage between CPU and GPU automatically.
- **System Tray**: Minimize to tray to keep your desktop clean.
- **Always on Top**: The cat floats above other windows (optional).
- **Customizable**: Drag to move, right-click for settings.

## 🛠️ Troubleshooting

- **App won't start**: Install the [.NET 8.0 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/8.0).
- **Animation lag**: Ensure your GPU drivers are updated.
- **Settings reset**: Settings are saved in `%LOCALAPPDATA%\GpuCatMonitor`.

## 👩‍💻 For Developers

If you want to contribute or build from source:

1. Clone the repository.
2. Open the solution in the `src` folder.
3. Use `dotnet build` or Visual Studio 2022.

```bash
# Run via CLI
dotnet run --project src/GpuCatMonitor
```

## Support 💖

- ⭐ **Star the repo** if you like the cat!
- 🐛 **Report bugs** in the Issues tab.
- 🎁 **[Support Development](docs/SPONSORS.html)**
