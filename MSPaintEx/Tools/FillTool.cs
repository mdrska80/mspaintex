using Avalonia.Input;
using SkiaSharp;
using System;
using System.Collections.Generic;

namespace MSPaintEx.Tools
{
    public class FillTool : ToolBase
    {
        public FillTool(ToolManager toolManager) : base(toolManager)
        {
        }
        
        public override void OnPointerPressed(SKPoint point, SKBitmap bitmap)
        {
            base.OnPointerPressed(point, bitmap);
            
            // Perform flood fill at the clicked point
            FloodFill(bitmap, point);
        }
        
        public override StandardCursorType GetCursor(SKPoint point)
        {
            return StandardCursorType.Hand;
        }
        
        // Flood fill algorithm
        private void FloodFill(SKBitmap bitmap, SKPoint startPoint)
        {
            if (bitmap == null) return;
            
            // Ensure coordinates are within bounds
            int x = (int)Math.Clamp(startPoint.X, 0, bitmap.Width - 1);
            int y = (int)Math.Clamp(startPoint.Y, 0, bitmap.Height - 1);
            
            // Get the target color (the color we're replacing)
            SKColor targetColor = bitmap.GetPixel(x, y);
            
            // Get the replacement color
            SKColor replacementColor = _toolManager.CurrentColor;
            
            // If the target color is the same as the replacement color, no need to fill
            if (targetColor.Equals(replacementColor)) return;
            
            // Queue for the flood fill algorithm
            Queue<(int X, int Y)> queue = new Queue<(int X, int Y)>();
            queue.Enqueue((x, y));
            
            // Directions to check (4-way connectivity)
            (int dx, int dy)[] directions = new[] 
            {
                (0, 1),   // Down
                (1, 0),   // Right
                (0, -1),  // Up
                (-1, 0)   // Left
            };
            
            // Create a canvas to draw on the bitmap
            using (var canvas = new SKCanvas(bitmap))
            {
                // Process the queue
                while (queue.Count > 0)
                {
                    var (currentX, currentY) = queue.Dequeue();
                    
                    // Skip if out of bounds
                    if (currentX < 0 || currentX >= bitmap.Width || currentY < 0 || currentY >= bitmap.Height)
                        continue;
                    
                    // Skip if the color is not the target color
                    if (!bitmap.GetPixel(currentX, currentY).Equals(targetColor))
                        continue;
                    
                    // Set the pixel to the replacement color
                    bitmap.SetPixel(currentX, currentY, replacementColor);
                    
                    // Add neighboring pixels to the queue
                    foreach (var (dx, dy) in directions)
                    {
                        queue.Enqueue((currentX + dx, currentY + dy));
                    }
                }
            }
        }
    }
} 