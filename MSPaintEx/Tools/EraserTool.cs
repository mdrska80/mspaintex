using Avalonia.Input;
using SkiaSharp;

namespace MSPaintEx.Tools
{
    public class EraserTool : ToolBase
    {
        public EraserTool(ToolManager toolManager) : base(toolManager)
        {
        }
        
        public override void OnPointerPressed(SKPoint point, SKBitmap bitmap)
        {
            base.OnPointerPressed(point, bitmap);
            
            // Draw a white circle for the eraser
            using (var canvas = new SKCanvas(bitmap))
            {
                var eraserPaint = new SKPaint
                {
                    Style = SKPaintStyle.Fill,
                    Color = SKColors.White,
                    IsAntialias = true
                };
                
                canvas.DrawCircle(point, _toolManager.StrokeWidth / 2, eraserPaint);
            }
        }
        
        public override void OnPointerMoved(SKPoint point, SKPoint? lastPoint, SKBitmap bitmap)
        {
            base.OnPointerMoved(point, lastPoint, bitmap);
            
            // Only draw if we're in drawing mode (mouse button is pressed)
            if (!_isDrawing || !lastPoint.HasValue) return;
            
            using (var canvas = new SKCanvas(bitmap))
            {
                // Use white color for eraser
                var eraserPaint = new SKPaint
                {
                    Style = SKPaintStyle.Stroke,
                    Color = SKColors.White,
                    StrokeWidth = _toolManager.StrokeWidth,
                    StrokeJoin = SKStrokeJoin.Round,
                    StrokeCap = SKStrokeCap.Round,
                    IsAntialias = true
                };
                
                canvas.DrawLine(lastPoint.Value, point, eraserPaint);
            }
        }
        
        public override StandardCursorType GetCursor(SKPoint point)
        {
            return StandardCursorType.Cross;
        }
    }
} 