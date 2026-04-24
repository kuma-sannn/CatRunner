using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Drawing;
using System.Linq;
using GpuCatMonitor.Core.Models;
using GpuCatMonitor.Core.Services;
using GpuCatMonitor.Services;
using ImageMagick;

namespace GpuCatMonitor.Views;

public partial class CatWindow : Window
{
    // === Constants (easily configurable for different animals/themes) ===
    private const string GifResourceName = "pixel-cat.gif";
    private const string GifFilePath = @"gif\pixel-cat.gif";
    
    private const int FloatingWidth = 100;
    private const int FloatingHeight = 80;
    private const int TrayIconSize = 32;
    
    private const double BaseFps = 12;
    private const double MinFps = 5;
    private const double MaxFpsExtra = 25;
    private const double FpsSmoothing = 0.2;
    // === End Constants ===
    
    private readonly UniversalGpuMonitor _gpuMonitor;
    private readonly CpuMonitor _cpuMonitor;
    private List<System.Drawing.Icon>? _iconFrames;
    private List<BitmapSource>? _gifFrames;
    private int _currentFrame = 0;
    private DispatcherTimer? _animationTimer;
    private DispatcherTimer? _positionSaveTimer;
    private GpuInfo? _lastGpuInfo;
    private float _lastCpuUsage;
    
    private double _baseFps = BaseFps;
    private double _currentFps = BaseFps;
    private bool _isFloatingWindowVisible = false;
    private MonitoringMode _monitoringMode = MonitoringMode.Adaptive;

    public CatWindow()
    {
        try
        {
            InitializeComponent();
            
            // Setup hover events for floating window
            FloatingWindowContent.MouseEnter += OnFloatingWindowMouseEnter;
            FloatingWindowContent.MouseLeave += OnFloatingWindowMouseLeave;
            
            // Load icon and GIF frames
            LoadIconFrames();
            LoadGifFrames();
            
            // Load settings and position
            LoadSettings();
            
            // Apply loaded visibility state
            if (_isFloatingWindowVisible)
            {
                ShowFloatingWindow();
            }
            else
            {
                HideFloatingWindow();
            }
            
            // Apply startup settings to menu
            UpdateMenuChecks();
            
            // Initialize GPU and CPU monitors
            _gpuMonitor = new UniversalGpuMonitor(500);
            _gpuMonitor.OnGpuInfoUpdated += OnGpuInfoUpdated;
            
            _cpuMonitor = new CpuMonitor(500);
            _cpuMonitor.OnCpuUsageUpdated += OnCpuUsageUpdated;
            
            // Setup animation timer
            _animationTimer = new DispatcherTimer();
            _animationTimer.Interval = TimeSpan.FromMilliseconds(1000 / _baseFps);
            _animationTimer.Tick += OnAnimationTick;
            _animationTimer.Start();
            
            // Save position periodically
            _positionSaveTimer = new DispatcherTimer();
            _positionSaveTimer.Interval = TimeSpan.FromSeconds(5);
            _positionSaveTimer.Tick += (s, e) => SaveSettings();
            _positionSaveTimer.Start();
            
            // Start monitoring
            _gpuMonitor.Start();
            _cpuMonitor.Start();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to start: {ex.Message}\n\n{ex.StackTrace}", "GPU Cat Monitor Error");
            throw;
        }
    }
    
    private void LoadIconFrames()
    {
        try
        {
            // Look for GIF in project folder first, then app directory
            var projectPath = System.IO.Path.Combine("d:\running cat", GifFilePath);
            
            var gifPath = System.IO.File.Exists(projectPath) 
                ? projectPath 
                : System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", GifResourceName);
            
            if (System.IO.File.Exists(gifPath))
            {
                LoadAnimatedFrames(gifPath);
            }
            else
            {
                // Try to load from embedded resource
                LoadAnimatedFramesFromResource();
            }
        }
        catch (Exception ex)
        {
            TrayIcon.ToolTipText = $"GPU Cat Monitor - Error: {ex.Message}";
        }
    }
    
    private void LoadAnimatedFramesFromResource()
    {
        try
        {
            var gifBytes = GetEmbeddedResourceBytes(GifResourceName);
            if (gifBytes == null) return;
            
            _iconFrames ??= new List<System.Drawing.Icon>();
            
            using var ms = new System.IO.MemoryStream(gifBytes);
            using var collection = new MagickImageCollection(ms);
            
            // Coalesce: Expand all frames to full size (fixes optimized GIFs)
            collection.Coalesce();
            
            foreach (var frame in collection)
            {
                frame.Resize(32, 32);
                var icon = MagickImageToIcon(frame);
                if (icon != null)
                {
                    _iconFrames.Add(icon);
                }
            }
            
            TrayIcon.ToolTipText = "GPU Cat Monitor - Running (animated)";
        }
        catch { /* Ignore errors */ }
    }
    
    private void LoadGifFrames()
    {
        try
        {
            // Look for GIF in project folder first, then app directory
            var projectPath = System.IO.Path.Combine(@"d:\running cat", GifFilePath);
            
            var gifPath = System.IO.File.Exists(projectPath) 
                ? projectPath 
                : System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", GifResourceName);
            
            if (!System.IO.File.Exists(gifPath)) return;
            
            _gifFrames = new List<BitmapSource>();
            LoadGifFramesFromFile(gifPath);
            
            if (_gifFrames.Count > 0)
            {
                CatImage.Source = _gifFrames[0];
            }
        }
        catch { /* Ignore errors */ }
    }
    
    private void LoadGifFramesFromFile(string gifPath)
    {
        using var collection = new MagickImageCollection(gifPath);
        
        // Coalesce: Expand all frames to full size (fixes optimized GIFs with partial frames)
        collection.Coalesce();
        
        foreach (var frame in collection)
        {
            // MagickImageToBitmapSource handles resize internally
            var bitmap = MagickImageToBitmapSource(frame, 100, 80);
            bitmap.Freeze();
            _gifFrames!.Add(bitmap);
        }
    }
    
    private void LoadGifFramesFromResource()
    {
        var gifBytes = GetEmbeddedResourceBytes("pixel-cat.gif");
        if (gifBytes == null) return;
        
        using var ms = new System.IO.MemoryStream(gifBytes);
        using var collection = new MagickImageCollection(ms);
        
        foreach (var frame in collection)
        {
            // MagickImageToBitmapSource handles resize internally
            var bitmap = MagickImageToBitmapSource(frame, 100, 80);
            bitmap.Freeze();
            _gifFrames!.Add(bitmap);
        }
    }
    
    private static BitmapSource MagickImageToBitmapSource(ImageMagick.IMagickImage<ushort> image, int targetWidth, int targetHeight)
    {
        // Get original dimensions
        var origWidth = (int)image.Width;
        var origHeight = (int)image.Height;
        
        // Check if we need to resize
        if (origWidth != targetWidth || origHeight != targetHeight)
        {
            // Resize to exact target dimensions
            image.Resize((uint)targetWidth, (uint)targetHeight);
        }
        
        // Get final dimensions
        var width = (int)image.Width;
        var height = (int)image.Height;
        
        var stride = width * 4;
        var pixelData = image.GetPixels().ToByteArray(PixelMapping.RGBA);
        
        // Create bitmap with actual dimensions
        var bitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Pbgra32, null);
        
        if (pixelData != null && pixelData.Length >= stride * height)
        {
            bitmap.WritePixels(new Int32Rect(0, 0, width, height), pixelData, stride, 0);
        }
        
        return bitmap;
    }
    
    private void LoadAnimatedFrames(string gifPath)
    {
        try
        {
            _iconFrames ??= new List<System.Drawing.Icon>();
            
            using var collection = new MagickImageCollection(gifPath);
            
            // Coalesce: Expand all frames to full size (fixes optimized GIFs)
            collection.Coalesce();
            
            foreach (var frame in collection)
            {
                frame.Resize(32, 32);
                var icon = MagickImageToIcon(frame);
                if (icon != null)
                {
                    _iconFrames.Add(icon);
                }
            }
            
            TrayIcon.ToolTipText = "GPU Cat Monitor - Running (animated)";
        }
        catch { /* Ignore animation errors, static icon will work */ }
    }
    
    private void OnAnimationTick(object? sender, EventArgs e)
    {
        // Always advance frame counter
        if (_gifFrames != null && _gifFrames.Count > 0)
        {
            _currentFrame = (_currentFrame + 1) % _gifFrames.Count;
            
            // Update floating window if visible
            if (_isFloatingWindowVisible && _gifFrames.Count > 0)
            {
                CatImage.Source = _gifFrames[_currentFrame];
            }
            
            // Update tray icon if we have GIF-based icons
            if (_iconFrames != null && _iconFrames.Count > 1)
            {
                var iconIndex = _currentFrame % _iconFrames.Count;
                TrayIcon.Icon = _iconFrames[iconIndex];
            }
        }
    }
    
    private void OnGpuInfoUpdated(object? sender, GpuInfo info)
    {
        _lastGpuInfo = info;
        UpdateStatsDisplay();
        
        // Calculate animation speed based on selected mode
        double usagePercent = GetActiveUsagePercent();
        var targetFps = MinFps + (usagePercent / 100.0) * MaxFpsExtra;
        _currentFps = (_currentFps * (1 - FpsSmoothing)) + (targetFps * FpsSmoothing);
        
        if (_animationTimer != null)
        {
            _animationTimer.Interval = TimeSpan.FromMilliseconds(1000 / _currentFps);
        }
    }
    
    private void OnCpuUsageUpdated(object? sender, float cpuUsage)
    {
        _lastCpuUsage = cpuUsage;
        UpdateStatsDisplay();
        
        // Update animation speed for CPU or Adaptive mode
        if (_monitoringMode != MonitoringMode.Gpu && _lastGpuInfo != null)
        {
            double usagePercent = GetActiveUsagePercent();
            var targetFps = MinFps + (usagePercent / 100.0) * MaxFpsExtra;
            _currentFps = (_currentFps * (1 - FpsSmoothing)) + (targetFps * FpsSmoothing);
            
            if (_animationTimer != null)
            {
                _animationTimer.Interval = TimeSpan.FromMilliseconds(1000 / _currentFps);
            }
        }
    }
    
    private void UpdateStatsDisplay()
    {
        Dispatcher.Invoke(() =>
        {
            if (_lastGpuInfo != null)
            {
                // Update tray tooltip
                GpuNameText.Text = $"GPU: {_lastGpuInfo.Name}";
                GpuUsageText.Text = $"{_lastGpuInfo.GpuUsage:F0}%";
                CpuUsageText.Text = $"{_lastCpuUsage:F0}%";
                TrayModeText.Text = $"Anim: {_monitoringMode}";
                TrayModeText.Foreground = _monitoringMode switch
                {
                    MonitoringMode.Gpu => System.Windows.Media.Brushes.Orange,
                    MonitoringMode.Cpu => System.Windows.Media.Brushes.Cyan,
                    _ => System.Windows.Media.Brushes.MediumPurple
                };
                
                // Color code GPU usage
                GpuUsageText.Foreground = _lastGpuInfo.GpuUsage switch
                {
                    < 30 => System.Windows.Media.Brushes.Lime,
                    < 70 => System.Windows.Media.Brushes.Yellow,
                    _ => System.Windows.Media.Brushes.Red
                };
                
                // Update floating window stats
                FloatingGpuNameText.Text = _lastGpuInfo.Name.Length > 20 
                    ? _lastGpuInfo.Name[..20] + "..." 
                    : _lastGpuInfo.Name;
                FloatingGpuUsageText.Text = $"{_lastGpuInfo.GpuUsage:F0}%";
                FloatingCpuUsageText.Text = $"CPU: {_lastCpuUsage:F0}%";
                
                // Show animation mode
                AnimationModeText.Text = $"Mode: {_monitoringMode}";
                AnimationModeText.Foreground = _monitoringMode switch
                {
                    MonitoringMode.Gpu => System.Windows.Media.Brushes.Orange,
                    MonitoringMode.Cpu => System.Windows.Media.Brushes.Cyan,
                    _ => System.Windows.Media.Brushes.MediumPurple
                };
                
                // Color code floating window based on active mode
                var activeUsage = GetActiveUsagePercent();
                FloatingGpuUsageText.Foreground = activeUsage switch
                {
                    < 30 => System.Windows.Media.Brushes.Lime,
                    < 70 => System.Windows.Media.Brushes.Yellow,
                    _ => System.Windows.Media.Brushes.Red
                };
                
                // Cat size is now fixed in XAML - no dynamic scaling
            }
        });
    }
    
    // Floating Window Controls
    private void OnToggleFloatingWindow(object sender, RoutedEventArgs e)
    {
        if (_isFloatingWindowVisible)
        {
            HideFloatingWindow();
        }
        else
        {
            ShowFloatingWindow();
        }
    }
    
    private void ShowFloatingWindow()
    {
        _isFloatingWindowVisible = true;
        Visibility = Visibility.Visible;
        WindowState = WindowState.Normal;
        ToggleWindowMenuItem.Header = "Hide Floating Cat";
        TrayIcon.ToolTipText = "GPU Cat Monitor - Floating window visible";
    }
    
    private void HideFloatingWindow()
    {
        _isFloatingWindowVisible = false;
        Visibility = Visibility.Collapsed;
        ToggleWindowMenuItem.Header = "Show Floating Cat";
        TrayIcon.ToolTipText = "GPU Cat Monitor - Running in tray only";
    }
    
    private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
        {
            DragMove();
        }
    }
    
    private void OnFloatingWindowMouseEnter(object sender, MouseEventArgs e)
    {
        StatsPopup.IsOpen = true;
    }
    
    private void OnFloatingWindowMouseLeave(object sender, MouseEventArgs e)
    {
        StatsPopup.IsOpen = false;
    }
    
    private double GetActiveUsagePercent()
    {
        return _monitoringMode switch
        {
            MonitoringMode.Gpu => _lastGpuInfo?.GpuUsage ?? 0,
            MonitoringMode.Cpu => _lastCpuUsage,
            MonitoringMode.Adaptive => Math.Max(_lastGpuInfo?.GpuUsage ?? 0, _lastCpuUsage),
            _ => 0
        };
    }

    private void LoadSettings()
    {
        var settings = SettingsManager.Load();
        
        // Setup initial mode
        _monitoringMode = settings.Mode;
        _isFloatingWindowVisible = settings.IsFloatingVisible;
        
        // Apply startup status
        if (settings.RunOnStartup != StartupManager.IsStartupEnabled())
        {
            if (settings.RunOnStartup) StartupManager.EnableStartup();
            else StartupManager.DisableStartup();
        }
        
        if (settings.Left >= 0 && settings.Top >= 0)
        {
            Left = settings.Left;
            Top = settings.Top;
        }
        else
        {
            // Default position - right side of screen, taskbar area
            var screenWidth = SystemParameters.PrimaryScreenWidth;
            var screenHeight = SystemParameters.PrimaryScreenHeight;
            var taskbarHeight = SystemParameters.PrimaryScreenHeight - SystemParameters.WorkArea.Height;
            
            Left = screenWidth - 140;
            Top = screenHeight - taskbarHeight - 120;
        }
    }
    
    private void SaveSettings()
    {
        var settings = new AppSettings
        {
            Left = Left,
            Top = Top,
            Mode = _monitoringMode,
            IsFloatingVisible = _isFloatingWindowVisible,
            RunOnStartup = StartupManager.IsStartupEnabled()
        };
        SettingsManager.Save(settings);
    }
    
    private void UpdateMenuChecks()
    {
        if (ModeAdaptiveMenuItem != null) ModeAdaptiveMenuItem.IsChecked = _monitoringMode == MonitoringMode.Adaptive;
        if (ModeGpuMenuItem != null) ModeGpuMenuItem.IsChecked = _monitoringMode == MonitoringMode.Gpu;
        if (ModeCpuMenuItem != null) ModeCpuMenuItem.IsChecked = _monitoringMode == MonitoringMode.Cpu;
        if (RunOnStartupMenuItem != null) RunOnStartupMenuItem.IsChecked = StartupManager.IsStartupEnabled();
    }
    
    // Menu event handlers
    private void OnTrayDoubleClick(object sender, EventArgs e)
    {
        OnToggleFloatingWindow(sender, new RoutedEventArgs());
        SaveSettings();
    }
    
    private void OnModeSelected(object sender, RoutedEventArgs e)
    {
        if (sender == ModeAdaptiveMenuItem) _monitoringMode = MonitoringMode.Adaptive;
        else if (sender == ModeGpuMenuItem) _monitoringMode = MonitoringMode.Gpu;
        else if (sender == ModeCpuMenuItem) _monitoringMode = MonitoringMode.Cpu;
        
        UpdateMenuChecks();
        SaveSettings();
        
        TrayIcon.ToolTipText = $"GPU Cat Monitor - Animation based on {_monitoringMode}";
        if (_lastGpuInfo != null) UpdateStatsDisplay();
    }
    
    private void OnToggleStartup(object sender, RoutedEventArgs e)
    {
        bool enable = RunOnStartupMenuItem.IsChecked;
        if (enable) StartupManager.EnableStartup();
        else StartupManager.DisableStartup();
        
        SaveSettings();
    }
    
    private void OnExit(object sender, RoutedEventArgs e)
    {
        Close();
    }
    
    private static System.Drawing.Icon? MagickImageToIcon(ImageMagick.IMagickImage<ushort> image)
    {
        try
        {
            // Resize to 32x32 for system tray
            image.Resize(32, 32);
            
            var width = (int)image.Width;
            var height = (int)image.Height;
            
            // Create bitmap with ARGB format for transparency
            using var bitmap = new System.Drawing.Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            
            var pixelData = image.GetPixels().ToByteArray(PixelMapping.RGBA);
            if (pixelData == null || pixelData.Length == 0) return null;
            
            // Copy pixels preserving original transparency
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int idx = (y * width + x) * 4;
                    if (idx + 3 >= pixelData.Length) continue;
                    
                    byte r = pixelData[idx];
                    byte g = pixelData[idx + 1];
                    byte b = pixelData[idx + 2];
                    byte a = pixelData[idx + 3];
                    
                    // Preserve original pixel exactly as-is
                    bitmap.SetPixel(x, y, System.Drawing.Color.FromArgb(a, r, g, b));
                }
            }
            
            // Convert to icon
            var hIcon = bitmap.GetHicon();
            return System.Drawing.Icon.FromHandle(hIcon);
        }
        catch { return null; }
    }
    
    private static byte[]? GetEmbeddedResourceBytes(string resourceName)
    {
        try
        {
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            var resourcePath = assembly.GetManifestResourceNames()
                .FirstOrDefault(r => r.Contains(resourceName));
            
            if (resourcePath == null) return null;
            
            using var stream = assembly.GetManifestResourceStream(resourcePath);
            if (stream == null) return null;
            
            using var ms = new System.IO.MemoryStream();
            stream.CopyTo(ms);
            return ms.ToArray();
        }
        catch { return null; }
    }
    
    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        _animationTimer?.Stop();
        _gpuMonitor?.Stop();
        _cpuMonitor?.Stop();
        _gpuMonitor?.Dispose();
        _cpuMonitor?.Dispose();
        
        // Dispose all icon frames
        if (_iconFrames != null)
        {
            foreach (var icon in _iconFrames)
            {
                icon?.Dispose();
            }
        }
        
        TrayIcon?.Dispose();
        base.OnClosing(e);
    }
}
