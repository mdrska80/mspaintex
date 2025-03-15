using Avalonia.Input;
using SkiaSharp;

namespace MSPaintEx.Tools
{
    public class PencilTool : ToolBase
    {
        public PencilTool(ToolManager toolManager) : base(toolManager)
        {
        }
        
        public override void OnPointerPressed(SKPoint point, SKBitmap bitmap)
        {
            base.OnPointerPressed(point, bitmap);
            
            // Draw a single point
            using (var canvas = new SKCanvas(bitmap))
            {
                canvas.DrawPoint(point, _toolManager.GetPaint());
            }
        }
        
        public override void OnPointerMoved(SKPoint point, SKPoint? lastPoint, SKBitmap bitmap)
        {
            base.OnPointerMoved(point, lastPoint, bitmap);
            
            // Only draw if we're in drawing mode (mouse button is pressed)
            if (!_isDrawing || !lastPoint.HasValue) return;
            
            using (var canvas = new SKCanvas(bitmap))
            {
                canvas.DrawLine(lastPoint.Value, point, _toolManager.GetPaint());
            }
        }
        
        public override StandardCursorType GetCursor(SKPoint point)
        {
            return StandardCursorType.Cross;
        }
    }
} 