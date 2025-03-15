using Avalonia.Input;
using SkiaSharp;
using System;

namespace MSPaintEx.Tools
{
    public class RoundedRectangleTool : ToolBase
    {
        private SKBitmap? _tempBitmap;
        private const float CORNER_RADIUS = 10.0f;
        
        public RoundedRectangleTool(ToolManager toolManager) : base(toolManager)
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
                
                // Draw the rounded rectangle
                var rect = new SKRect(
                    _toolManager.ShapeStartPoint.Value.X,
                    _toolManager.ShapeStartPoint.Value.Y,
                    point.X,
                    point.Y
                );
                canvas.DrawRoundRect(rect, CORNER_RADIUS, CORNER_RADIUS, shapePaint);
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
                
                // Draw the final rounded rectangle
                using (var canvas = new SKCanvas(bitmap))
                {
                    var rect = new SKRect(
                        _toolManager.ShapeStartPoint.Value.X,
                        _toolManager.ShapeStartPoint.Value.Y,
                        point.X,
                        point.Y
                    );
                    canvas.DrawRoundRect(rect, CORNER_RADIUS, CORNER_RADIUS, shapePaint);
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