using Avalonia.Controls;
using MSPaintEx.Controls;
using Avalonia.Media;
using Avalonia.Interactivity;
using Avalonia.Controls.Shapes;

namespace MSPaintEx.Views;

public partial class MainWindow : Window
{
    // Reference to our drawing canvas
    private DrawingCanvas? _canvas;
    private Rectangle? _currentColorDisplay;

    public MainWindow()
    {
        InitializeComponent();
        
        // Get reference to the canvas after initialization
        _canvas = this.FindControl<DrawingCanvas>("Canvas");
        _currentColorDisplay = this.FindControl<Rectangle>("CurrentColorDisplay");
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
}