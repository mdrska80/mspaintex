using Avalonia.Input;
using SkiaSharp;

namespace MSPaintEx.Tools
{
    public class BrushTool : ToolBase
    {
        public BrushTool(ToolManager toolManager) : base(toolManager)
        {
        }
        
        public override void OnPointerPressed(SKPoint point, SKBitmap bitmap)
        {
            base.OnPointerPressed(point, bitmap);
            
            // Draw a circle for the brush
            using (var canvas = new SKCanvas(bitmap))
            {
                canvas.DrawCircle(point, _toolManager.StrokeWidth / 2, _toolManager.GetPaint());
            }
        }
        
        public override void OnPointerMoved(SKPoint point, SKPoint? lastPoint, SKBitmap bitmap)
        {
            base.OnPointerMoved(point, lastPoint, bitmap);
            
            // Only draw if we're in drawing mode (mouse button is pressed)
            if (!_isDrawing || !lastPoint.HasValue) return;
            
            using (var canvas = new SKCanvas(bitmap))
            {
                // Draw a line with rounded caps for smoother brush effect
                var brushPaint = new SKPaint
                {
                    Style = SKPaintStyle.Stroke,
                    Color = _toolManager.CurrentColor,
                    StrokeWidth = _toolManager.StrokeWidth,
                    StrokeJoin = SKStrokeJoin.Round,
                    StrokeCap = SKStrokeCap.Round,
                    IsAntialias = true
                };
                
                canvas.DrawLine(lastPoint.Value, point, brushPaint);
            }
        }
        
        public override StandardCursorType GetCursor(SKPoint point)
        {
            return StandardCursorType.Cross;
        }
    }
} 