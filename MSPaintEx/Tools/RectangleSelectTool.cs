using Avalonia.Input;
using SkiaSharp;
using System;

namespace MSPaintEx.Tools
{
    public class RectangleSelectTool : ToolBase
    {
        private SKRect _selectionRect;
        private bool _isSelecting = false;
        private SKPoint _startPoint;
        
        // Constants for better visibility
        private const float BORDER_THICKNESS = 2.0f;
        private const float HANDLE_SIZE = 10.0f;
        
        public RectangleSelectTool(ToolManager toolManager) : base(toolManager)
        {
        }
        
        public override void OnPointerPressed(SKPoint point, SKBitmap bitmap)
        {
            // Set the drawing flag and last point
            _isDrawing = true;
            _lastPoint = point;
            
            // Get the current zoom factor
            double zoomFactor = _toolManager.GetZoomFactor();
            
            // Convert screen coordinates to bitmap coordinates
            float bitmapX = point.X / (float)zoomFactor;
            float bitmapY = point.Y / (float)zoomFactor;
            
            // Round to nearest integer pixel
            int pixelX = (int)Math.Round(bitmapX);
            int pixelY = (int)Math.Round(bitmapY);
            
            // Convert back to screen coordinates
            float snappedX = pixelX * (float)zoomFactor;
            float snappedY = pixelY * (float)zoomFactor;
            
            System.Diagnostics.Debug.WriteLine($"RectangleSelectTool: Pointer pressed at screen: {point.X}, {point.Y}, bitmap: {pixelX}, {pixelY}, snapped: {snappedX}, {snappedY}");
            
            SKPoint snappedPoint = new SKPoint(snappedX, snappedY);
            
            // Check if we're clicking on a resize handle
            var handle = _toolManager.GetResizeHandleAtPoint(snappedX, snappedY);
            if (handle != ToolManager.ResizeHandle.None && _toolManager.HasSelection)
            {
                System.Diagnostics.Debug.WriteLine($"RectangleSelectTool: Clicked on resize handle {handle}");
                _toolManager.SetActiveResizeHandle(handle);
                _toolManager.SetIsResizingSelection(true);
                _toolManager.SetOriginalSelectionRect(_toolManager.SelectionRect);
                _toolManager.SetSelectionMoveStart(snappedPoint);
                return;
            }
            
            // Check if we're clicking inside the selection
            if (_toolManager.HasSelection && _toolManager.SelectionRect.Contains(snappedX, snappedY))
            {
                System.Diagnostics.Debug.WriteLine($"RectangleSelectTool: Clicked inside selection");
                _toolManager.SetIsMovingSelection(true);
                _toolManager.SetSelectionMoveStart(snappedPoint);
                return;
            }
            
            // If we have a selection and clicked outside, commit the selection
            if (_toolManager.HasSelection)
            {
                System.Diagnostics.Debug.WriteLine($"RectangleSelectTool: Committing existing selection");
                CommitSelection(bitmap);
            }
            
            // Start a new selection
            _isSelecting = true;
            _startPoint = snappedPoint;
            _selectionRect = new SKRect(snappedX, snappedY, snappedX, snappedY);
            
            // Initialize the ToolManager's selection rectangle
            _toolManager.SetSelectionRect(_selectionRect);
            _toolManager.SetHasSelection(false); // Don't set to true until mouse release
            
            System.Diagnostics.Debug.WriteLine($"RectangleSelectTool: Started new selection at bitmap: {pixelX}, {pixelY}, screen: {snappedX}, {snappedY}");
        }
        
        public override void OnPointerMoved(SKPoint point, SKPoint? lastPoint, SKBitmap bitmap)
        {
            // Update the last point
            _lastPoint = point;
            
            // Get the current zoom factor
            double zoomFactor = _toolManager.GetZoomFactor();
            
            // Convert screen coordinates to bitmap coordinates
            float bitmapX = point.X / (float)zoomFactor;
            float bitmapY = point.Y / (float)zoomFactor;
            
            // Round to nearest integer pixel
            int pixelX = (int)Math.Round(bitmapX);
            int pixelY = (int)Math.Round(bitmapY);
            
            // Convert back to screen coordinates
            float snappedX = pixelX * (float)zoomFactor;
            float snappedY = pixelY * (float)zoomFactor;
            
            // Handle resizing selection
            if (_toolManager.IsResizingSelection && _toolManager.HasSelection)
            {
                HandleSelectionResize(snappedX, snappedY);
                return;
            }
            
            // Handle moving selection
            if (_toolManager.IsMovingSelection && _toolManager.HasSelection)
            {
                HandleSelectionMove(snappedX, snappedY);
                return;
            }
            
            // Handle drawing selection rectangle
            if (_isDrawing && _isSelecting)
            {
                // Update the selection rectangle
                _selectionRect = new SKRect(
                    _startPoint.X,
                    _startPoint.Y,
                    snappedX,
                    snappedY
                );
                
                // Update the ToolManager's selection rectangle
                _toolManager.SetSelectionRect(_selectionRect);
                
                System.Diagnostics.Debug.WriteLine($"RectangleSelectTool: Drawing selection from {_startPoint.X}, {_startPoint.Y} to bitmap: {pixelX}, {pixelY}, screen: {snappedX}, {snappedY}");
            }
        }
        
        public override void OnPointerReleased(SKPoint point, SKBitmap bitmap)
        {
            // Get the current zoom factor
            double zoomFactor = _toolManager.GetZoomFactor();
            
            // Convert screen coordinates to bitmap coordinates
            float bitmapX = point.X / (float)zoomFactor;
            float bitmapY = point.Y / (float)zoomFactor;
            
            // Round to nearest integer pixel
            int pixelX = (int)Math.Round(bitmapX);
            int pixelY = (int)Math.Round(bitmapY);
            
            // Convert back to screen coordinates
            float snappedX = pixelX * (float)zoomFactor;
            float snappedY = pixelY * (float)zoomFactor;
            
            System.Diagnostics.Debug.WriteLine($"RectangleSelectTool: Pointer released at screen: {point.X}, {point.Y}, bitmap: {pixelX}, {pixelY}, snapped: {snappedX}, {snappedY}");
            
            // Handle resizing completion
            if (_toolManager.IsResizingSelection)
            {
                System.Diagnostics.Debug.WriteLine($"RectangleSelectTool: Finished resizing selection");
                
                // Ensure the final selection rectangle is snapped to the pixel grid
                SKRect rect = _toolManager.SelectionRect;
                
                // Convert each corner to bitmap coordinates and round
                float left = (float)Math.Floor(rect.Left / zoomFactor);
                float top = (float)Math.Floor(rect.Top / zoomFactor);
                float right = (float)Math.Ceiling(rect.Right / zoomFactor);
                float bottom = (float)Math.Ceiling(rect.Bottom / zoomFactor);
                
                // Convert back to screen coordinates
                rect.Left = left * (float)zoomFactor;
                rect.Top = top * (float)zoomFactor;
                rect.Right = right * (float)zoomFactor;
                rect.Bottom = bottom * (float)zoomFactor;
                
                _toolManager.SetSelectionRect(rect);
                
                UpdateSelectionBitmap(bitmap);
                _toolManager.SetIsResizingSelection(false);
                _toolManager.SetActiveResizeHandle(ToolManager.ResizeHandle.None);
                _isDrawing = false;
                return;
            }
            
            // Handle moving completion
            if (_toolManager.IsMovingSelection)
            {
                System.Diagnostics.Debug.WriteLine($"RectangleSelectTool: Finished moving selection");
                
                // Get the selection rectangle and offset
                var rect = _toolManager.SelectionRect;
                var offset = _toolManager.SelectionOffset;
                
                // Convert offset to bitmap coordinates and round
                float offsetX = (float)Math.Round(offset.X / zoomFactor);
                float offsetY = (float)Math.Round(offset.Y / zoomFactor);
                
                // Convert back to screen coordinates
                offset.X = offsetX * (float)zoomFactor;
                offset.Y = offsetY * (float)zoomFactor;
                
                // Apply the snapped offset
                rect.Offset(offset.X, offset.Y);
                
                // Ensure the final position is snapped to the pixel grid
                // Convert each corner to bitmap coordinates and round
                float left = (float)Math.Floor(rect.Left / zoomFactor);
                float top = (float)Math.Floor(rect.Top / zoomFactor);
                float right = (float)Math.Ceiling(rect.Right / zoomFactor);
                float bottom = (float)Math.Ceiling(rect.Bottom / zoomFactor);
                
                // Convert back to screen coordinates
                rect.Left = left * (float)zoomFactor;
                rect.Top = top * (float)zoomFactor;
                rect.Right = right * (float)zoomFactor;
                rect.Bottom = bottom * (float)zoomFactor;
                
                _toolManager.SetSelectionRect(rect);
                _toolManager.SetSelectionOffset(new SKPoint(0, 0));
                _toolManager.SetIsMovingSelection(false);
                _isDrawing = false;
                return;
            }
            
            // Handle selection completion
            if (_isSelecting && _isDrawing)
            {
                // Update the final point of the selection rectangle
                _selectionRect.Right = snappedX;
                _selectionRect.Bottom = snappedY;
                
                // Normalize the selection rectangle
                _selectionRect = NormalizeRect(_selectionRect);
                
                // Ensure the selection rectangle is snapped to the pixel grid
                // Convert each corner to bitmap coordinates and round
                float left = (float)Math.Floor(_selectionRect.Left / zoomFactor);
                float top = (float)Math.Floor(_selectionRect.Top / zoomFactor);
                float right = (float)Math.Ceiling(_selectionRect.Right / zoomFactor);
                float bottom = (float)Math.Ceiling(_selectionRect.Bottom / zoomFactor);
                
                // Convert back to screen coordinates
                _selectionRect.Left = left * (float)zoomFactor;
                _selectionRect.Top = top * (float)zoomFactor;
                _selectionRect.Right = right * (float)zoomFactor;
                _selectionRect.Bottom = bottom * (float)zoomFactor;
                
                System.Diagnostics.Debug.WriteLine($"RectangleSelectTool: Final selection from bitmap: {left}, {top} to {right}, {bottom}, screen: {_selectionRect.Left}, {_selectionRect.Top} to {_selectionRect.Right}, {_selectionRect.Bottom}");
                
                // Only create a selection if it has a reasonable size
                if (_selectionRect.Width > 5 && _selectionRect.Height > 5)
                {
                    // Set the selection rectangle
                    _toolManager.SetSelectionRect(_selectionRect);
                    
                    // Create the selection bitmap
                    UpdateSelectionBitmap(bitmap);
                    
                    // Set HasSelection to true
                    _toolManager.SetHasSelection(true);
                    
                    System.Diagnostics.Debug.WriteLine($"RectangleSelectTool: Selection created successfully, HasSelection = {_toolManager.HasSelection}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"RectangleSelectTool: Selection too small, ignoring");
                }
            }
            
            // Reset state
            _isSelecting = false;
            _isDrawing = false;
            _lastPoint = null;
        }
        
        public override void DrawOverlay(SKCanvas canvas, SKBitmap bitmap)
        {
            // Draw selection in progress (while dragging)
            if (_isDrawing && _isSelecting)
            {
                // Normalize the rectangle for display
                SKRect normalizedRect = NormalizeRect(_selectionRect);
                
                // Ensure the rectangle has integer coordinates
                normalizedRect.Left = (float)Math.Floor(normalizedRect.Left);
                normalizedRect.Top = (float)Math.Floor(normalizedRect.Top);
                normalizedRect.Right = (float)Math.Ceiling(normalizedRect.Right);
                normalizedRect.Bottom = (float)Math.Ceiling(normalizedRect.Bottom);
                
                // Draw a semi-transparent blue overlay
                var overlayPaint = new SKPaint
                {
                    Style = SKPaintStyle.Fill,
                    Color = new SKColor(0, 0, 255, 32)
                };
                
                // Draw the overlay as four rectangles around the selection
                // Top
                canvas.DrawRect(0, 0, bitmap.Width, normalizedRect.Top, overlayPaint);
                // Left
                canvas.DrawRect(0, normalizedRect.Top, normalizedRect.Left, normalizedRect.Bottom, overlayPaint);
                // Right
                canvas.DrawRect(normalizedRect.Right, normalizedRect.Top, bitmap.Width, normalizedRect.Bottom, overlayPaint);
                // Bottom
                canvas.DrawRect(0, normalizedRect.Bottom, bitmap.Width, bitmap.Height, overlayPaint);
                
                // Draw white dashed line
                var whitePaint = new SKPaint
                {
                    Style = SKPaintStyle.Stroke,
                    Color = SKColors.White,
                    StrokeWidth = 1.0f,
                    PathEffect = SKPathEffect.CreateDash(new float[] { 4, 4 }, 0),
                    IsAntialias = false // Disable antialiasing for pixel-perfect lines
                };
                canvas.DrawRect(normalizedRect, whitePaint);
                
                // Draw black dashed line with offset
                var blackPaint = new SKPaint
                {
                    Style = SKPaintStyle.Stroke,
                    Color = SKColors.Black,
                    StrokeWidth = 1.0f,
                    PathEffect = SKPathEffect.CreateDash(new float[] { 4, 4 }, 4),
                    IsAntialias = false // Disable antialiasing for pixel-perfect lines
                };
                canvas.DrawRect(normalizedRect, blackPaint);
            }
            
            // Draw existing selection (if not moving)
            if (_toolManager.HasSelection && !_toolManager.IsMovingSelection) return;

            // Draw the selection bitmap if we're in the process of moving it
            if (_toolManager.IsMovingSelection && _toolManager.SelectionBitmap != null)
            {
                // Get the selection rectangle with offset
                SKRect offsetRect = _toolManager.SelectionRect;
                offsetRect.Offset(_toolManager.SelectionOffset.X, _toolManager.SelectionOffset.Y);
                
                // Ensure the rectangle has integer coordinates
                offsetRect.Left = (float)Math.Floor(offsetRect.Left);
                offsetRect.Top = (float)Math.Floor(offsetRect.Top);
                offsetRect.Right = (float)Math.Ceiling(offsetRect.Right);
                offsetRect.Bottom = (float)Math.Ceiling(offsetRect.Bottom);
                
                // Draw the selection bitmap at the new position
                canvas.DrawBitmap(_toolManager.SelectionBitmap, offsetRect.Left, offsetRect.Top);
                
                // Draw a dashed border around the moved selection
                var whitePaint = new SKPaint
                {
                    Style = SKPaintStyle.Stroke,
                    Color = SKColors.White,
                    StrokeWidth = 1.0f,
                    PathEffect = SKPathEffect.CreateDash(new float[] { 4, 4 }, 0),
                    IsAntialias = false // Disable antialiasing for pixel-perfect lines
                };
                canvas.DrawRect(offsetRect, whitePaint);
                
                // Draw black dashed line with offset
                var blackPaint = new SKPaint
                {
                    Style = SKPaintStyle.Stroke,
                    Color = SKColors.Black,
                    StrokeWidth = 1.0f,
                    PathEffect = SKPathEffect.CreateDash(new float[] { 4, 4 }, 4),
                    IsAntialias = false // Disable antialiasing for pixel-perfect lines
                };
                canvas.DrawRect(offsetRect, blackPaint);
            }
        }
        
        // Helper method to normalize a rectangle (ensure positive width and height)
        private SKRect NormalizeRect(SKRect rect)
        {
            return new SKRect(
                Math.Min(rect.Left, rect.Right),
                Math.Min(rect.Top, rect.Bottom),
                Math.Max(rect.Left, rect.Right),
                Math.Max(rect.Top, rect.Bottom)
            );
        }
        
        // Handle selection resize
        private void HandleSelectionResize(float x, float y)
        {
            // Get the original selection rectangle
            SKRect newRect = _toolManager.SelectionRect;
            
            // Store the original selection move start point
            SKPoint originalStart = _toolManager.SelectionMoveStart;
            
            // Update the selection move start point to the current position
            // This makes the resize incremental rather than based on the initial click position
            _toolManager.SetSelectionMoveStart(new SKPoint(x, y));
            
            // Calculate the delta from the previous position
            float deltaX = x - originalStart.X;
            float deltaY = y - originalStart.Y;
            
            // Get the current zoom factor
            double zoomFactor = _toolManager.GetZoomFactor();
            
            // Apply the resize based on which handle is active
            switch (_toolManager.ActiveResizeHandle)
            {
                case ToolManager.ResizeHandle.TopLeft:
                    newRect.Left += deltaX;
                    newRect.Top += deltaY;
                    break;
                case ToolManager.ResizeHandle.TopCenter:
                    newRect.Top += deltaY;
                    break;
                case ToolManager.ResizeHandle.TopRight:
                    newRect.Right += deltaX;
                    newRect.Top += deltaY;
                    break;
                case ToolManager.ResizeHandle.MiddleLeft:
                    newRect.Left += deltaX;
                    break;
                case ToolManager.ResizeHandle.MiddleRight:
                    newRect.Right += deltaX;
                    break;
                case ToolManager.ResizeHandle.BottomLeft:
                    newRect.Left += deltaX;
                    newRect.Bottom += deltaY;
                    break;
                case ToolManager.ResizeHandle.BottomCenter:
                    newRect.Bottom += deltaY;
                    break;
                case ToolManager.ResizeHandle.BottomRight:
                    newRect.Right += deltaX;
                    newRect.Bottom += deltaY;
                    break;
            }
            
            // Ensure the rectangle has positive width and height
            if (newRect.Width > 0 && newRect.Height > 0)
            {
                _toolManager.SetSelectionRect(newRect);
                System.Diagnostics.Debug.WriteLine($"RectangleSelectTool: Resized to {newRect.Left}, {newRect.Top}, {newRect.Right}, {newRect.Bottom}");
            }
        }
        
        // Handle selection move
        private void HandleSelectionMove(float x, float y)
        {
            // Calculate the offset from the start position
            SKPoint offset = new SKPoint(
                x - _toolManager.SelectionMoveStart.X,
                y - _toolManager.SelectionMoveStart.Y
            );
            
            _toolManager.SetSelectionOffset(offset);
        }
        
        // Update the selection bitmap
        private void UpdateSelectionBitmap(SKBitmap bitmap)
        {
            if (bitmap == null) return;
            
            // Get and normalize the selection rectangle
            var selectionRect = NormalizeRect(_toolManager.SelectionRect);
            
            // Ensure the selection has integer coordinates
            selectionRect.Left = (float)Math.Floor(selectionRect.Left);
            selectionRect.Top = (float)Math.Floor(selectionRect.Top);
            selectionRect.Right = (float)Math.Ceiling(selectionRect.Right);
            selectionRect.Bottom = (float)Math.Ceiling(selectionRect.Bottom);
            
            // Ensure the selection is within the bitmap bounds
            selectionRect.Left = Math.Max(0, selectionRect.Left);
            selectionRect.Top = Math.Max(0, selectionRect.Top);
            selectionRect.Right = Math.Min(bitmap.Width, selectionRect.Right);
            selectionRect.Bottom = Math.Min(bitmap.Height, selectionRect.Bottom);
            
            // Update the selection rectangle in the tool manager
            _toolManager.SetSelectionRect(selectionRect);
            
            System.Diagnostics.Debug.WriteLine($"RectangleSelectTool: Creating selection bitmap from {selectionRect.Left}, {selectionRect.Top} to {selectionRect.Right}, {selectionRect.Bottom}");
            
            // Only create a selection if it has a reasonable size
            if (selectionRect.Width <= 5 || selectionRect.Height <= 5)
            {
                System.Diagnostics.Debug.WriteLine($"RectangleSelectTool: Selection too small, ignoring");
                return;
            }
            
            // Create a new bitmap for the selection with integer dimensions
            int width = (int)selectionRect.Width;
            int height = (int)selectionRect.Height;
            
            System.Diagnostics.Debug.WriteLine($"RectangleSelectTool: Creating selection bitmap with size {width}x{height}");
            
            var newSelectionBitmap = new SKBitmap(width, height);
            
            using (var canvas = new SKCanvas(newSelectionBitmap))
            {
                // Clear with transparent background
                canvas.Clear(SKColors.Transparent);
                
                // Draw the portion of the main bitmap that corresponds to the selection area
                var srcRect = new SKRect(
                    selectionRect.Left, 
                    selectionRect.Top, 
                    selectionRect.Right, 
                    selectionRect.Bottom);
                
                var destRect = new SKRect(0, 0, width, height);
                
                canvas.DrawBitmap(bitmap, srcRect, destRect);
            }
            
            // Set the new selection bitmap
            _toolManager.SetSelectionBitmap(newSelectionBitmap);
            
            System.Diagnostics.Debug.WriteLine($"RectangleSelectTool: Selection bitmap created successfully");
        }
        
        // Commit the current selection (apply any changes)
        private void CommitSelection(SKBitmap bitmap)
        {
            if (!_toolManager.HasSelection || bitmap == null || _toolManager.SelectionBitmap == null) return;
            
            System.Diagnostics.Debug.WriteLine($"RectangleSelectTool: Committing selection");
            
            using (var canvas = new SKCanvas(bitmap))
            {
                // If the selection was moved, clear the original area and draw at the new position
                if (!_toolManager.SelectionOffset.Equals(new SKPoint(0, 0)))
                {
                    // Clear the original selection area with white
                    var clearPaint = new SKPaint { Color = SKColors.White };
                    canvas.DrawRect(_toolManager.SelectionRect, clearPaint);
                    
                    // Draw the selection at its new position
                    var destRect = _toolManager.SelectionRect;
                    destRect.Offset(_toolManager.SelectionOffset.X, _toolManager.SelectionOffset.Y);
                    canvas.DrawBitmap(_toolManager.SelectionBitmap, destRect.Left, destRect.Top);
                }
                else
                {
                    // If the selection was resized, just draw it at its current position
                    canvas.DrawBitmap(_toolManager.SelectionBitmap, _toolManager.SelectionRect.Left, _toolManager.SelectionRect.Top);
                }
            }
            
            _toolManager.ClearSelection();
        }

        // This method is no longer needed as the DrawingCanvas handles drawing the selection rectangle
        private void DrawSelectionRectangle(SKCanvas canvas, SKRect rect)
        {
            // Method left empty as it's no longer used
        }

        // This method is no longer needed as the DrawingCanvas handles drawing the selection handles
        private void DrawHandle(SKCanvas canvas, float x, float y, SKPaint backgroundPaint, SKPaint fillPaint, SKPaint borderPaint, float size)
        {
            // Method left empty as it's no longer used
        }
        
        public override StandardCursorType GetCursor(SKPoint point)
        {
            if (!_toolManager.IsMovingSelection && !_toolManager.IsResizingSelection && _toolManager.HasSelection)
            {
                var handle = _toolManager.GetResizeHandleAtPoint(point.X, point.Y);
                switch (handle)
                {
                    case ToolManager.ResizeHandle.TopLeft:
                    case ToolManager.ResizeHandle.BottomRight:
                        return StandardCursorType.TopLeftCorner;
                    case ToolManager.ResizeHandle.TopRight:
                    case ToolManager.ResizeHandle.BottomLeft:
                        return StandardCursorType.TopRightCorner;
                    case ToolManager.ResizeHandle.TopCenter:
                    case ToolManager.ResizeHandle.BottomCenter:
                        return StandardCursorType.SizeNorthSouth;
                    case ToolManager.ResizeHandle.MiddleLeft:
                    case ToolManager.ResizeHandle.MiddleRight:
                        return StandardCursorType.SizeWestEast;
                    default:
                        if (_toolManager.SelectionRect.Contains(point.X, point.Y))
                            return StandardCursorType.SizeAll;
                        else
                            return StandardCursorType.Cross;
                }
            }
            
            return StandardCursorType.Cross;
        }

        // Helper method to snap coordinates to pixel grid based on zoom factor
        private float SnapToPixelGrid(float coordinate, double zoomFactor)
        {
            // Convert from screen coordinates to bitmap coordinates
            float bitmapCoordinate = coordinate / (float)zoomFactor;
            
            // Round to the nearest integer to ensure we're on a pixel boundary
            int pixelCoordinate = (int)Math.Round(bitmapCoordinate);
            
            // Convert back to screen coordinates
            return pixelCoordinate * (float)zoomFactor;
        }
    }
} 