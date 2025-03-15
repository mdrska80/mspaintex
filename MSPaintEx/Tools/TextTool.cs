using Avalonia.Input;
using SkiaSharp;

namespace MSPaintEx.Tools
{
    public class TextTool : ToolBase
    {
        public TextTool(ToolManager toolManager) : base(toolManager)
        {
        }
        
        public override void OnPointerPressed(SKPoint point, SKBitmap bitmap)
        {
            base.OnPointerPressed(point, bitmap);
            
            if (bitmap == null) return;
            
            // For now, just draw "Text" at the clicked position
            // In a real implementation, you would show a text input dialog
            using (var canvas = new SKCanvas(bitmap))
            {
                var textPaint = new SKPaint
                {
                    Color = _toolManager.CurrentColor,
                    TextSize = 20,
                    IsAntialias = true
                };
                
                canvas.DrawText("Text", point.X, point.Y, textPaint);
            }
        }
        
        public override StandardCursorType GetCursor(SKPoint point)
        {
            return StandardCursorType.Ibeam;
        }
    }
} 