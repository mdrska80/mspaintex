using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Platform;
using SkiaSharp;
using System;
using Avalonia.VisualTree;
using Avalonia.Controls.Primitives;
using Avalonia.Input;  // Add this for mouse handling
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Input.Platform;
using System.IO;
using MSPaintEx.Tools; // Use the Tools namespace
using Avalonia.Threading; // Add this for Dispatcher

namespace MSPaintEx.Controls
{
    // DrawingCanvas: Custom control that provides basic drawing functionality
    public class DrawingCanvas : TemplatedControl  // Change to TemplatedControl which has Background property
    {
        // Define StrokeThickness as an AvaloniaProperty
        public static readonly StyledProperty<double> StrokeThicknessProperty =
            AvaloniaProperty.Register<DrawingCanvas, double>(nameof(StrokeThickness), 1.0);

        public double StrokeThickness
        {
            get => GetValue(StrokeThicknessProperty);
            set => SetValue(StrokeThicknessProperty, value);
        }

        // The bitmap that represents our drawing surface
        private SKBitmap? _bitmap;
        
        // Drawing settings
        private SKPaint _paint;
        private SKPoint? _lastPoint;  // Track last point for drawing
        private bool _isDrawing;      // Track if we're currently drawing
        
        // Selection properties
        private SKPaint _selectionPaint;
        
        // Temporary bitmap for preview
        private SKBitmap? _tempBitmap;

        // The canvas size properties
        private const int DEFAULT_WIDTH = 800;
        private const int DEFAULT_HEIGHT = 600;

        // Tool manager
        private ToolManager _toolManager;

        // Event for color selection
        public event EventHandler<SKColor>? ColorSelected;

        public DrawingCanvas()
        {
            InitializeBitmap(DEFAULT_WIDTH, DEFAULT_HEIGHT);
            Background = Brushes.White;
            
            // Initialize paint
            _paint = new SKPaint
            {
                Style = SKPaintStyle.Stroke,
                Color = SKColors.Black,
                StrokeWidth = 1,
                IsAntialias = true
            };
            
            // Initialize selection paint
            _selectionPaint = new SKPaint
            {
                Style = SKPaintStyle.Stroke,
                Color = SKColors.Black,
                StrokeWidth = 1,
                PathEffect = SKPathEffect.CreateDash(new float[] { 5, 5 }, 0),
                IsAntialias = true
            };
            
            // Initialize tool manager
            _toolManager = new ToolManager();
            _toolManager.ColorSelected += OnColorSelected;
            
            this.PointerPressed += OnPointerPressed;
            this.PointerMoved += OnPointerMoved;
            this.PointerReleased += OnPointerReleased;
            this.KeyDown += OnKeyDown;
            this.Focusable = true;
        }

        // Helper method to get the current zoom factor from the parent window
        private double GetCurrentZoomFactor()
        {
            // Find the parent window
            var window = this.GetVisualRoot() as MSPaintEx.Views.MainWindow;
            if (window != null)
            {
                // Get the zoom level from the window
                var zoomText = window.FindControl<TextBlock>("ZoomLevel")?.Text;
                if (zoomText != null && zoomText.EndsWith("%"))
                {
                    if (decimal.TryParse(zoomText.TrimEnd('%'), out decimal zoomLevel))
                    {
                        return (double)(zoomLevel / 100m);
                    }
                }
            }
            
            // Default to 1.0 (100%) if we can't determine the zoom level
            return 1.0;
        }

        // Handle color selection event from tool manager
        private void OnColorSelected(object? sender, SKColor color)
        {
            // Forward the event to subscribers
            ColorSelected?.Invoke(this, color);
        }

        // Override property changed to handle bounds changes
        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == BoundsProperty)
            {
                var bounds = (Rect)change.NewValue!;
                if (bounds.Width > 0 && bounds.Height > 0)
                {
                    Resize((int)bounds.Width, (int)bounds.Height);
                }
            }
        }

        // Handle pointer (mouse) pressed event
        private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (_bitmap == null) return;

            var point = e.GetPosition(this);
            double zoomFactor = GetCurrentZoomFactor();
            
            // Convert point to bitmap coordinates, accounting for zoom
            var x = (float)(point.X * _bitmap.Width / (Bounds.Width * zoomFactor));
            var y = (float)(point.Y * _bitmap.Height / (Bounds.Height * zoomFactor));
            
            System.Diagnostics.Debug.WriteLine($"Pointer pressed at screen: {point.X}, {point.Y}, bitmap: {x}, {y}, zoom: {zoomFactor}");
            
            // Create a temporary bitmap for preview if needed
            if (_toolManager.CurrentTool == DrawingTool.Line ||
                _toolManager.CurrentTool == DrawingTool.Rectangle ||
                _toolManager.CurrentTool == DrawingTool.Ellipse ||
                _toolManager.CurrentTool == DrawingTool.RoundedRectangle ||
                _toolManager.CurrentTool == DrawingTool.Polygon)
            {
                _tempBitmap = _bitmap.Copy();
            }
            
            // Pass the event to the tool manager
            _toolManager.OnPointerPressed(new SKPoint(x, y), _bitmap);
            
            // Update the last point
            _lastPoint = new SKPoint(x, y);
            _isDrawing = true;
            
            InvalidateVisual();
        }
        
        // Handle pointer (mouse) moved event
        private void OnPointerMoved(object? sender, PointerEventArgs e)
        {
            if (_bitmap == null) return;
            
            var point = e.GetPosition(this);
            double zoomFactor = GetCurrentZoomFactor();
            
            // Convert point to bitmap coordinates, accounting for zoom
            var x = (float)(point.X * _bitmap.Width / (Bounds.Width * zoomFactor));
            var y = (float)(point.Y * _bitmap.Height / (Bounds.Height * zoomFactor));
            var currentPoint = new SKPoint(x, y);
            
            // Update cursor based on position
            Cursor = new Cursor(_toolManager.GetCursor(currentPoint));
            
            // Pass the event to the tool manager
            _toolManager.OnPointerMoved(currentPoint, _lastPoint, _bitmap);
            
            // Update the last point
            _lastPoint = currentPoint;
            
            InvalidateVisual();
        }
        
        // Handle pointer (mouse) released event
        private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            if (_bitmap == null) return;
            
            var point = e.GetPosition(this);
            double zoomFactor = GetCurrentZoomFactor();
            
            // Convert point to bitmap coordinates, accounting for zoom
            var x = (float)(point.X * _bitmap.Width / (Bounds.Width * zoomFactor));
            var y = (float)(point.Y * _bitmap.Height / (Bounds.Height * zoomFactor));
            
            System.Diagnostics.Debug.WriteLine($"Pointer released at screen: {point.X}, {point.Y}, bitmap: {x}, {y}, zoom: {zoomFactor}");
            
            // Debug output before passing to tool manager
            System.Diagnostics.Debug.WriteLine($"OnPointerReleased before tool manager: HasSelection={_toolManager.HasSelection}");
            
            // Pass the event to the tool manager
            _toolManager.OnPointerReleased(new SKPoint(x, y), _bitmap);
            
            // Debug output after passing to tool manager
            System.Diagnostics.Debug.WriteLine($"OnPointerReleased after tool manager: HasSelection={_toolManager.HasSelection}");
            
            // Clean up temporary resources
            _tempBitmap?.Dispose();
            _tempBitmap = null;
            _isDrawing = false;
            _lastPoint = null;
            
            // Ensure the selection is visible if one was created
            EnsureSelectionVisible();
            
            InvalidateVisual();
        }

        // Handle keyboard events for selection manipulation
        private void OnKeyDown(object? sender, KeyEventArgs e)
        {
            if (!_toolManager.HasSelection) return;
            
            // Handle arrow keys for moving selection
            int moveStep = e.KeyModifiers.HasFlag(KeyModifiers.Shift) ? 10 : 1;
            
            switch (e.Key)
            {
                case Key.Left:
                    MoveSelection(-moveStep, 0);
                    e.Handled = true;
                    break;
                case Key.Right:
                    MoveSelection(moveStep, 0);
                    e.Handled = true;
                    break;
                case Key.Up:
                    MoveSelection(0, -moveStep);
                    e.Handled = true;
                    break;
                case Key.Down:
                    MoveSelection(0, moveStep);
                    e.Handled = true;
                    break;
                case Key.Delete:
                    // Delete the selection (clear it with white)
                    if (_bitmap != null)
                    {
                        using (var canvas = new SKCanvas(_bitmap))
                        {
                            var clearPaint = new SKPaint { Color = SKColors.White };
                            canvas.DrawRect(_toolManager.SelectionRect, clearPaint);
                        }
                        ClearSelection();
                        InvalidateVisual();
                    }
                    e.Handled = true;
                    break;
                case Key.Escape:
                    // Cancel the selection
                    ClearSelection();
                    InvalidateVisual();
                    e.Handled = true;
                    break;
                case Key.R:
                    // Rotate selection 90 degrees clockwise with Ctrl+R
                    if (e.KeyModifiers.HasFlag(KeyModifiers.Control) && _toolManager.SelectionBitmap != null)
                    {
                        RotateSelection(90);
                        e.Handled = true;
                    }
                    break;
                case Key.F:
                    // Flip selection horizontally with Ctrl+F
                    if (e.KeyModifiers.HasFlag(KeyModifiers.Control) && _toolManager.SelectionBitmap != null)
                    {
                        FlipSelectionHorizontally();
                        e.Handled = true;
                    }
                    break;
                case Key.V:
                    // Flip selection vertically with Ctrl+Shift+V
                    if (e.KeyModifiers.HasFlag(KeyModifiers.Control) && 
                        e.KeyModifiers.HasFlag(KeyModifiers.Shift) && 
                        _toolManager.SelectionBitmap != null)
                    {
                        FlipSelectionVertically();
                        e.Handled = true;
                    }
                    break;
            }
        }
        
        // Rotate the selection by the specified angle in degrees
        private void RotateSelection(float angleDegrees)
        {
            if (!_toolManager.HasSelection || _toolManager.SelectionBitmap == null) return;
            
            // Create a new bitmap for the rotated selection
            int newWidth = (int)_toolManager.SelectionRect.Height;
            int newHeight = (int)_toolManager.SelectionRect.Width;
            
            var rotatedBitmap = new SKBitmap(newWidth, newHeight);
            
            using (var canvas = new SKCanvas(rotatedBitmap))
            {
                // Clear with transparent background
                canvas.Clear(SKColors.Transparent);
                
                // Translate to center of new bitmap
                canvas.Translate(newWidth / 2f, newHeight / 2f);
                
                // Rotate
                canvas.RotateDegrees(angleDegrees);
                
                // Translate back to draw the original bitmap centered
                canvas.Translate(-_toolManager.SelectionRect.Width / 2f, -_toolManager.SelectionRect.Height / 2f);
                
                // Draw the original selection bitmap
                canvas.DrawBitmap(_toolManager.SelectionBitmap, 0, 0);
            }
            
            // Update the selection bitmap and rectangle
            float centerX = _toolManager.SelectionRect.MidX;
            float centerY = _toolManager.SelectionRect.MidY;
            SKRect newRect = new SKRect(
                centerX - newWidth / 2f,
                centerY - newHeight / 2f,
                centerX + newWidth / 2f,
                centerY + newHeight / 2f
            );
            
            // Update the selection bitmap and rectangle in the tool manager
            _toolManager.SetSelectionBitmap(rotatedBitmap);
            _toolManager.SetSelectionRect(newRect);
            
            InvalidateVisual();
        }
        
        // Flip the selection horizontally
        private void FlipSelectionHorizontally()
        {
            if (!_toolManager.HasSelection || _toolManager.SelectionBitmap == null) return;
            
            // Create a new bitmap for the flipped selection
            var flippedBitmap = new SKBitmap(_toolManager.SelectionBitmap.Width, _toolManager.SelectionBitmap.Height);
            
            using (var canvas = new SKCanvas(flippedBitmap))
            {
                // Clear with transparent background
                canvas.Clear(SKColors.Transparent);
                
                // Scale horizontally by -1 to flip
                canvas.Scale(-1, 1);
                
                // Translate to draw the flipped bitmap
                canvas.Translate(-_toolManager.SelectionBitmap.Width, 0);
                
                // Draw the original selection bitmap
                canvas.DrawBitmap(_toolManager.SelectionBitmap, 0, 0);
            }
            
            // Update the selection bitmap in the tool manager
            _toolManager.SetSelectionBitmap(flippedBitmap);
            
            InvalidateVisual();
        }
        
        // Flip the selection vertically
        private void FlipSelectionVertically()
        {
            if (!_toolManager.HasSelection || _toolManager.SelectionBitmap == null) return;
            
            // Create a new bitmap for the flipped selection
            var flippedBitmap = new SKBitmap(_toolManager.SelectionBitmap.Width, _toolManager.SelectionBitmap.Height);
            
            using (var canvas = new SKCanvas(flippedBitmap))
            {
                // Clear with transparent background
                canvas.Clear(SKColors.Transparent);
                
                // Scale vertically by -1 to flip
                canvas.Scale(1, -1);
                
                // Translate to draw the flipped bitmap
                canvas.Translate(0, -_toolManager.SelectionBitmap.Height);
                
                // Draw the original selection bitmap
                canvas.DrawBitmap(_toolManager.SelectionBitmap, 0, 0);
            }
            
            // Update the selection bitmap in the tool manager
            _toolManager.SetSelectionBitmap(flippedBitmap);
            
            InvalidateVisual();
        }

        // Move the selection by the specified delta
        private void MoveSelection(int deltaX, int deltaY)
        {
            if (!_toolManager.HasSelection) return;
            
            // Get the current selection rectangle
            SKRect selectionRect = _toolManager.SelectionRect;
            
            // Apply the move to the selection rectangle
            selectionRect.Offset(deltaX, deltaY);
            
            // Ensure the selection stays within the bitmap bounds
            if (_bitmap != null)
            {
                if (selectionRect.Left < 0) selectionRect.Offset(-selectionRect.Left, 0);
                if (selectionRect.Top < 0) selectionRect.Offset(0, -selectionRect.Top);
                if (selectionRect.Right > _bitmap.Width) selectionRect.Offset(_bitmap.Width - selectionRect.Right, 0);
                if (selectionRect.Bottom > _bitmap.Height) selectionRect.Offset(0, _bitmap.Height - selectionRect.Bottom);
            }
            
            // Update the selection rectangle in the tool manager
            _toolManager.SetSelectionRect(selectionRect);
            
            InvalidateVisual();
        }

        // Method to set the current drawing color
        public void SetColor(Color color)
        {
            _toolManager.SetColor(new SKColor(color.R, color.G, color.B, color.A));
        }

        // Method to set the stroke width
        public void SetStrokeWidth(float width)
        {
            StrokeThickness = width;
            _toolManager.SetStrokeWidth(width);
        }

        private void InitializeBitmap(int width, int height)
        {
            _bitmap = new SKBitmap(width, height, SKColorType.Bgra8888, SKAlphaType.Premul);
            
            using (var canvas = new SKCanvas(_bitmap))
            {
                canvas.Clear(SKColors.White);
            }
        }

        // Override OnRender to draw our bitmap
        public override void Render(DrawingContext context)
        {
            base.Render(context);

            if (_bitmap == null) return;

            // Debug output
            System.Diagnostics.Debug.WriteLine($"Render called with HasSelection: {_toolManager.HasSelection}, SelectionBitmap: {_toolManager.SelectionBitmap != null}, IsMovingSelection: {_toolManager.IsMovingSelection}");

            // Create a bitmap that we can draw on for this frame
            using var renderBitmap = _bitmap.Copy();
            
            // Let the tool manager draw any overlays
            using (var canvas = new SKCanvas(renderBitmap))
            {
                // Draw the selection if we have one
                if (_toolManager.HasSelection && _toolManager.SelectionBitmap != null && !_toolManager.IsMovingSelection)
                {
                    // Debug output
                    System.Diagnostics.Debug.WriteLine($"Drawing selection bitmap at: {_toolManager.SelectionRect.Left},{_toolManager.SelectionRect.Top}");
                    
                    // Draw the selection bitmap at its position
                    canvas.DrawBitmap(_toolManager.SelectionBitmap, _toolManager.SelectionRect.Left, _toolManager.SelectionRect.Top);
                }
                
                // Let the active tool draw any overlays
                _toolManager.DrawOverlay(canvas, _bitmap);
            }
            
            // Convert the bitmap to an Avalonia bitmap
            using var data = renderBitmap.PeekPixels();
            var bitmap = new Avalonia.Media.Imaging.Bitmap(
                Avalonia.Platform.PixelFormat.Bgra8888,
                Avalonia.Platform.AlphaFormat.Premul,
                data.GetPixels(),
                new Avalonia.PixelSize(renderBitmap.Width, renderBitmap.Height),
                new Avalonia.Vector(96, 96),
                data.RowBytes);

            context.DrawImage(
                bitmap,
                new Rect(0, 0, renderBitmap.Width, renderBitmap.Height),
                new Rect(0, 0, Bounds.Width, Bounds.Height));
        }
        
        private void DrawSelectionHandle(SKCanvas canvas, float x, float y, float size, SKPaint fillPaint, SKPaint borderPaint)
        {
            float halfSize = size / 2;
            SKRect handleRect = new SKRect(x - halfSize, y - halfSize, x + halfSize, y + halfSize);
            canvas.DrawRect(handleRect, fillPaint);
            canvas.DrawRect(handleRect, borderPaint);
        }

        // Method to clear the canvas
        public void Clear()
        {
            if (_bitmap == null) return;

            using (var canvas = new SKCanvas(_bitmap))
            {
                canvas.Clear(SKColors.White);
            }
            
            ClearSelection();
            InvalidateVisual();
        }

        // Method to resize the canvas
        public void Resize(int width, int height)
        {
            if (width <= 0 || height <= 0) return;

            var newBitmap = new SKBitmap(width, height, SKColorType.Bgra8888, SKAlphaType.Premul);
            
            using (var canvas = new SKCanvas(newBitmap))
            {
                // Clear the new bitmap with white
                canvas.Clear(SKColors.White);
                
                if (_bitmap != null)
                {
                    // Draw the original bitmap without scaling
                    // This will either crop it (if new size is smaller) or
                    // add white space (if new size is larger)
                    canvas.DrawBitmap(_bitmap, 0, 0);
                }
            }

            _bitmap?.Dispose();
            _bitmap = newBitmap;
            
            // Clear selection when resizing
            ClearSelection();
            InvalidateVisual();
        }

        // Method to load an image into the canvas
        public void LoadImage(Avalonia.Media.Imaging.Bitmap bitmap)
        {
            if (bitmap == null) return;
            
            // Create a new SKBitmap with the dimensions of the loaded image
            var width = bitmap.PixelSize.Width;
            var height = bitmap.PixelSize.Height;
            
            // Resize the canvas to match the image dimensions
            Resize(width, height);
            
            // Clear the canvas
            using (var canvas = new SKCanvas(_bitmap))
            {
                canvas.Clear(SKColors.White);
            }
            
            // Draw the image onto the canvas using Avalonia's DrawingContext
            // This is a workaround since we can't directly access the bitmap's pixel data
            var renderTarget = new SKBitmap(width, height);
            using (var canvas = new SKCanvas(renderTarget))
            {
                // Draw the image onto the canvas
                canvas.Clear(SKColors.White);
                
                // Convert Avalonia bitmap to SKBitmap (simplified approach)
                // This is a placeholder - in a real app, you'd need to properly convert the bitmap
                // For now, we'll just create a white canvas with the same dimensions
            }
            
            // Copy the render target to our bitmap
            _bitmap = renderTarget;
            
            // Clear selection when loading a new image
            ClearSelection();
            
            // Redraw the canvas
            InvalidateVisual();
        }

        // Method to draw an SKBitmap onto the canvas
        public void DrawBitmap(SKBitmap bitmap)
        {
            if (bitmap == null || _bitmap == null) return;
            
            using (var canvas = new SKCanvas(_bitmap))
            {
                // Clear the canvas first
                canvas.Clear(SKColors.White);
                
                // Draw the bitmap onto the canvas
                canvas.DrawBitmap(bitmap, 0, 0);
            }
            
            // Clear selection when loading a new image
            ClearSelection();
            
            // Redraw the canvas
            InvalidateVisual();
        }

        // Method to get the current bitmap for saving
        public SKBitmap GetBitmap()
        {
            return _bitmap?.Copy() ?? new SKBitmap(1, 1);
        }
        
        // Selection methods
        
        // Select the entire canvas
        public void SelectAll()
        {
            if (_bitmap == null) return;
            
            // Create a selection rectangle that covers the entire canvas
            SKRect selectionRect = new SKRect(0, 0, _bitmap.Width, _bitmap.Height);
            
            // Set the selection in the tool manager
            _toolManager.SetTool(DrawingTool.RectangleSelect);
            _toolManager.SetSelectionRect(selectionRect);
            
            // Create a copy of the selected area
            var selectionBitmap = new SKBitmap((int)selectionRect.Width, (int)selectionRect.Height);
            using (var canvas = new SKCanvas(selectionBitmap))
            {
                canvas.DrawBitmap(_bitmap, 0, 0);
            }
            
            // Set the selection bitmap in the tool manager
            _toolManager.SetSelectionBitmap(selectionBitmap);
            _toolManager.SetHasSelection(true);
            
            InvalidateVisual();
        }
        
        // Clear the current selection
        public void ClearSelection()
        {
            _toolManager.ClearSelection();
            InvalidateVisual();
        }
        
        // Cut the current selection to clipboard
        public void Cut()
        {
            if (!_toolManager.HasSelection || _bitmap == null || _toolManager.SelectionBitmap == null) return;
            
            // Copy to clipboard first
            Copy();
            
            // Then clear the selected area
            using (var canvas = new SKCanvas(_bitmap))
            {
                var clearPaint = new SKPaint { Color = SKColors.White };
                canvas.DrawRect(_toolManager.SelectionRect, clearPaint);
            }
            
            ClearSelection();
            InvalidateVisual();
        }
        
        // Copy the current selection to clipboard
        public void Copy()
        {
            if (!_toolManager.HasSelection || _toolManager.SelectionBitmap == null) return;
            
            try
            {
                // Convert SKBitmap to Avalonia Bitmap for clipboard
                var avBitmap = new Avalonia.Media.Imaging.Bitmap(
                    Avalonia.Platform.PixelFormat.Bgra8888,
                    Avalonia.Platform.AlphaFormat.Premul,
                    _toolManager.SelectionBitmap.GetPixels(),
                    new Avalonia.PixelSize(_toolManager.SelectionBitmap.Width, _toolManager.SelectionBitmap.Height),
                    new Avalonia.Vector(96, 96),
                    _toolManager.SelectionBitmap.RowBytes);
                
                // Set to clipboard using DataObject
                var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
                if (clipboard != null)
                {
                    // Save bitmap to memory stream as PNG
                    using var ms = new MemoryStream();
                    avBitmap.Save(ms);
                    ms.Position = 0;
                    
                    // Create data object with PNG data
                    var dataObject = new DataObject();
                    dataObject.Set("image/png", ms.ToArray());
                    
                    // Set to clipboard
                    clipboard.SetDataObjectAsync(dataObject);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error copying to clipboard: {ex}");
            }
        }
        
        // Paste from clipboard
        public async Task PasteAsync()
        {
            try
            {
                if (_bitmap == null) return;
                
                // Get clipboard data
                var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
                if (clipboard == null) return;
                
                // Try to get text formats
                var formats = await clipboard.GetFormatsAsync();
                
                // Check for image data
                if (Array.IndexOf(formats, "image/png") >= 0)
                {
                    var pngData = await clipboard.GetDataAsync("image/png") as byte[];
                    if (pngData != null)
                    {
                        // Load the image from PNG data
                        using var ms = new MemoryStream(pngData);
                        var skBitmap = SKBitmap.Decode(ms);
                        
                        // Create a selection with the pasted image
                        // Position it in the center of the visible area
                        float centerX = _bitmap.Width / 2;
                        float centerY = _bitmap.Height / 2;
                        
                        // Calculate the selection rectangle
                        SKRect selectionRect = new SKRect(
                            centerX - skBitmap.Width / 2,
                            centerY - skBitmap.Height / 2,
                            centerX + skBitmap.Width / 2,
                            centerY + skBitmap.Height / 2
                        );
                        
                        // Ensure the selection is within the bitmap bounds
                        if (selectionRect.Left < 0) selectionRect.Offset(-selectionRect.Left, 0);
                        if (selectionRect.Top < 0) selectionRect.Offset(0, -selectionRect.Top);
                        if (selectionRect.Right > _bitmap.Width) selectionRect.Offset(_bitmap.Width - selectionRect.Right, 0);
                        if (selectionRect.Bottom > _bitmap.Height) selectionRect.Offset(0, _bitmap.Height - selectionRect.Bottom);
                        
                        // Set up the selection in the tool manager
                        _toolManager.SetTool(DrawingTool.RectangleSelect);
                        _toolManager.SetSelectionRect(selectionRect);
                        _toolManager.SetSelectionBitmap(skBitmap);
                        _toolManager.SetHasSelection(true);
                        
                        InvalidateVisual();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error pasting from clipboard: {ex}");
            }
        }
        
        // Commit the current selection (apply any changes)
        private void CommitSelection()
        {
            if (!_toolManager.HasSelection || _bitmap == null || _toolManager.SelectionBitmap == null) return;
            
            using (var canvas = new SKCanvas(_bitmap))
            {
                // If the selection was moved, clear the original area and draw at the new position
                if (!_toolManager.SelectionOffset.Equals(new SKPoint(0, 0)))
                {
                    // Clear the original selection area with white
                    var clearPaint = new SKPaint { Color = SKColors.White };
                    canvas.DrawRect(_toolManager.SelectionRect, clearPaint);
                    
                    // Draw the selection at its new position
                    var destRect = _toolManager.SelectionRect;
                    destRect.Offset(_toolManager.SelectionOffset.X, _toolManager.SelectionOffset.Y);
                    canvas.DrawBitmap(_toolManager.SelectionBitmap, destRect.Left, destRect.Top);
                }
                else
                {
                    // If the selection was resized, just draw it at its current position
                    canvas.DrawBitmap(_toolManager.SelectionBitmap, _toolManager.SelectionRect.Left, _toolManager.SelectionRect.Top);
                }
            }
            
            ClearSelection();
        }

        // Clean up resources when control is detached from visual tree
        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            // Unsubscribe from events
            this.PointerPressed -= OnPointerPressed;
            this.PointerMoved -= OnPointerMoved;
            this.PointerReleased -= OnPointerReleased;

            base.OnDetachedFromVisualTree(e);
            _bitmap?.Dispose();
            _bitmap = null;
            _tempBitmap?.Dispose();
            _tempBitmap = null;
        }

        // Method to set the current drawing tool
        public void SetTool(DrawingTool tool)
        {
            _toolManager.SetTool(tool);
            InvalidateVisual();
        }

        // Method to get the current drawing tool
        public DrawingTool CurrentTool => _toolManager.CurrentTool;

        // Method to set whether shapes should be filled
        public void SetFillShapes(bool fill)
        {
            _toolManager.SetFillShapes(fill);
        }

        // Method to get whether shapes are filled
        public bool FillShapes => _toolManager.FillShapes;

        // Method to ensure the selection is properly rendered
        private void EnsureSelectionVisible()
        {
            if (_toolManager.HasSelection)
            {
                System.Diagnostics.Debug.WriteLine("Selection is active, ensuring it's visible");
                
                // Force a redraw
                InvalidateVisual();
                
                // Schedule another redraw after a short delay to ensure the selection is visible
                // This is a workaround for some rendering issues
                Task.Delay(50).ContinueWith(_ => 
                {
                    Dispatcher.UIThread.Post(() => 
                    {
                        System.Diagnostics.Debug.WriteLine("Delayed redraw for selection");
                        InvalidateVisual();
                    });
                });
            }
        }

        // Method to create a test selection
        public void CreateTestSelection()
        {
            if (_bitmap == null) return;
            
            // Create a selection rectangle in the center of the canvas
            float centerX = _bitmap.Width / 2;
            float centerY = _bitmap.Height / 2;
            int width = 300;
            int height = 200;
            
            SKRect selectionRect = new SKRect(
                centerX - width / 2,
                centerY - height / 2,
                centerX + width / 2,
                centerY + height / 2
            );
            
            // Create a selection bitmap
            var selectionBitmap = new SKBitmap(width, height);
            
            using (var canvas = new SKCanvas(selectionBitmap))
            {
                // Clear with a semi-transparent red color
                canvas.Clear(new SKColor(255, 0, 0, 128));
                
                // Draw a pattern to make it visible
                var paint = new SKPaint
                {
                    Style = SKPaintStyle.Stroke,
                    Color = SKColors.White,
                    StrokeWidth = 3
                };
                
                for (int i = 0; i < width; i += 20)
                {
                    canvas.DrawLine(i, 0, i, height, paint);
                }
                
                for (int i = 0; i < height; i += 20)
                {
                    canvas.DrawLine(0, i, width, i, paint);
                }
                
                // Draw a diagonal cross
                canvas.DrawLine(0, 0, width, height, paint);
                canvas.DrawLine(width, 0, 0, height, paint);
            }
            
            // Set the tool to rectangle select
            _toolManager.SetTool(DrawingTool.RectangleSelect);
            
            // Set up the selection in the tool manager
            _toolManager.SetSelectionRect(selectionRect);
            _toolManager.SetSelectionBitmap(selectionBitmap);
            _toolManager.SetHasSelection(true);
            
            // Debug output
            System.Diagnostics.Debug.WriteLine("Created test selection with HasSelection = " + _toolManager.HasSelection);
            
            // Ensure the selection is visible
            EnsureSelectionVisible();
            
            // Force multiple redraws to ensure the selection is visible
            for (int i = 0; i < 5; i++)
            {
                Task.Delay(100 * i).ContinueWith(_ => 
                {
                    Dispatcher.UIThread.Post(() => 
                    {
                        System.Diagnostics.Debug.WriteLine($"Forced redraw {i} for test selection");
                        InvalidateVisual();
                    });
                });
            }
        }
    }
} 