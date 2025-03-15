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
        private bool _hasSelection;
        private SKRect _selectionRect;
        private SKBitmap? _selectionBitmap;
        private SKPaint _selectionPaint;
        private bool _isMovingSelection;
        private bool _isResizingSelection;
        private SKPoint _selectionMoveStart;
        private SKPoint _selectionOffset;
        private ResizeHandle _activeResizeHandle = ResizeHandle.None;
        private SKRect _originalSelectionRect;

        // Resize handle size
        private const float HANDLE_SIZE = 8;
        
        // Enum to track which resize handle is active
        private enum ResizeHandle
        {
            None,
            TopLeft,
            TopCenter,
            TopRight,
            MiddleLeft,
            MiddleRight,
            BottomLeft,
            BottomCenter,
            BottomRight
        }

        // The canvas size properties
        private const int DEFAULT_WIDTH = 800;
        private const int DEFAULT_HEIGHT = 600;

        public DrawingCanvas()
        {
            InitializeBitmap(DEFAULT_WIDTH, DEFAULT_HEIGHT);
            Background = Brushes.White;
            
            this.PointerPressed += OnPointerPressed;
            this.PointerMoved += OnPointerMoved;
            this.PointerReleased += OnPointerReleased;

            _paint = new SKPaint
            {
                Style = SKPaintStyle.Stroke,
                Color = SKColors.Black,
                StrokeWidth = (float)StrokeThickness,
                StrokeJoin = SKStrokeJoin.Round,
                StrokeCap = SKStrokeCap.Round,
                // Disable antialiasing for pixel-perfect drawing
                IsAntialias = false
            };
            
            // Initialize selection paint
            _selectionPaint = new SKPaint
            {
                Style = SKPaintStyle.Stroke,
                Color = SKColors.Black,
                StrokeWidth = 1,
                PathEffect = SKPathEffect.CreateDash(new float[] { 5, 5 }, 0)
            };
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

        // Check if a point is near a resize handle
        private ResizeHandle GetResizeHandleAtPoint(float x, float y)
        {
            if (!_hasSelection) return ResizeHandle.None;
            
            float halfSize = HANDLE_SIZE / 2;
            
            // Check corners first (they take precedence)
            if (Math.Abs(x - _selectionRect.Left) <= halfSize && Math.Abs(y - _selectionRect.Top) <= halfSize)
                return ResizeHandle.TopLeft;
                
            if (Math.Abs(x - _selectionRect.Right) <= halfSize && Math.Abs(y - _selectionRect.Top) <= halfSize)
                return ResizeHandle.TopRight;
                
            if (Math.Abs(x - _selectionRect.Left) <= halfSize && Math.Abs(y - _selectionRect.Bottom) <= halfSize)
                return ResizeHandle.BottomLeft;
                
            if (Math.Abs(x - _selectionRect.Right) <= halfSize && Math.Abs(y - _selectionRect.Bottom) <= halfSize)
                return ResizeHandle.BottomRight;
            
            // Then check edges
            if (Math.Abs(x - (_selectionRect.Left + _selectionRect.Width / 2)) <= halfSize && Math.Abs(y - _selectionRect.Top) <= halfSize)
                return ResizeHandle.TopCenter;
                
            if (Math.Abs(x - (_selectionRect.Left + _selectionRect.Width / 2)) <= halfSize && Math.Abs(y - _selectionRect.Bottom) <= halfSize)
                return ResizeHandle.BottomCenter;
                
            if (Math.Abs(x - _selectionRect.Left) <= halfSize && Math.Abs(y - (_selectionRect.Top + _selectionRect.Height / 2)) <= halfSize)
                return ResizeHandle.MiddleLeft;
                
            if (Math.Abs(x - _selectionRect.Right) <= halfSize && Math.Abs(y - (_selectionRect.Top + _selectionRect.Height / 2)) <= halfSize)
                return ResizeHandle.MiddleRight;
            
            return ResizeHandle.None;
        }

        // Handle pointer (mouse) pressed event
        private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (_bitmap == null) return;

            var point = e.GetPosition(this);
            
            // Convert point to bitmap coordinates
            var x = (float)(point.X * _bitmap.Width / Bounds.Width);
            var y = (float)(point.Y * _bitmap.Height / Bounds.Height);
            
            // Check if we're clicking on a resize handle
            var handle = GetResizeHandleAtPoint(x, y);
            if (handle != ResizeHandle.None)
            {
                _activeResizeHandle = handle;
                _isResizingSelection = true;
                _originalSelectionRect = _selectionRect;
                _selectionMoveStart = new SKPoint(x, y);
                return;
            }
            
            // Check if we're clicking inside the selection
            if (_hasSelection && _selectionRect.Contains(x, y))
            {
                _isMovingSelection = true;
                _selectionMoveStart = new SKPoint(x, y);
                return;
            }
            
            // If we have a selection and clicked outside, commit the selection
            if (_hasSelection)
            {
                CommitSelection();
            }
            
            _lastPoint = new SKPoint(x, y);
            _isDrawing = true;
        }

        // Handle pointer (mouse) moved event
        private void OnPointerMoved(object? sender, PointerEventArgs e)
        {
            if (_bitmap == null) return;
            
            var point = e.GetPosition(this);
            
            // Convert point to bitmap coordinates
            var x = (float)(point.X * _bitmap.Width / Bounds.Width);
            var y = (float)(point.Y * _bitmap.Height / Bounds.Height);
            var currentPoint = new SKPoint(x, y);
            
            // Update cursor based on position
            if (!_isMovingSelection && !_isResizingSelection && _hasSelection)
            {
                var handle = GetResizeHandleAtPoint(x, y);
                switch (handle)
                {
                    case ResizeHandle.TopLeft:
                    case ResizeHandle.BottomRight:
                        Cursor = new Cursor(StandardCursorType.TopLeftCorner);
                        break;
                    case ResizeHandle.TopRight:
                    case ResizeHandle.BottomLeft:
                        Cursor = new Cursor(StandardCursorType.TopRightCorner);
                        break;
                    case ResizeHandle.TopCenter:
                    case ResizeHandle.BottomCenter:
                        Cursor = new Cursor(StandardCursorType.SizeNorthSouth);
                        break;
                    case ResizeHandle.MiddleLeft:
                    case ResizeHandle.MiddleRight:
                        Cursor = new Cursor(StandardCursorType.SizeWestEast);
                        break;
                    default:
                        if (_selectionRect.Contains(x, y))
                            Cursor = new Cursor(StandardCursorType.SizeAll);
                        else
                            Cursor = new Cursor(StandardCursorType.Arrow);
                        break;
                }
            }
            
            // Handle resizing selection
            if (_isResizingSelection && _hasSelection)
            {
                SKRect newRect = _originalSelectionRect;
                
                // Calculate the delta from the start position
                float deltaX = x - _selectionMoveStart.X;
                float deltaY = y - _selectionMoveStart.Y;
                
                // Apply the resize based on which handle is active
                switch (_activeResizeHandle)
                {
                    case ResizeHandle.TopLeft:
                        newRect.Left = _originalSelectionRect.Left + deltaX;
                        newRect.Top = _originalSelectionRect.Top + deltaY;
                        break;
                    case ResizeHandle.TopCenter:
                        newRect.Top = _originalSelectionRect.Top + deltaY;
                        break;
                    case ResizeHandle.TopRight:
                        newRect.Right = _originalSelectionRect.Right + deltaX;
                        newRect.Top = _originalSelectionRect.Top + deltaY;
                        break;
                    case ResizeHandle.MiddleLeft:
                        newRect.Left = _originalSelectionRect.Left + deltaX;
                        break;
                    case ResizeHandle.MiddleRight:
                        newRect.Right = _originalSelectionRect.Right + deltaX;
                        break;
                    case ResizeHandle.BottomLeft:
                        newRect.Left = _originalSelectionRect.Left + deltaX;
                        newRect.Bottom = _originalSelectionRect.Bottom + deltaY;
                        break;
                    case ResizeHandle.BottomCenter:
                        newRect.Bottom = _originalSelectionRect.Bottom + deltaY;
                        break;
                    case ResizeHandle.BottomRight:
                        newRect.Right = _originalSelectionRect.Right + deltaX;
                        newRect.Bottom = _originalSelectionRect.Bottom + deltaY;
                        break;
                }
                
                // Ensure the rectangle has positive width and height
                if (newRect.Width > 0 && newRect.Height > 0)
                {
                    _selectionRect = newRect;
                    InvalidateVisual();
                }
                
                return;
            }
            
            // Handle moving selection
            if (_isMovingSelection && _hasSelection)
            {
                // Calculate the offset from the start position
                _selectionOffset.X = x - _selectionMoveStart.X;
                _selectionOffset.Y = y - _selectionMoveStart.Y;
                
                // Redraw to show the selection in its new position
                InvalidateVisual();
                return;
            }

            // Handle drawing
            if (!_isDrawing || _lastPoint == null) return;

            using (var canvas = new SKCanvas(_bitmap))
            {
                canvas.DrawLine(_lastPoint.Value, currentPoint, _paint);
            }

            _lastPoint = currentPoint;
            InvalidateVisual();
        }

        // Handle pointer (mouse) released event
        private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            if (_isResizingSelection)
            {
                // When resizing is complete, update the selection bitmap
                UpdateSelectionBitmap();
                _isResizingSelection = false;
                _activeResizeHandle = ResizeHandle.None;
                InvalidateVisual();
                return;
            }
            
            if (_isMovingSelection)
            {
                // Apply the move to the selection rectangle
                _selectionRect.Offset(_selectionOffset.X, _selectionOffset.Y);
                _selectionOffset = new SKPoint(0, 0);
                _isMovingSelection = false;
                InvalidateVisual();
                return;
            }
            
            _isDrawing = false;
            _lastPoint = null;
        }
        
        // Update the selection bitmap after resizing
        private void UpdateSelectionBitmap()
        {
            if (!_hasSelection || _bitmap == null) return;
            
            // Create a new bitmap for the resized selection
            var newSelectionBitmap = new SKBitmap((int)_selectionRect.Width, (int)_selectionRect.Height);
            
            using (var canvas = new SKCanvas(newSelectionBitmap))
            {
                // Clear with white background
                canvas.Clear(SKColors.White);
                
                // Draw the portion of the main bitmap that corresponds to the selection area
                var srcRect = new SKRect(
                    _selectionRect.Left, 
                    _selectionRect.Top, 
                    _selectionRect.Right, 
                    _selectionRect.Bottom);
                
                canvas.DrawBitmap(_bitmap, srcRect, new SKRect(0, 0, _selectionRect.Width, _selectionRect.Height));
            }
            
            // Dispose the old selection bitmap and set the new one
            _selectionBitmap?.Dispose();
            _selectionBitmap = newSelectionBitmap;
        }

        // Method to set the current drawing color
        public void SetColor(Color color)
        {
            if (_paint != null)
            {
                _paint.Color = new SKColor(color.R, color.G, color.B, color.A);
            }
        }

        // Method to set the stroke width
        public void SetStrokeWidth(float width)
        {
            StrokeThickness = width;
            if (_paint != null)
            {
                _paint.StrokeWidth = width;
            }
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

            var bitmap = new Avalonia.Media.Imaging.Bitmap(
                Avalonia.Platform.PixelFormat.Bgra8888,
                Avalonia.Platform.AlphaFormat.Premul,
                _bitmap.GetPixels(),
                new Avalonia.PixelSize(_bitmap.Width, _bitmap.Height),
                new Avalonia.Vector(96, 96),
                _bitmap.RowBytes);

            context.DrawImage(
                bitmap,
                new Rect(0, 0, _bitmap.Width, _bitmap.Height),
                new Rect(0, 0, Bounds.Width, Bounds.Height));
                
            // Draw selection if active
            if (_hasSelection)
            {
                // Create a temporary bitmap to draw the selection overlay
                using var tempBitmap = new SKBitmap(_bitmap.Width, _bitmap.Height);
                using var canvas = new SKCanvas(tempBitmap);
                
                // Draw the selection rectangle with dashed border
                var rect = _selectionRect;
                if (_isMovingSelection)
                {
                    rect.Offset(_selectionOffset.X, _selectionOffset.Y);
                }
                
                canvas.DrawRect(rect, _selectionPaint);
                
                // Draw the selection handles (small squares at the corners and edges)
                float handleSize = HANDLE_SIZE;
                SKPaint handlePaint = new SKPaint { Color = SKColors.White, Style = SKPaintStyle.Fill };
                SKPaint handleBorderPaint = new SKPaint { Color = SKColors.Black, Style = SKPaintStyle.Stroke, StrokeWidth = 1 };
                
                // Draw handles at each corner and edge
                // Corners
                DrawSelectionHandle(canvas, rect.Left, rect.Top, handleSize, handlePaint, handleBorderPaint);
                DrawSelectionHandle(canvas, rect.Right, rect.Top, handleSize, handlePaint, handleBorderPaint);
                DrawSelectionHandle(canvas, rect.Left, rect.Bottom, handleSize, handlePaint, handleBorderPaint);
                DrawSelectionHandle(canvas, rect.Right, rect.Bottom, handleSize, handlePaint, handleBorderPaint);
                
                // Edges
                DrawSelectionHandle(canvas, rect.Left + rect.Width / 2, rect.Top, handleSize, handlePaint, handleBorderPaint);
                DrawSelectionHandle(canvas, rect.Left + rect.Width / 2, rect.Bottom, handleSize, handlePaint, handleBorderPaint);
                DrawSelectionHandle(canvas, rect.Left, rect.Top + rect.Height / 2, handleSize, handlePaint, handleBorderPaint);
                DrawSelectionHandle(canvas, rect.Right, rect.Top + rect.Height / 2, handleSize, handlePaint, handleBorderPaint);
                
                // Convert to Avalonia bitmap and draw
                var selectionOverlay = new Avalonia.Media.Imaging.Bitmap(
                    Avalonia.Platform.PixelFormat.Bgra8888,
                    Avalonia.Platform.AlphaFormat.Premul,
                    tempBitmap.GetPixels(),
                    new Avalonia.PixelSize(tempBitmap.Width, tempBitmap.Height),
                    new Avalonia.Vector(96, 96),
                    tempBitmap.RowBytes);
                
                context.DrawImage(
                    selectionOverlay,
                    new Rect(0, 0, tempBitmap.Width, tempBitmap.Height),
                    new Rect(0, 0, Bounds.Width, Bounds.Height));
            }
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
            
            _selectionRect = new SKRect(0, 0, _bitmap.Width, _bitmap.Height);
            _hasSelection = true;
            
            // Create a copy of the selected area
            _selectionBitmap = new SKBitmap((int)_selectionRect.Width, (int)_selectionRect.Height);
            using (var canvas = new SKCanvas(_selectionBitmap))
            {
                canvas.DrawBitmap(_bitmap, -_selectionRect.Left, -_selectionRect.Top);
            }
            
            InvalidateVisual();
        }
        
        // Clear the current selection
        public void ClearSelection()
        {
            _hasSelection = false;
            _selectionBitmap?.Dispose();
            _selectionBitmap = null;
            _isMovingSelection = false;
            _isResizingSelection = false;
            _activeResizeHandle = ResizeHandle.None;
            Cursor = new Cursor(StandardCursorType.Arrow);
            InvalidateVisual();
        }
        
        // Cut the current selection to clipboard
        public void Cut()
        {
            if (!_hasSelection || _bitmap == null || _selectionBitmap == null) return;
            
            // Copy to clipboard first
            Copy();
            
            // Then clear the selected area
            using (var canvas = new SKCanvas(_bitmap))
            {
                var clearPaint = new SKPaint { Color = SKColors.White };
                canvas.DrawRect(_selectionRect, clearPaint);
            }
            
            ClearSelection();
            InvalidateVisual();
        }
        
        // Copy the current selection to clipboard
        public void Copy()
        {
            if (!_hasSelection || _selectionBitmap == null) return;
            
            try
            {
                // Convert SKBitmap to Avalonia Bitmap for clipboard
                var avBitmap = new Avalonia.Media.Imaging.Bitmap(
                    Avalonia.Platform.PixelFormat.Bgra8888,
                    Avalonia.Platform.AlphaFormat.Premul,
                    _selectionBitmap.GetPixels(),
                    new Avalonia.PixelSize(_selectionBitmap.Width, _selectionBitmap.Height),
                    new Avalonia.Vector(96, 96),
                    _selectionBitmap.RowBytes);
                
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
                        using var ms = new MemoryStream(pngData);
                        var skBitmap = SKBitmap.Decode(ms);
                        
                        if (skBitmap != null)
                        {
                            // Create selection from the pasted image
                            int width = skBitmap.Width;
                            int height = skBitmap.Height;
                            
                            // Create selection rectangle centered in the view
                            float left = Math.Max(0, (_bitmap.Width - width) / 2);
                            float top = Math.Max(0, (_bitmap.Height - height) / 2);
                            _selectionRect = new SKRect(left, top, left + width, top + height);
                            
                            // Create selection bitmap
                            _selectionBitmap?.Dispose();
                            _selectionBitmap = skBitmap;
                            
                            _hasSelection = true;
                            _isMovingSelection = true;
                            _selectionMoveStart = new SKPoint(_selectionRect.MidX, _selectionRect.MidY);
                            _selectionOffset = new SKPoint(0, 0);
                            
                            InvalidateVisual();
                        }
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
            if (!_hasSelection || _bitmap == null || _selectionBitmap == null) return;
            
            using (var canvas = new SKCanvas(_bitmap))
            {
                // If the selection was moved, clear the original area and draw at the new position
                if (!_selectionOffset.Equals(new SKPoint(0, 0)))
                {
                    // Clear the original selection area with white
                    var clearPaint = new SKPaint { Color = SKColors.White };
                    canvas.DrawRect(_selectionRect, clearPaint);
                    
                    // Draw the selection at its new position
                    var destRect = _selectionRect;
                    destRect.Offset(_selectionOffset.X, _selectionOffset.Y);
                    canvas.DrawBitmap(_selectionBitmap, destRect.Left, destRect.Top);
                }
                else
                {
                    // If the selection was resized, just draw it at its current position
                    canvas.DrawBitmap(_selectionBitmap, _selectionRect.Left, _selectionRect.Top);
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
            _selectionBitmap?.Dispose();
            _selectionBitmap = null;
        }
    }
} 