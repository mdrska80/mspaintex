using Avalonia.Input;
using SkiaSharp;
using System;

namespace MSPaintEx.Tools
{
    public class ColorPickerTool : ToolBase
    {
        public ColorPickerTool(ToolManager toolManager) : base(toolManager)
        {
        }
        
        public override void OnPointerPressed(SKPoint point, SKBitmap bitmap)
        {
            base.OnPointerPressed(point, bitmap);
            
            if (bitmap == null) return;
            
            // Ensure coordinates are within bounds
            int x = (int)Math.Clamp(point.X, 0, bitmap.Width - 1);
            int y = (int)Math.Clamp(point.Y, 0, bitmap.Height - 1);
            
            // Get the color at the clicked point
            SKColor color = bitmap.GetPixel(x, y);
            
            // Set as current color
            _toolManager.SetColor(color);
            
            // Notify that color has changed
            _toolManager.RaiseColorSelected(color);
        }
        
        public override StandardCursorType GetCursor(SKPoint point)
        {
            return StandardCursorType.Hand;
        }
    }
} 