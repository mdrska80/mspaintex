using Avalonia.Controls;
using MSPaintEx.Controls;
using Avalonia.Media;
using Avalonia.Interactivity;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using System;

namespace MSPaintEx.Views;

public partial class MainWindow : Window
{
    // Reference to our drawing canvas
    private DrawingCanvas? _canvas;
    private Rectangle? _currentColorDisplay;
    private ScaleTransform? _canvasScale;
    private TextBlock? _zoomLevel;
    private Grid? _canvasContainer;
    private Grid? _scrollContent;
    private ScrollViewer? _scrollViewer;
    private decimal _currentZoom = 100m;
    private const decimal MIN_ZOOM = 10m;
    private const decimal MAX_ZOOM = 3200m;

    public MainWindow()
    {
        InitializeComponent();
        
        // Get reference to the canvas after initialization
        _canvas = this.FindControl<DrawingCanvas>("Canvas");
        _currentColorDisplay = this.FindControl<Rectangle>("CurrentColorDisplay");
        _canvasContainer = this.FindControl<Grid>("CanvasContainer");
        _scrollContent = this.FindControl<Grid>("ScrollContent");
        _zoomLevel = this.FindControl<TextBlock>("ZoomLevel");
        _scrollViewer = this.FindControl<ScrollViewer>("CanvasScrollViewer");

        if (_canvasContainer != null)
        {
            _canvasScale = _canvasContainer.RenderTransform as ScaleTransform;
        }

        if (_scrollViewer != null)
        {
            _scrollViewer.PointerWheelChanged += OnScrollViewerPointerWheelChanged;
        }
    }

    private void UpdateZoom(decimal newZoom)
    {
        _currentZoom = Math.Max(MIN_ZOOM, Math.Min(MAX_ZOOM, newZoom));
        
        if (_zoomLevel != null)
        {
            _zoomLevel.Text = $"{_currentZoom:0}%";
        }

        if (_canvasScale != null)
        {
            double zoomFactor = (double)_currentZoom / 100.0;
            _canvasScale.ScaleX = zoomFactor;
            _canvasScale.ScaleY = zoomFactor;

            // Update the scroll content size to match the zoomed canvas
            if (_scrollContent != null)
            {
                _scrollContent.Width = 800 * zoomFactor;
                _scrollContent.Height = 600 * zoomFactor;
            }
        }
    }

    private void OnScrollViewerPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        if (e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            e.Handled = true;
            var delta = e.Delta.Y > 0 ? 10m : -10m; // 10% increment/decrement
            UpdateZoom(_currentZoom + delta);
        }
    }

    private void OnColorButtonClick(object? sender, RoutedEventArgs e)
    {
        if (_canvas == null || _currentColorDisplay == null || sender is not Button button) return;

        // Get color name from button's Tag
        var colorName = button.Tag?.ToString() ?? "Black";
        
        // Convert color name to Color
        var color = Color.Parse(colorName);
        
        // Update canvas color
        _canvas.SetColor(color);
        
        // Update current color display
        _currentColorDisplay.Fill = new SolidColorBrush(color);

        // Update text color for visibility
        var textBlock = button.FindControl<TextBlock>("CurrentColorText");
        if (textBlock != null)
        {
            // Use white text for dark colors, black text for light colors
            var brightness = (color.R * 299 + color.G * 587 + color.B * 114) / 1000;
            textBlock.Foreground = brightness < 128 ? Brushes.White : Brushes.Black;
        }
    }

    private void OnStrokeWidthChanged(object? sender, NumericUpDownValueChangedEventArgs e)
    {
        if (_canvas == null || !e.NewValue.HasValue) return;

        _canvas.SetStrokeWidth((float)e.NewValue.Value);
    }

    private void OnColorSelected(object? sender, Color color)
    {
        if (Canvas != null)
        {
            Canvas.SetColor(color);
        }
    }

    private void OnResetZoom(object? sender, RoutedEventArgs e)
    {
        UpdateZoom(100m);
    }
}