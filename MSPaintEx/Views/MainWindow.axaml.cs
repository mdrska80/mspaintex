using Avalonia.Controls;
using MSPaintEx.Controls;
using Avalonia.Media;
using Avalonia.Interactivity;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using System;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using MSPaintEx.Services;
using System.Linq;

namespace MSPaintEx.Views;

public partial class MainWindow : Window
{
    private const string LOG_SOURCE = "MainWindow";
    
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
        try
        {
            LogService.LogInfo(LOG_SOURCE, "Initializing MainWindow");
            InitializeComponent();
            
            // Get reference to the canvas after initialization
            _canvas = this.FindControl<DrawingCanvas>("Canvas");
            _currentColorDisplay = this.FindControl<Rectangle>("CurrentColorDisplay");
            _canvasContainer = this.FindControl<Grid>("CanvasContainer");
            _scrollContent = this.FindControl<Grid>("ScrollContent");
            _zoomLevel = this.FindControl<TextBlock>("ZoomLevel");
            _scrollViewer = this.FindControl<ScrollViewer>("CanvasScrollViewer");

            if (_canvas == null)
            {
                LogService.LogError(LOG_SOURCE, "Canvas control not found");
                throw new InvalidOperationException("Canvas not found");
            }
            if (_canvasContainer == null)
            {
                LogService.LogError(LOG_SOURCE, "CanvasContainer control not found");
                throw new InvalidOperationException("CanvasContainer not found");
            }
            if (_scrollContent == null)
            {
                LogService.LogError(LOG_SOURCE, "ScrollContent control not found");
                throw new InvalidOperationException("ScrollContent not found");
            }
            if (_zoomLevel == null)
            {
                LogService.LogError(LOG_SOURCE, "ZoomLevel control not found");
                throw new InvalidOperationException("ZoomLevel not found");
            }
            if (_scrollViewer == null)
            {
                LogService.LogError(LOG_SOURCE, "ScrollViewer control not found");
                throw new InvalidOperationException("ScrollViewer not found");
            }

            // Initialize canvas scale
            _canvasScale = _canvasContainer.RenderTransform as ScaleTransform;
            if (_canvasScale == null)
            {
                LogService.LogError(LOG_SOURCE, "Canvas scale transform not found");
                throw new InvalidOperationException("Canvas scale transform not found");
            }

            // Setup event handlers
            _scrollViewer.PointerWheelChanged += OnScrollViewerPointerWheelChanged;
            
            // Initialize canvas
            _canvas.Clear();

            // Ensure keyboard shortcuts work by handling them directly
            this.KeyDown += (s, e) => 
            {
                if (e.Key == Key.E && e.KeyModifiers == KeyModifiers.Control)
                {
                    OnResizeCanvasClick(this, new RoutedEventArgs());
                    e.Handled = true;
                }
            };

            LogService.LogInfo(LOG_SOURCE, "MainWindow initialized successfully");
        }
        catch (Exception ex)
        {
            LogService.LogError(LOG_SOURCE, "Failed to initialize MainWindow", ex);
            throw; // Rethrow to crash the app - we can't recover from initialization failure
        }
    }

    private void UpdateZoom(decimal newZoom)
    {
        try
        {
            LogService.LogInfo(LOG_SOURCE, $"Updating zoom to {newZoom}%");
            _currentZoom = Math.Max(MIN_ZOOM, Math.Min(MAX_ZOOM, newZoom));
            
            if (_zoomLevel == null)
            {
                LogService.LogWarning(LOG_SOURCE, "ZoomLevel control is null during zoom update");
                return;
            }
            _zoomLevel.Text = $"{_currentZoom:0}%";

            if (_canvasScale == null || _scrollContent == null)
            {
                LogService.LogWarning(LOG_SOURCE, "CanvasScale or ScrollContent is null during zoom update");
                return;
            }
            
            double zoomFactor = (double)_currentZoom / 100.0;
            _canvasScale.ScaleX = zoomFactor;
            _canvasScale.ScaleY = zoomFactor;

            // Update the scroll content size to match the zoomed canvas
            _scrollContent.Width = 800 * zoomFactor;
            _scrollContent.Height = 600 * zoomFactor;
            LogService.LogInfo(LOG_SOURCE, "Zoom updated successfully");
        }
        catch (Exception ex)
        {
            LogService.LogError(LOG_SOURCE, "Failed to update zoom", ex);
        }
    }

    private void OnScrollViewerPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        try
        {
            if (e.KeyModifiers.HasFlag(KeyModifiers.Control))
            {
                e.Handled = true;
                var delta = e.Delta.Y > 0 ? 10m : -10m; // 10% increment/decrement
                LogService.LogInfo(LOG_SOURCE, $"Mouse wheel zoom delta: {delta}");
                UpdateZoom(_currentZoom + delta);
            }
        }
        catch (Exception ex)
        {
            LogService.LogError(LOG_SOURCE, "Error handling mouse wheel zoom", ex);
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
        try
        {
            if (_canvas == null)
            {
                LogService.LogWarning(LOG_SOURCE, "Canvas is null during color selection");
                return;
            }
            LogService.LogInfo(LOG_SOURCE, $"Setting color to {color}");
            _canvas.SetColor(color);
        }
        catch (Exception ex)
        {
            LogService.LogError(LOG_SOURCE, "Failed to set color", ex);
        }
    }

    private void OnResetZoom(object? sender, RoutedEventArgs e)
    {
        UpdateZoom(100m);
    }

    #region Menu Event Handlers

    private void OnNewClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            LogService.LogInfo(LOG_SOURCE, "Creating new document");
            if (_canvas == null)
            {
                LogService.LogWarning(LOG_SOURCE, "Canvas is null during new document creation");
                return;
            }
            // TODO: Add confirmation dialog if there are unsaved changes
            _canvas.Clear();
            LogService.LogInfo(LOG_SOURCE, "New document created successfully");
        }
        catch (Exception ex)
        {
            LogService.LogError(LOG_SOURCE, "Failed to create new document", ex);
        }
    }

    private async void OnOpenClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            LogService.LogInfo(LOG_SOURCE, "Opening file picker for image");
            var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Open Image",
                AllowMultiple = false,
                FileTypeFilter = new[] 
                { 
                    new FilePickerFileType("Image Files")
                    {
                        Patterns = new[] { "*.png", "*.jpg", "*.jpeg", "*.bmp" }
                    }
                }
            });

            if (files.Count > 0)
            {
                LogService.LogInfo(LOG_SOURCE, $"Selected file: {files[0].Name}");
                // TODO: Implement file opening
            }
            else
            {
                LogService.LogInfo(LOG_SOURCE, "No file selected");
            }
        }
        catch (Exception ex)
        {
            LogService.LogError(LOG_SOURCE, "Failed to open file", ex);
        }
    }

    private async void OnSaveClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            await SaveAsAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error saving file: {ex}");
        }
    }

    private async void OnSaveAsClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            await SaveAsAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error saving file: {ex}");
        }
    }

    private async Task SaveAsAsync()
    {
        try
        {
            if (_canvas == null) return;

            var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Save Image As",
                DefaultExtension = ".png",
                FileTypeChoices = new[] 
                {
                    new FilePickerFileType("PNG Image") { Patterns = new[] { "*.png" } },
                    new FilePickerFileType("JPEG Image") { Patterns = new[] { "*.jpg", "*.jpeg" } },
                    new FilePickerFileType("BMP Image") { Patterns = new[] { "*.bmp" } }
                }
            });

            if (file != null)
            {
                // TODO: Implement save as functionality
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in SaveAsAsync: {ex}");
            throw; // Rethrow to be caught by the caller
        }
    }

    private void OnExitClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    private void OnUndoClick(object? sender, RoutedEventArgs e)
    {
        // TODO: Implement undo
    }

    private void OnRedoClick(object? sender, RoutedEventArgs e)
    {
        // TODO: Implement redo
    }

    private void OnCutClick(object? sender, RoutedEventArgs e)
    {
        // TODO: Implement cut
    }

    private void OnCopyClick(object? sender, RoutedEventArgs e)
    {
        // TODO: Implement copy
    }

    private void OnPasteClick(object? sender, RoutedEventArgs e)
    {
        // TODO: Implement paste
    }

    private void OnSelectAllClick(object? sender, RoutedEventArgs e)
    {
        // TODO: Implement select all
    }

    private void OnClearSelectionClick(object? sender, RoutedEventArgs e)
    {
        // TODO: Implement clear selection
    }

    private void OnZoomClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            if (sender is MenuItem menuItem)
            {
                string zoomText = menuItem.Header?.ToString() ?? "100%";
                
                // If it's the custom zoom option
                if (zoomText == "Custom...")
                {
                    // TODO: Show custom zoom dialog
                    return;
                }

                // Parse the zoom level from the text (remove the % sign)
                if (decimal.TryParse(zoomText.TrimEnd('%'), out decimal zoomLevel))
                {
                    UpdateZoom(zoomLevel);
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error handling zoom click: {ex}");
        }
    }

    private void OnGridLinesClick(object? sender, RoutedEventArgs e)
    {
        // TODO: Implement grid lines toggle
    }

    private void OnRotateFlipClick(object? sender, RoutedEventArgs e)
    {
        // TODO: Implement rotate/flip operations
    }

    private void OnResizeSkewClick(object? sender, RoutedEventArgs e)
    {
        // TODO: Implement resize/skew dialog
    }

    private void OnCanvasSizeClick(object? sender, RoutedEventArgs e)
    {
        // TODO: Implement canvas size dialog
    }

    private void OnInvertColorsClick(object? sender, RoutedEventArgs e)
    {
        // TODO: Implement color inversion
    }

    private void OnGrayscaleClick(object? sender, RoutedEventArgs e)
    {
        // TODO: Implement grayscale conversion
    }

    private async void OnAboutClick(object sender, RoutedEventArgs e)
    {
        var about = new AboutWindow();
        LogService.LogInfo("MainWindow", "Opening About dialog");
        await about.ShowDialog(this);
    }

    private async void OnResizeCanvasClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            if (_canvas == null)
            {
                LogService.LogWarning(LOG_SOURCE, "Canvas is null during resize attempt");
                return;
            }

            var resizeWindow = new ResizeCanvasWindow((int)_canvas.Width, (int)_canvas.Height);
            var result = await resizeWindow.ShowDialog<ResizeResult>(this);

            if (result?.Confirmed == true)
            {
                LogService.LogInfo(LOG_SOURCE, $"Resizing canvas to {result.Width}x{result.Height}");
                
                // First resize the bitmap in the canvas
                _canvas.Resize(result.Width, result.Height);
                
                // Then update all the UI elements
                _canvas.Width = result.Width;
                _canvas.Height = result.Height;
                
                if (_canvasContainer != null)
                {
                    _canvasContainer.Width = result.Width;
                    _canvasContainer.Height = result.Height;
                    
                    // Update background rectangle size
                    var backgroundRect = _canvasContainer.Children.OfType<Rectangle>().FirstOrDefault();
                    if (backgroundRect != null)
                    {
                        backgroundRect.Width = result.Width;
                        backgroundRect.Height = result.Height;
                    }
                }
                
                // Update scroll content size based on current zoom
                if (_scrollContent != null)
                {
                    double zoomFactor = (double)_currentZoom / 100.0;
                    _scrollContent.Width = result.Width * zoomFactor;
                    _scrollContent.Height = result.Height * zoomFactor;
                }

                // Force a visual update
                _canvas.InvalidateVisual();
                if (_canvasContainer != null) _canvasContainer.InvalidateVisual();
                if (_scrollContent != null) _scrollContent.InvalidateVisual();
            }
        }
        catch (Exception ex)
        {
            LogService.LogError(LOG_SOURCE, "Error resizing canvas", ex);
        }
    }

    private void OnTitleBarPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        // This method is no longer needed with native window decorations
    }

    private void OnMinimizeClick(object? sender, RoutedEventArgs e)
    {
        // This method is no longer needed with native window decorations
    }

    private void OnMaximizeClick(object? sender, RoutedEventArgs e)
    {
        // This method is no longer needed with native window decorations
    }

    #endregion
}