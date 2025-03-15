using Avalonia.Input;
using SkiaSharp;

namespace MSPaintEx.Tools
{
    public class LineTool : ToolBase
    {
        private SKBitmap? _tempBitmap;
        
        public LineTool(ToolManager toolManager) : base(toolManager)
        {
        }
        
        public override void OnPointerPressed(SKPoint point, SKBitmap bitmap)
        {
            base.OnPointerPressed(point, bitmap);
            
            if (bitmap == null) return;
            
            // Store the start point
            _toolManager.SetShapeStartPoint(point);
            
            // Create a temporary bitmap for preview
            _tempBitmap = bitmap.Copy();
        }
        
        public override void OnPointerMoved(SKPoint point, SKPoint? lastPoint, SKBitmap bitmap)
        {
            base.OnPointerMoved(point, lastPoint, bitmap);
            
            if (!_isDrawing || bitmap == null || _tempBitmap == null || !_toolManager.ShapeStartPoint.HasValue) return;
            
            // Create a copy of the temp bitmap to draw on
            bitmap.Erase(SKColors.White);
            using (var canvas = new SKCanvas(bitmap))
            {
                // Draw the original bitmap
                canvas.DrawBitmap(_tempBitmap, 0, 0);
                
                // Draw the line
                canvas.DrawLine(_toolManager.ShapeStartPoint.Value, point, _toolManager.GetPaint());
            }
        }
        
        public override void OnPointerReleased(SKPoint point, SKBitmap bitmap)
        {
            if (_isDrawing && bitmap != null && _toolManager.ShapeStartPoint.HasValue)
            {
                // Draw the final line
                using (var canvas = new SKCanvas(bitmap))
                {
                    canvas.DrawLine(_toolManager.ShapeStartPoint.Value, point, _toolManager.GetPaint());
                }
            }
            
            // Clean up
            _tempBitmap?.Dispose();
            _tempBitmap = null;
            _toolManager.SetShapeStartPoint(null);
            
            base.OnPointerReleased(point, bitmap);
        }
        
        public override StandardCursorType GetCursor(SKPoint point)
        {
            return StandardCursorType.Cross;
        }
    }
} 