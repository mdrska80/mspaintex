using Avalonia.Input;
using SkiaSharp;
using System;

namespace MSPaintEx.Tools
{
    public class FreeformSelectTool : ToolBase
    {
        public FreeformSelectTool(ToolManager toolManager) : base(toolManager)
        {
        }
        
        public override void OnPointerPressed(SKPoint point, SKBitmap bitmap)
        {
            // Check if we're clicking on a resize handle
            var handle = _toolManager.GetResizeHandleAtPoint(point.X, point.Y);
            if (handle != ToolManager.ResizeHandle.None)
            {
                _toolManager.SetActiveResizeHandle(handle);
                _toolManager.SetIsResizingSelection(true);
                _toolManager.SetOriginalSelectionRect(_toolManager.SelectionRect);
                _toolManager.SetSelectionMoveStart(point);
                return;
            }
            
            // Check if we're clicking inside the selection
            if (_toolManager.HasSelection && _toolManager.SelectionRect.Contains(point.X, point.Y))
            {
                _toolManager.SetIsMovingSelection(true);
                _toolManager.SetSelectionMoveStart(point);
                return;
            }
            
            // If we have a selection and clicked outside, commit the selection
            if (_toolManager.HasSelection)
            {
                CommitSelection(bitmap);
            }
            
            // Start a new freeform selection
            _toolManager.ClearFreeformSelectionPoints();
            _toolManager.AddFreeformSelectionPoint(point);
            _toolManager.SetFreeformSelectionPath(new SKPath());
            _toolManager.FreeformSelectionPath?.MoveTo(point);
            _lastPoint = point;
            _isDrawing = true;
            _toolManager.SetHasSelection(false);
        }
        
        public override void OnPointerMoved(SKPoint point, SKPoint? lastPoint, SKBitmap bitmap)
        {
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
            
            // Handle drawing freeform selection path
            if (_isDrawing && lastPoint.HasValue && _toolManager.FreeformSelectionPath != null)
            {
                // Only add points if they're a minimum distance away from the last point
                // to avoid too many points for small movements
                float minDistance = 2.0f;
                float dx = point.X - lastPoint.Value.X;
                float dy = point.Y - lastPoint.Value.Y;
                float distance = (float)Math.Sqrt(dx * dx + dy * dy);
                
                if (distance >= minDistance)
                {
                    _toolManager.AddFreeformSelectionPoint(point);
                    _toolManager.FreeformSelectionPath.LineTo(point);
                    _lastPoint = point; // Update the last point for proper drawing
                }
            }
        }
        
        public override void OnPointerReleased(SKPoint point, SKBitmap bitmap)
        {
            if (_toolManager.IsResizingSelection)
            {
                // When resizing is complete, update the selection bitmap
                UpdateSelectionBitmap(bitmap);
                _toolManager.SetIsResizingSelection(false);
                _toolManager.SetActiveResizeHandle(ToolManager.ResizeHandle.None);
                return;
            }
            
            if (_toolManager.IsMovingSelection)
            {
                // Apply the move to the selection rectangle
                var rect = _toolManager.SelectionRect;
                rect.Offset(_toolManager.SelectionOffset.X, _toolManager.SelectionOffset.Y);
                _toolManager.SetSelectionRect(rect);
                _toolManager.SetSelectionOffset(new SKPoint(0, 0));
                _toolManager.SetIsMovingSelection(false);
                return;
            }
            
            if (_isDrawing && _toolManager.FreeformSelectionPath != null)
            {
                _isDrawing = false;
                
                // Close the path if it has enough points
                if (_toolManager.FreeformSelectionPoints.Count >= 3)
                {
                    // Close the path
                    _toolManager.FreeformSelectionPath.Close();
                    
                    // Calculate the bounds of the path
                    var selectionRect = _toolManager.FreeformSelectionPath.ComputeTightBounds();
                    _toolManager.SetSelectionRect(selectionRect);
                    
                    // Create a selection bitmap with the freeform shape
                    CreateFreeformSelectionBitmap(bitmap);
                    
                    _toolManager.SetHasSelection(true);
                }
                
                _lastPoint = null;
            }
        }
        
        public override void DrawOverlay(SKCanvas canvas, SKBitmap bitmap)
        {
            // Draw freeform selection path in progress
            if (_isDrawing && !_toolManager.HasSelection && _toolManager.FreeformSelectionPath != null)
            {
                canvas.DrawPath(_toolManager.FreeformSelectionPath, _toolManager.GetSelectionPaint());
            }
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
            SKRect newRect = _toolManager.SelectionRect;
            
            // Calculate the delta from the start position
            float deltaX = x - _toolManager.SelectionMoveStart.X;
            float deltaY = y - _toolManager.SelectionMoveStart.Y;
            
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
        
        // Create a selection bitmap for freeform selection
        private void CreateFreeformSelectionBitmap(SKBitmap bitmap)
        {
            if (bitmap == null || _toolManager.FreeformSelectionPath == null) return;
            
            var selectionRect = _toolManager.SelectionRect;
            
            // Create a new bitmap for the selection area
            int width = (int)Math.Ceiling(selectionRect.Width);
            int height = (int)Math.Ceiling(selectionRect.Height);
            
            if (width <= 0 || height <= 0) return;
            
            var newSelectionBitmap = new SKBitmap(width, height);
            
            using (var canvas = new SKCanvas(newSelectionBitmap))
            {
                // Clear with transparent background
                canvas.Clear(SKColors.Transparent);
                
                // Create a translated path for the selection
                var translatedPath = new SKPath(_toolManager.FreeformSelectionPath);
                translatedPath.Transform(SKMatrix.CreateTranslation(-selectionRect.Left, -selectionRect.Top));
                
                // Create a clip path from the selection path
                canvas.ClipPath(translatedPath);
                
                // Draw the portion of the main bitmap that corresponds to the selection area
                canvas.DrawBitmap(bitmap, -selectionRect.Left, -selectionRect.Top);
            }
            
            // Set the new selection bitmap
            _toolManager.SetSelectionBitmap(newSelectionBitmap);
        }
        
        // Update the selection bitmap after resizing
        private void UpdateSelectionBitmap(SKBitmap bitmap)
        {
            if (!_toolManager.HasSelection || bitmap == null) return;
            
            var selectionRect = _toolManager.SelectionRect;
            
            // Create a new bitmap for the resized selection
            var newSelectionBitmap = new SKBitmap((int)selectionRect.Width, (int)selectionRect.Height);
            
            using (var canvas = new SKCanvas(newSelectionBitmap))
            {
                // Clear with white background
                canvas.Clear(SKColors.White);
                
                // Draw the portion of the main bitmap that corresponds to the selection area
                var srcRect = new SKRect(
                    selectionRect.Left, 
                    selectionRect.Top, 
                    selectionRect.Right, 
                    selectionRect.Bottom);
                
                canvas.DrawBitmap(bitmap, srcRect, new SKRect(0, 0, selectionRect.Width, selectionRect.Height));
            }
            
            // Set the new selection bitmap
            _toolManager.SetSelectionBitmap(newSelectionBitmap);
        }
        
        // Commit the current selection (apply any changes)
        private void CommitSelection(SKBitmap bitmap)
        {
            if (!_toolManager.HasSelection || bitmap == null || _toolManager.SelectionBitmap == null) return;
            
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