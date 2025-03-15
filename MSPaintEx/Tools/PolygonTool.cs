using Avalonia.Input;
using SkiaSharp;
using System;

namespace MSPaintEx.Tools
{
    public class PolygonTool : ToolBase
    {
        private SKBitmap? _tempBitmap;
        
        public PolygonTool(ToolManager toolManager) : base(toolManager)
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
                
                // Create paint for shape
                var shapePaint = new SKPaint
                {
                    Color = _toolManager.CurrentColor,
                    StrokeWidth = _toolManager.StrokeWidth,
                    IsAntialias = true
                };
                
                // Set style based on fill option
                shapePaint.Style = _toolManager.FillShapes ? SKPaintStyle.Fill : SKPaintStyle.Stroke;
                
                // Draw a simple triangle for now
                // In a real implementation, you would allow the user to add multiple points
                var path = new SKPath();
                path.MoveTo(_toolManager.ShapeStartPoint.Value);
                path.LineTo(point.X, _toolManager.ShapeStartPoint.Value.Y);
                path.LineTo(point.X, point.Y);
                path.Close();
                
                canvas.DrawPath(path, shapePaint);
            }
        }
        
        public override void OnPointerReleased(SKPoint point, SKBitmap bitmap)
        {
            if (_isDrawing && bitmap != null && _toolManager.ShapeStartPoint.HasValue)
            {
                // Create paint for shape
                var shapePaint = new SKPaint
                {
                    Color = _toolManager.CurrentColor,
                    StrokeWidth = _toolManager.StrokeWidth,
                    IsAntialias = true
                };
                
                // Set style based on fill option
                shapePaint.Style = _toolManager.FillShapes ? SKPaintStyle.Fill : SKPaintStyle.Stroke;
                
                // Draw the final polygon (triangle for now)
                using (var canvas = new SKCanvas(bitmap))
                {
                    var path = new SKPath();
                    path.MoveTo(_toolManager.ShapeStartPoint.Value);
                    path.LineTo(point.X, _toolManager.ShapeStartPoint.Value.Y);
                    path.LineTo(point.X, point.Y);
                    path.Close();
                    
                    canvas.DrawPath(path, shapePaint);
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