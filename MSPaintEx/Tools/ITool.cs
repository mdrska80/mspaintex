using Avalonia.Input;
using SkiaSharp;

namespace MSPaintEx.Tools
{
    public interface ITool
    {
        // Handle pointer pressed event
        void OnPointerPressed(SKPoint point, SKBitmap bitmap);
        
        // Handle pointer moved event
        void OnPointerMoved(SKPoint point, SKPoint? lastPoint, SKBitmap bitmap);
        
        // Handle pointer released event
        void OnPointerReleased(SKPoint point, SKBitmap bitmap);
        
        // Draw any tool-specific overlays
        void DrawOverlay(SKCanvas canvas, SKBitmap bitmap);
        
        // Get the cursor for this tool
        StandardCursorType GetCursor(SKPoint point);
    }
} 