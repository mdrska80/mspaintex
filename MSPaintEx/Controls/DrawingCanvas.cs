using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Platform;
using SkiaSharp;
using System;
using Avalonia.VisualTree;
using Avalonia.Controls.Primitives;
using Avalonia.Input;  // Add this for mouse handling

namespace MSPaintEx.Controls
{
    // DrawingCanvas: Custom control that provides basic drawing functionality
    public class DrawingCanvas : TemplatedControl  // Change to TemplatedControl which has Background property
    {
        // The bitmap that represents our drawing surface
        private SKBitmap? _bitmap;
        
        // Drawing settings
        private SKColor _currentColor = SKColors.Black;  // Default color
        private float _strokeWidth = 2f;                 // Default stroke width
        
        // The canvas size properties
        private const int DEFAULT_WIDTH = 800;
        private const int DEFAULT_HEIGHT = 600;

        private SKPoint? _lastPoint;  // Track last point for drawing
        private bool _isDrawing;      // Track if we're currently drawing

        public DrawingCanvas()
        {
            InitializeBitmap(DEFAULT_WIDTH, DEFAULT_HEIGHT);
            Background = Brushes.White;
            
            this.PointerPressed += OnPointerPressed;
            this.PointerMoved += OnPointerMoved;
            this.PointerReleased += OnPointerReleased;
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
                using (var paint = new SKPaint
                {
                    Color = _currentColor,
                    StrokeWidth = _strokeWidth,
                    StrokeCap = SKStrokeCap.Round,
                    StrokeJoin = SKStrokeJoin.Round,
                    Style = SKPaintStyle.Stroke,
                    IsAntialias = true  // Add antialiasing for smoother lines
                })
                {
                    canvas.DrawLine(_lastPoint.Value, currentPoint, paint);
                }
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
            _currentColor = new SKColor(color.R, color.G, color.B, color.A);
        }

        // Method to set the stroke width
        public void SetStrokeWidth(float width)
        {
            if (width > 0)
            {
                _strokeWidth = width;
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
                canvas.Clear(SKColors.White);
                
                if (_bitmap != null)
                {
                    // Scale the old bitmap to fit the new size
                    var scaleX = (float)width / _bitmap.Width;
                    var scaleY = (float)height / _bitmap.Height;
                    canvas.Scale(scaleX, scaleY);
                    canvas.DrawBitmap(_bitmap, 0, 0);
                }
            }

            _bitmap?.Dispose();
            _bitmap = newBitmap;
            
            InvalidateVisual();
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