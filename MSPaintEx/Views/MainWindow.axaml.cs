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
using SkiaSharp;
using System.IO;

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
    
    // Track the current file for Save operations
    private IStorageFile? _currentFile;

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
                else if (e.Key == Key.A && (e.KeyModifiers == KeyModifiers.Control || e.KeyModifiers == KeyModifiers.Meta))
                {
                    OnSelectAllClick(this, new RoutedEventArgs());
                    e.Handled = true;
                }
                else if (e.Key == Key.Escape)
                {
                    OnClearSelectionClick(this, new RoutedEventArgs());
                    e.Handled = true;
                }
                else if (e.Key == Key.X && (e.KeyModifiers == KeyModifiers.Control || e.KeyModifiers == KeyModifiers.Meta))
                {
                    OnCutClick(this, new RoutedEventArgs());
                    e.Handled = true;
                }
                else if (e.Key == Key.C && (e.KeyModifiers == KeyModifiers.Control || e.KeyModifiers == KeyModifiers.Meta))
                {
                    OnCopyClick(this, new RoutedEventArgs());
                    e.Handled = true;
                }
                else if (e.Key == Key.V && (e.KeyModifiers == KeyModifiers.Control || e.KeyModifiers == KeyModifiers.Meta))
                {
                    OnPasteClick(this, new RoutedEventArgs());
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

    private async void OnNewClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            LogService.LogInfo(LOG_SOURCE, "Creating new document");
            if (_canvas == null)
            {
                LogService.LogWarning(LOG_SOURCE, "Canvas is null during new document creation");
                return;
            }
            
            // For now, just create a new document without confirmation
            // In a real application, you would add a confirmation dialog here
            
            _canvas.Clear();
            
            // Reset current file
            _currentFile = null;
            
            // Reset window title
            Title = "MSPaintEx";
            LogService.LogInfo(LOG_SOURCE, "Reset window title to: MSPaintEx");
            
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
                        Patterns = new[] { "*.png", "*.jpg", "*.jpeg", "*.bmp", "*.gif" }
                    }
                }
            });

            if (files.Count > 0)
            {
                LogService.LogInfo(LOG_SOURCE, $"Selected file: {files[0].Name}");
                
                // Update window title with file path
                string filePath = files[0].Path.LocalPath;
                Title = $"MSPaintEx - {filePath}";
                LogService.LogInfo(LOG_SOURCE, $"Updated window title to: {Title}");
                
                // Load the image using SkiaSharp
                using var stream = await files[0].OpenReadAsync();
                using var memoryStream = new System.IO.MemoryStream();
                await stream.CopyToAsync(memoryStream);
                memoryStream.Position = 0;
                
                // Decode the image using SkiaSharp
                var skBitmap = SKBitmap.Decode(memoryStream);
                
                if (skBitmap != null)
                {
                    // Get image dimensions
                    int imageWidth = skBitmap.Width;
                    int imageHeight = skBitmap.Height;
                    
                    LogService.LogInfo(LOG_SOURCE, $"Image dimensions: {imageWidth}x{imageHeight}");
                    
                    // Resize canvas to match image dimensions
                    if (_canvas != null)
                    {
                        // Resize the canvas bitmap
                        _canvas.Resize(imageWidth, imageHeight);
                        
                        // Update canvas dimensions
                        _canvas.Width = imageWidth;
                        _canvas.Height = imageHeight;
                        
                        // Draw the loaded image onto the canvas
                        _canvas.DrawBitmap(skBitmap);
                        
                        // Update container dimensions
                        if (_canvasContainer != null)
                        {
                            _canvasContainer.Width = imageWidth;
                            _canvasContainer.Height = imageHeight;
                            
                            // Update background rectangle size
                            var backgroundRect = _canvasContainer.Children.OfType<Rectangle>().FirstOrDefault();
                            if (backgroundRect != null)
                            {
                                backgroundRect.Width = imageWidth;
                                backgroundRect.Height = imageHeight;
                            }
                        }
                        
                        // Update scroll content size based on current zoom
                        if (_scrollContent != null)
                        {
                            double zoomFactor = (double)_currentZoom / 100.0;
                            _scrollContent.Width = imageWidth * zoomFactor;
                            _scrollContent.Height = imageHeight * zoomFactor;
                        }
                        
                        // Force a visual update
                        _canvas.InvalidateVisual();
                        if (_canvasContainer != null) _canvasContainer.InvalidateVisual();
                        if (_scrollContent != null) _scrollContent.InvalidateVisual();
                        
                        LogService.LogInfo(LOG_SOURCE, "Image loaded successfully");
                    }
                }
                else
                {
                    LogService.LogError(LOG_SOURCE, "Failed to decode image");
                }
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
            LogService.LogInfo(LOG_SOURCE, "Save requested");
            
            if (_currentFile != null)
            {
                // We have a current file, save to it
                await SaveCanvasToFile(_currentFile);
                LogService.LogInfo(LOG_SOURCE, $"Saved to existing file: {_currentFile.Name}");
            }
            else
            {
                // No current file, use Save As
                await SaveAsAsync();
            }
        }
        catch (Exception ex)
        {
            LogService.LogError(LOG_SOURCE, "Error saving file", ex);
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
                await SaveCanvasToFile(file);
                
                // Store the current file for future "Save" operations
                _currentFile = file;
                
                // Update window title with file path
                string filePath = file.Path.LocalPath;
                Title = $"MSPaintEx - {filePath}";
                LogService.LogInfo(LOG_SOURCE, $"Updated window title to: {Title}");
            }
        }
        catch (Exception ex)
        {
            LogService.LogError(LOG_SOURCE, $"Error in SaveAsAsync: {ex}");
            throw; // Rethrow to be caught by the caller
        }
    }
    
    private async Task SaveCanvasToFile(IStorageFile file)
    {
        if (_canvas == null || file == null) return;
        
        LogService.LogInfo(LOG_SOURCE, $"Saving image to {file.Name}");
        
        try
        {
            // Create a memory stream to save the bitmap
            using var memoryStream = new MemoryStream();
            
            // Determine the format based on file extension
            string extension = System.IO.Path.GetExtension(file.Name).ToLowerInvariant();
            SKEncodedImageFormat format = SKEncodedImageFormat.Png; // Default
            
            switch (extension)
            {
                case ".jpg":
                case ".jpeg":
                    format = SKEncodedImageFormat.Jpeg;
                    break;
                case ".bmp":
                    format = SKEncodedImageFormat.Bmp;
                    break;
                case ".png":
                default:
                    format = SKEncodedImageFormat.Png;
                    break;
            }
            
            // Get the SKBitmap from the canvas and encode it
            var skBitmap = _canvas.GetBitmap();
            if (skBitmap != null)
            {
                skBitmap.Encode(memoryStream, format, 100);
                memoryStream.Position = 0;
                
                // Write the memory stream to the file
                using var fileStream = await file.OpenWriteAsync();
                await memoryStream.CopyToAsync(fileStream);
                
                LogService.LogInfo(LOG_SOURCE, $"Image saved successfully to {file.Name}");
            }
            else
            {
                LogService.LogError(LOG_SOURCE, "Failed to get bitmap from canvas");
            }
        }
        catch (Exception ex)
        {
            LogService.LogError(LOG_SOURCE, $"Error saving file: {ex}");
            throw;
        }
    }

    private async void OnExitClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            LogService.LogInfo(LOG_SOURCE, "Exit requested");
            
            // For now, just exit without confirmation
            // In a real application, you would add a confirmation dialog here
            // if there are unsaved changes
            
            Close();
        }
        catch (Exception ex)
        {
            LogService.LogError(LOG_SOURCE, "Error during exit", ex);
        }
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
        try
        {
            if (_canvas == null)
            {
                LogService.LogWarning(LOG_SOURCE, "Canvas is null during cut operation");
                return;
            }
            
            LogService.LogInfo(LOG_SOURCE, "Cutting selection to clipboard");
            _canvas.Cut();
        }
        catch (Exception ex)
        {
            LogService.LogError(LOG_SOURCE, "Error during cut operation", ex);
        }
    }

    private void OnCopyClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            if (_canvas == null)
            {
                LogService.LogWarning(LOG_SOURCE, "Canvas is null during copy operation");
                return;
            }
            
            LogService.LogInfo(LOG_SOURCE, "Copying selection to clipboard");
            _canvas.Copy();
        }
        catch (Exception ex)
        {
            LogService.LogError(LOG_SOURCE, "Error during copy operation", ex);
        }
    }

    private async void OnPasteClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            if (_canvas == null)
            {
                LogService.LogWarning(LOG_SOURCE, "Canvas is null during paste operation");
                return;
            }
            
            LogService.LogInfo(LOG_SOURCE, "Pasting from clipboard");
            await _canvas.PasteAsync();
        }
        catch (Exception ex)
        {
            LogService.LogError(LOG_SOURCE, "Error during paste operation", ex);
        }
    }

    private void OnSelectAllClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            if (_canvas == null)
            {
                LogService.LogWarning(LOG_SOURCE, "Canvas is null during select all operation");
                return;
            }
            
            LogService.LogInfo(LOG_SOURCE, "Selecting all content");
            _canvas.SelectAll();
        }
        catch (Exception ex)
        {
            LogService.LogError(LOG_SOURCE, "Error during select all operation", ex);
        }
    }

    private void OnClearSelectionClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            if (_canvas == null)
            {
                LogService.LogWarning(LOG_SOURCE, "Canvas is null during clear selection operation");
                return;
            }
            
            LogService.LogInfo(LOG_SOURCE, "Clearing selection");
            _canvas.ClearSelection();
        }
        catch (Exception ex)
        {
            LogService.LogError(LOG_SOURCE, "Error during clear selection operation", ex);
        }
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

            // Ensure we have valid dimensions
            int canvasWidth = Math.Max(1, (int)_canvas.Width);
            int canvasHeight = Math.Max(1, (int)_canvas.Height);
            
            LogService.LogInfo(LOG_SOURCE, $"Opening resize dialog with current size: {canvasWidth}x{canvasHeight}");
            var resizeWindow = new ResizeCanvasWindow(canvasWidth, canvasHeight);
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