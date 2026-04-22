using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using GpuCatMonitor.Core.Models;
using GpuCatMonitor.Core.Services;
using ImageMagick;

namespace GpuCatMonitor.Views;

public partial class CatWindow : Window
{
    private readonly UniversalGpuMonitor _gpuMonitor;
    private readonly CpuMonitor _cpuMonitor;
    private List<BitmapSource>? _gifFrames;
    private int _currentFrame = 0;
    private DispatcherTimer? _animationTimer;
    private GpuInfo? _lastGpuInfo;
    private float _lastCpuUsage;
    
    private double _baseFps = 12; // Base animation FPS
    private double _currentFps = 12;

    public CatWindow()
    {
        InitializeComponent();
        
        // Initialize GPU and CPU monitors
        _gpuMonitor = new UniversalGpuMonitor(500);
        _gpuMonitor.OnGpuInfoUpdated += OnGpuInfoUpdated;
        
        _cpuMonitor = new CpuMonitor(500);
        _cpuMonitor.OnCpuUsageUpdated += OnCpuUsageUpdated;
        
        // Load and decode GIF
        LoadGifFrames();
        
        // Setup animation timer
        _animationTimer = new DispatcherTimer();
        _animationTimer.Interval = TimeSpan.FromMilliseconds(1000 / _baseFps);
        _animationTimer.Tick += OnAnimationTick;
        _animationTimer.Start();
        
        // Start monitoring
        _gpuMonitor.Start();
        _cpuMonitor.Start();
    }
    
    private void LoadGifFrames()
    {
        try
        {
            var gifPath = System.IO.Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory, 
                "Resources", "pixel-cat.gif");
            
            if (!System.IO.File.Exists(gifPath))
            {
                gifPath = @"d:\running cat\gif\pixel-cat.gif";
            }
            
            if (!System.IO.File.Exists(gifPath))
            {
                TrayIcon.ToolTipText = "GPU Cat Monitor - GIF not found";
                return;
            }
            
            _gifFrames = new List<BitmapSource>();
            
            using var collection = new MagickImageCollection(gifPath);
            
            foreach (var frame in collection)
            {
                frame.Scale(80, 60);
                var bitmap = MagickImageToBitmapSource(frame);
                bitmap.Freeze();
                _gifFrames.Add(bitmap);
            }
            
            if (_gifFrames.Count > 0)
            {
                TooltipCatImage.Source = _gifFrames[0];
            }
        }
        catch (Exception ex)
        {
            TrayIcon.ToolTipText = $"GPU Cat Monitor - Error: {ex.Message}";
        }
    }
    
    private void OnAnimationTick(object? sender, EventArgs e)
    {
        if (_gifFrames == null || _gifFrames.Count == 0) return;
        
        _currentFrame = (_currentFrame + 1) % _gifFrames.Count;
        
        // Update the tooltip cat image
        Dispatcher.Invoke(() =>
        {
            TooltipCatImage.Source = _gifFrames[_currentFrame];
        });
    }
    
    private void OnGpuInfoUpdated(object? sender, GpuInfo info)
    {
        _lastGpuInfo = info;
        UpdateStatsDisplay();
        
        // Calculate animation speed based on GPU usage
        var targetFps = 5 + (info.GpuUsage / 100.0) * 25;
        _currentFps = (_currentFps * 0.8) + (targetFps * 0.2);
        
        if (_animationTimer != null)
        {
            _animationTimer.Interval = TimeSpan.FromMilliseconds(1000 / _currentFps);
        }
    }
    
    private void OnCpuUsageUpdated(object? sender, float cpuUsage)
    {
        _lastCpuUsage = cpuUsage;
        UpdateStatsDisplay();
    }
    
    private void UpdateStatsDisplay()
    {
        Dispatcher.Invoke(() =>
        {
            if (_lastGpuInfo != null)
            {
                GpuNameText.Text = $"GPU: {_lastGpuInfo.Name}";
                GpuUsageText.Text = $"{_lastGpuInfo.GpuUsage:F0}%";
                CpuUsageText.Text = $"{_lastCpuUsage:F0}%";
                
                // Color code GPU usage
                GpuUsageText.Foreground = _lastGpuInfo.GpuUsage switch
                {
                    < 30 => Brushes.Lime,
                    < 70 => Brushes.Yellow,
                    _ => Brushes.Red
                };
            }
        });
    }
    
    // Menu event handlers
    private void OnShowAnimation(object sender, RoutedEventArgs e)
    {
        MessageBox.Show("Cat animation is always running in the system tray! Hover over the icon to see it.");
    }
    
    private void OnTrayDoubleClick(object sender, EventArgs e)
    {
        // Could open a settings window here
        MessageBox.Show("GPU Cat Monitor is running!\n\nHover over the tray icon to see GPU/CPU stats and animated cat.");
    }
    
    private void OnSettings(object sender, RoutedEventArgs e)
    {
        MessageBox.Show("Settings coming soon!\n\nPlanned features:\n- Custom animations\n- Startup with Windows\n- Color themes");
    }
    
    private void OnExit(object sender, RoutedEventArgs e)
    {
        Close();
    }
    
    private static BitmapSource MagickImageToBitmapSource(ImageMagick.IMagickImage<ushort> image)
    {
        var width = (int)image.Width;
        var height = (int)image.Height;
        var stride = width * 4; // RGBA
        var pixelData = image.GetPixels().ToByteArray(PixelMapping.RGBA);
        
        var bitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Pbgra32, null);
        bitmap.WritePixels(new Int32Rect(0, 0, width, height), pixelData, stride, 0);
        
        return bitmap;
    }
    
    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        _animationTimer?.Stop();
        _gpuMonitor?.Stop();
        _cpuMonitor?.Stop();
        _gpuMonitor?.Dispose();
        _cpuMonitor?.Dispose();
        TrayIcon?.Dispose();
        base.OnClosing(e);
    }
}
