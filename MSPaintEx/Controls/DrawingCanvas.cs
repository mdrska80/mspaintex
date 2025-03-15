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
            
            // Convert point to bitmap coordinates
            var x = (float)(point.X * _bitmap.Width / Bounds.Width);
            var y = (float)(point.Y * _bitmap.Height / Bounds.Height);
            
            _lastPoint = new SKPoint(x, y);
            _isDrawing = true;
        }

        // Handle pointer (mouse) moved event
        private void OnPointerMoved(object? sender, PointerEventArgs e)
        {
            if (!_isDrawing || _lastPoint == null || _bitmap == null) return;

            var point = e.GetPosition(this);
            
            // Convert point to bitmap coordinates
            var x = (float)(point.X * _bitmap.Width / Bounds.Width);
            var y = (float)(point.Y * _bitmap.Height / Bounds.Height);
            var currentPoint = new SKPoint(x, y);

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
            _isDrawing = false;
            _lastPoint = null;
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
        }

        // Method to clear the canvas
        public void Clear()
        {
            if (_bitmap == null) return;

            using (var canvas = new SKCanvas(_bitmap))
            {
                canvas.Clear(SKColors.White);
            }

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
            
            // Redraw the canvas
            InvalidateVisual();
        }

        // Method to get the current bitmap for saving
        public SKBitmap GetBitmap()
        {
            return _bitmap?.Copy() ?? new SKBitmap(1, 1);
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
        }
    }
} 