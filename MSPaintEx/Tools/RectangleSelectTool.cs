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
            
            System.Diagnostics.Debug.WriteLine($"RectangleSelectTool: Pointer pressed at {point.X}, {point.Y}");
            
            // Check if we're clicking on a resize handle
            var handle = _toolManager.GetResizeHandleAtPoint(point.X, point.Y);
            if (handle != ToolManager.ResizeHandle.None && _toolManager.HasSelection)
            {
                System.Diagnostics.Debug.WriteLine($"RectangleSelectTool: Clicked on resize handle {handle}");
                _toolManager.SetActiveResizeHandle(handle);
                _toolManager.SetIsResizingSelection(true);
                _toolManager.SetOriginalSelectionRect(_toolManager.SelectionRect);
                _toolManager.SetSelectionMoveStart(point);
                return;
            }
            
            // Check if we're clicking inside the selection
            if (_toolManager.HasSelection && _toolManager.SelectionRect.Contains(point.X, point.Y))
            {
                System.Diagnostics.Debug.WriteLine($"RectangleSelectTool: Clicked inside selection");
                _toolManager.SetIsMovingSelection(true);
                _toolManager.SetSelectionMoveStart(point);
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
            _startPoint = point;
            _selectionRect = new SKRect(point.X, point.Y, point.X, point.Y);
            
            // Initialize the ToolManager's selection rectangle
            _toolManager.SetSelectionRect(_selectionRect);
            _toolManager.SetHasSelection(false); // Don't set to true until mouse release
            
            System.Diagnostics.Debug.WriteLine($"RectangleSelectTool: Started new selection at {point.X}, {point.Y}");
        }
        
        public override void OnPointerMoved(SKPoint point, SKPoint? lastPoint, SKBitmap bitmap)
        {
            // Update the last point
            _lastPoint = point;
            
            // Handle resizing selection
            if (_toolManager.IsResizingSelection && _toolManager.HasSelection)
            {
                HandleSelectionResize(point.X, point.Y);
                return;
            }
            
            // Handle moving selection
            if (_toolManager.IsMovingSelection && _toolManager.HasSelection)
            {
                HandleSelectionMove(point.X, point.Y);
                return;
            }
            
            // Handle drawing selection rectangle
            if (_isDrawing && _isSelecting)
            {
                // Update the selection rectangle
                _selectionRect = new SKRect(
                    _startPoint.X,
                    _startPoint.Y,
                    point.X,
                    point.Y
                );
                
                // Update the ToolManager's selection rectangle
                _toolManager.SetSelectionRect(_selectionRect);
                
                System.Diagnostics.Debug.WriteLine($"RectangleSelectTool: Drawing selection from {_startPoint.X}, {_startPoint.Y} to {point.X}, {point.Y}");
            }
        }
        
        public override void OnPointerReleased(SKPoint point, SKBitmap bitmap)
        {
            System.Diagnostics.Debug.WriteLine($"RectangleSelectTool: Pointer released at {point.X}, {point.Y}");
            
            // Handle resizing completion
            if (_toolManager.IsResizingSelection)
            {
                System.Diagnostics.Debug.WriteLine($"RectangleSelectTool: Finished resizing selection");
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
                var rect = _toolManager.SelectionRect;
                rect.Offset(_toolManager.SelectionOffset.X, _toolManager.SelectionOffset.Y);
                _toolManager.SetSelectionRect(rect);
                _toolManager.SetSelectionOffset(new SKPoint(0, 0));
                _toolManager.SetIsMovingSelection(false);
                _isDrawing = false;
                return;
            }
            
            // Handle selection completion
            if (_isSelecting && _isDrawing)
            {
                // Normalize the selection rectangle
                _selectionRect = NormalizeRect(_selectionRect);
                
                System.Diagnostics.Debug.WriteLine($"RectangleSelectTool: Final selection from {_selectionRect.Left}, {_selectionRect.Top} to {_selectionRect.Right}, {_selectionRect.Bottom}");
                
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
            System.Diagnostics.Debug.WriteLine($"RectangleSelectTool: DrawOverlay called with _isDrawing={_isDrawing}, HasSelection={_toolManager.HasSelection}, IsMovingSelection={_toolManager.IsMovingSelection}");
            
            // Draw selection in progress
            if (_isDrawing && _isSelecting)
            {
                SKRect normalizedRect = NormalizeRect(_selectionRect);
                System.Diagnostics.Debug.WriteLine($"RectangleSelectTool: Drawing selection in progress from {normalizedRect.Left}, {normalizedRect.Top} to {normalizedRect.Right}, {normalizedRect.Bottom}");
                DrawSelectionRectangle(canvas, normalizedRect);
            }
            
            // Draw active selection
            if (_toolManager.HasSelection && !_toolManager.IsMovingSelection)
            {
                System.Diagnostics.Debug.WriteLine($"RectangleSelectTool: Drawing active selection from {_toolManager.SelectionRect.Left}, {_toolManager.SelectionRect.Top} to {_toolManager.SelectionRect.Right}, {_toolManager.SelectionRect.Bottom}");
                
                // Draw the selection rectangle
                DrawSelectionRectangle(canvas, _toolManager.SelectionRect);
                
                // Draw resize handles
                DrawSelectionHandles(canvas, _toolManager.SelectionRect);
            }
            
            // Draw selection being moved
            if (_toolManager.IsMovingSelection && _toolManager.HasSelection)
            {
                // Get the selection rectangle with offset
                SKRect offsetRect = _toolManager.SelectionRect;
                offsetRect.Offset(_toolManager.SelectionOffset.X, _toolManager.SelectionOffset.Y);
                
                // Draw the selection bitmap at the new position
                if (_toolManager.SelectionBitmap != null)
                {
                    canvas.DrawBitmap(_toolManager.SelectionBitmap, offsetRect.Left, offsetRect.Top);
                }
                
                // Draw the selection rectangle at the new position
                DrawSelectionRectangle(canvas, offsetRect);
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
        
        // Helper method to draw a selection rectangle with high visibility
        private void DrawSelectionRectangle(SKCanvas canvas, SKRect rect)
        {
            // Draw a semi-transparent blue overlay around the selection
            var overlayPaint = new SKPaint
            {
                Style = SKPaintStyle.Fill,
                Color = new SKColor(0, 0, 255, 32)
            };
            
            // Draw the overlay as four rectangles around the selection
            // Top
            canvas.DrawRect(0, 0, canvas.DeviceClipBounds.Width, rect.Top, overlayPaint);
            // Left
            canvas.DrawRect(0, rect.Top, rect.Left, rect.Bottom, overlayPaint);
            // Right
            canvas.DrawRect(rect.Right, rect.Top, canvas.DeviceClipBounds.Width, rect.Bottom, overlayPaint);
            // Bottom
            canvas.DrawRect(0, rect.Bottom, canvas.DeviceClipBounds.Width, canvas.DeviceClipBounds.Height, overlayPaint);
            
            // Draw white dashed line
            var whitePaint = new SKPaint
            {
                Style = SKPaintStyle.Stroke,
                Color = SKColors.White,
                StrokeWidth = BORDER_THICKNESS,
                PathEffect = SKPathEffect.CreateDash(new float[] { 4, 4 }, 0)
            };
            canvas.DrawRect(rect, whitePaint);
            
            // Draw black dashed line with offset
            var blackPaint = new SKPaint
            {
                Style = SKPaintStyle.Stroke,
                Color = SKColors.Black,
                StrokeWidth = BORDER_THICKNESS,
                PathEffect = SKPathEffect.CreateDash(new float[] { 4, 4 }, 4)
            };
            canvas.DrawRect(rect, blackPaint);
        }
        
        // Helper method to draw selection handles
        private void DrawSelectionHandles(SKCanvas canvas, SKRect rect)
        {
            // Create paints for the handles
            var fillPaint = new SKPaint
            {
                Style = SKPaintStyle.Fill,
                Color = SKColors.White
            };
            
            var borderPaint = new SKPaint
            {
                Style = SKPaintStyle.Stroke,
                Color = SKColors.Black,
                StrokeWidth = 2
            };
            
            var backgroundPaint = new SKPaint
            {
                Style = SKPaintStyle.Fill,
                Color = SKColors.Black
            };
            
            // Draw handles at corners and midpoints
            // Corners
            DrawHandle(canvas, rect.Left, rect.Top, backgroundPaint, fillPaint, borderPaint);
            DrawHandle(canvas, rect.Right, rect.Top, backgroundPaint, fillPaint, borderPaint);
            DrawHandle(canvas, rect.Left, rect.Bottom, backgroundPaint, fillPaint, borderPaint);
            DrawHandle(canvas, rect.Right, rect.Bottom, backgroundPaint, fillPaint, borderPaint);
            
            // Midpoints
            DrawHandle(canvas, rect.Left + rect.Width / 2, rect.Top, backgroundPaint, fillPaint, borderPaint);
            DrawHandle(canvas, rect.Left, rect.Top + rect.Height / 2, backgroundPaint, fillPaint, borderPaint);
            DrawHandle(canvas, rect.Right, rect.Top + rect.Height / 2, backgroundPaint, fillPaint, borderPaint);
            DrawHandle(canvas, rect.Left + rect.Width / 2, rect.Bottom, backgroundPaint, fillPaint, borderPaint);
        }
        
        // Helper method to draw a single handle
        private void DrawHandle(SKCanvas canvas, float x, float y, SKPaint backgroundPaint, SKPaint fillPaint, SKPaint borderPaint)
        {
            float halfSize = HANDLE_SIZE / 2;
            
            // Draw background (slightly larger)
            canvas.DrawRect(new SKRect(
                x - halfSize - 1,
                y - halfSize - 1,
                x + halfSize + 1,
                y + halfSize + 1
            ), backgroundPaint);
            
            // Draw fill
            canvas.DrawRect(new SKRect(
                x - halfSize,
                y - halfSize,
                x + halfSize,
                y + halfSize
            ), fillPaint);
            
            // Draw border
            canvas.DrawRect(new SKRect(
                x - halfSize,
                y - halfSize,
                x + halfSize,
                y + halfSize
            ), borderPaint);
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
            
            // Create a new bitmap for the selection
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
    }
} 