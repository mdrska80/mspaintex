using Avalonia.Input;
using SkiaSharp;
using System;
using System.Collections.Generic;

namespace MSPaintEx.Tools
{
    public class ToolManager
    {
        // Current tool and options
        private DrawingTool _currentTool = DrawingTool.RectangleSelect;
        private bool _fillShapes = false;
        
        // Drawing settings
        private SKPaint _paint;
        
        // For shape drawing
        private SKPoint? _shapeStartPoint;
        
        // For freeform selection
        private List<SKPoint> _freeformSelectionPoints = new List<SKPoint>();
        private SKPath? _freeformSelectionPath;
        
        // Selection properties
        private bool _hasSelection;
        private SKRect _selectionRect;
        private SKBitmap? _selectionBitmap;
        private SKPaint _selectionPaint;
        private bool _isMovingSelection;
        private bool _isResizingSelection;
        private SKPoint _selectionMoveStart;
        private SKPoint _selectionOffset;
        private ResizeHandle _activeResizeHandle = ResizeHandle.None;
        private SKRect _originalSelectionRect;
        
        // Resize handle size
        private const float HANDLE_SIZE = 8;
        
        // Dictionary of tools
        private Dictionary<DrawingTool, ITool> _tools = new Dictionary<DrawingTool, ITool>();
        
        // Current active tool
        private ITool _activeTool;
        
        // Enum to track which resize handle is active
        public enum ResizeHandle
        {
            None,
            TopLeft,
            TopCenter,
            TopRight,
            MiddleLeft,
            MiddleRight,
            BottomLeft,
            BottomCenter,
            BottomRight
        }
        
        // Event for color selection
        public event EventHandler<SKColor>? ColorSelected;
        
        public ToolManager()
        {
            // Initialize paint
            _paint = new SKPaint
            {
                Style = SKPaintStyle.Stroke,
                Color = SKColors.Black,
                StrokeWidth = 1,
                StrokeJoin = SKStrokeJoin.Round,
                StrokeCap = SKStrokeCap.Round,
                // Disable antialiasing for pixel-perfect drawing
                IsAntialias = false
            };
            
            // Initialize selection paint
            _selectionPaint = new SKPaint
            {
                Style = SKPaintStyle.Stroke,
                Color = SKColors.Black,
                StrokeWidth = 1,
                PathEffect = SKPathEffect.CreateDash(new float[] { 5, 5 }, 0)
            };
            
            // Initialize tools
            InitializeTools();
            
            // Set the active tool
            _activeTool = _tools[_currentTool];
        }
        
        // Initialize all tools
        private void InitializeTools()
        {
            _tools[DrawingTool.RectangleSelect] = new RectangleSelectTool(this);
            _tools[DrawingTool.FreeformSelect] = new FreeformSelectTool(this);
            _tools[DrawingTool.Pencil] = new PencilTool(this);
            _tools[DrawingTool.Brush] = new BrushTool(this);
            _tools[DrawingTool.Eraser] = new EraserTool(this);
            _tools[DrawingTool.Fill] = new FillTool(this);
            _tools[DrawingTool.Text] = new TextTool(this);
            _tools[DrawingTool.ColorPicker] = new ColorPickerTool(this);
            _tools[DrawingTool.Line] = new LineTool(this);
            _tools[DrawingTool.Rectangle] = new RectangleTool(this);
            _tools[DrawingTool.Ellipse] = new EllipseTool(this);
            _tools[DrawingTool.RoundedRectangle] = new RoundedRectangleTool(this);
            _tools[DrawingTool.Polygon] = new PolygonTool(this);
        }
        
        // Get the current tool
        public DrawingTool CurrentTool => _currentTool;
        
        // Set the current tool
        public void SetTool(DrawingTool tool)
        {
            _currentTool = tool;
            
            // Set the active tool
            if (_tools.ContainsKey(tool))
            {
                _activeTool = _tools[tool];
            }
            else
            {
                // If the tool is not implemented yet, use the pencil tool as a fallback
                _activeTool = _tools[DrawingTool.Pencil];
            }
        }
        
        // Handle pointer pressed event
        public void OnPointerPressed(SKPoint point, SKBitmap bitmap)
        {
            _activeTool.OnPointerPressed(point, bitmap);
        }
        
        // Handle pointer moved event
        public void OnPointerMoved(SKPoint point, SKPoint? lastPoint, SKBitmap bitmap)
        {
            _activeTool.OnPointerMoved(point, lastPoint, bitmap);
        }
        
        // Handle pointer released event
        public void OnPointerReleased(SKPoint point, SKBitmap bitmap)
        {
            System.Diagnostics.Debug.WriteLine($"ToolManager.OnPointerReleased called with point: {point.X},{point.Y}, HasSelection before: {_hasSelection}");
            _activeTool.OnPointerReleased(point, bitmap);
            System.Diagnostics.Debug.WriteLine($"ToolManager.OnPointerReleased after active tool call, HasSelection after: {_hasSelection}");
        }
        
        // Draw any tool-specific overlays
        public void DrawOverlay(SKCanvas canvas, SKBitmap bitmap)
        {
            _activeTool.DrawOverlay(canvas, bitmap);
        }
        
        // Get the cursor for the current tool
        public StandardCursorType GetCursor(SKPoint point)
        {
            return _activeTool.GetCursor(point);
        }
        
        // Set fill option for shapes
        public void SetFillShapes(bool fill)
        {
            _fillShapes = fill;
        }
        
        // Get fill option for shapes
        public bool FillShapes => _fillShapes;
        
        // Method to set the current drawing color
        public void SetColor(SKColor color)
        {
            if (_paint != null)
            {
                _paint.Color = color;
            }
        }
        
        // Get the current drawing color
        public SKColor CurrentColor => _paint.Color;

        // Method to set the stroke width
        public void SetStrokeWidth(float width)
        {
            if (_paint != null)
            {
                _paint.StrokeWidth = width;
            }
        }
        
        // Get the current stroke width
        public float StrokeWidth => _paint.StrokeWidth;
        
        // Get the paint object
        public SKPaint GetPaint()
        {
            return _paint;
        }
        
        // Get the selection paint object
        public SKPaint GetSelectionPaint()
        {
            return _selectionPaint;
        }
        
        // Check if a point is near a resize handle
        public ResizeHandle GetResizeHandleAtPoint(float x, float y)
        {
            if (!_hasSelection) return ResizeHandle.None;
            
            // Get the current zoom factor to adjust handle hit testing
            double zoomFactor = GetZoomFactor();
            
            // Adjust handle size based on zoom factor
            float handleSize = HANDLE_SIZE / (float)zoomFactor;
            float halfSize = handleSize / 2;
            
            // Check corners first (they take precedence)
            if (Math.Abs(x - _selectionRect.Left) <= halfSize && Math.Abs(y - _selectionRect.Top) <= halfSize)
                return ResizeHandle.TopLeft;
                
            if (Math.Abs(x - _selectionRect.Right) <= halfSize && Math.Abs(y - _selectionRect.Top) <= halfSize)
                return ResizeHandle.TopRight;
                
            if (Math.Abs(x - _selectionRect.Left) <= halfSize && Math.Abs(y - _selectionRect.Bottom) <= halfSize)
                return ResizeHandle.BottomLeft;
                
            if (Math.Abs(x - _selectionRect.Right) <= halfSize && Math.Abs(y - _selectionRect.Bottom) <= halfSize)
                return ResizeHandle.BottomRight;
            
            // Then check edges
            if (Math.Abs(x - (_selectionRect.Left + _selectionRect.Width / 2)) <= halfSize && Math.Abs(y - _selectionRect.Top) <= halfSize)
                return ResizeHandle.TopCenter;
                
            if (Math.Abs(x - (_selectionRect.Left + _selectionRect.Width / 2)) <= halfSize && Math.Abs(y - _selectionRect.Bottom) <= halfSize)
                return ResizeHandle.BottomCenter;
                
            if (Math.Abs(x - _selectionRect.Left) <= halfSize && Math.Abs(y - (_selectionRect.Top + _selectionRect.Height / 2)) <= halfSize)
                return ResizeHandle.MiddleLeft;
                
            if (Math.Abs(x - _selectionRect.Right) <= halfSize && Math.Abs(y - (_selectionRect.Top + _selectionRect.Height / 2)) <= halfSize)
                return ResizeHandle.MiddleRight;
            
            return ResizeHandle.None;
        }
        
        // Selection properties and methods
        public bool HasSelection => _hasSelection;
        public SKRect SelectionRect => _selectionRect;
        public SKBitmap? SelectionBitmap => _selectionBitmap;
        public bool IsMovingSelection => _isMovingSelection;
        public bool IsResizingSelection => _isResizingSelection;
        public SKPoint SelectionMoveStart => _selectionMoveStart;
        public SKPoint SelectionOffset => _selectionOffset;
        public ResizeHandle ActiveResizeHandle => _activeResizeHandle;
        
        // Set selection properties
        public void SetHasSelection(bool hasSelection)
        {
            System.Diagnostics.Debug.WriteLine($"SetHasSelection called with value: {hasSelection}");
            _hasSelection = hasSelection;
        }
        
        public void SetSelectionRect(SKRect rect)
        {
            System.Diagnostics.Debug.WriteLine($"SetSelectionRect called with rect: {rect.Left},{rect.Top} to {rect.Right},{rect.Bottom}");
            _selectionRect = rect;
        }
        
        public void SetSelectionBitmap(SKBitmap bitmap)
        {
            System.Diagnostics.Debug.WriteLine($"SetSelectionBitmap called with bitmap: {bitmap.Width}x{bitmap.Height}");
            _selectionBitmap?.Dispose();
            _selectionBitmap = bitmap;
        }
        
        public void SetIsMovingSelection(bool isMoving)
        {
            _isMovingSelection = isMoving;
        }
        
        public void SetIsResizingSelection(bool isResizing)
        {
            _isResizingSelection = isResizing;
        }
        
        public void SetSelectionMoveStart(SKPoint point)
        {
            _selectionMoveStart = point;
        }
        
        public void SetSelectionOffset(SKPoint offset)
        {
            _selectionOffset = offset;
        }
        
        public void SetActiveResizeHandle(ResizeHandle handle)
        {
            _activeResizeHandle = handle;
        }
        
        public void SetOriginalSelectionRect(SKRect rect)
        {
            _originalSelectionRect = rect;
        }
        
        // Freeform selection properties and methods
        public List<SKPoint> FreeformSelectionPoints => _freeformSelectionPoints;
        public SKPath? FreeformSelectionPath => _freeformSelectionPath;
        
        public void SetFreeformSelectionPath(SKPath path)
        {
            _freeformSelectionPath = path;
        }
        
        public void ClearFreeformSelectionPoints()
        {
            _freeformSelectionPoints.Clear();
        }
        
        public void AddFreeformSelectionPoint(SKPoint point)
        {
            _freeformSelectionPoints.Add(point);
        }
        
        // Shape drawing properties and methods
        public SKPoint? ShapeStartPoint => _shapeStartPoint;
        
        public void SetShapeStartPoint(SKPoint? point)
        {
            _shapeStartPoint = point;
        }
        
        // Clear selection
        public void ClearSelection()
        {
            _hasSelection = false;
            _selectionBitmap?.Dispose();
            _selectionBitmap = null;
            _isMovingSelection = false;
            _isResizingSelection = false;
            _activeResizeHandle = ResizeHandle.None;
            _freeformSelectionPath = null;
            _freeformSelectionPoints.Clear();
        }
        
        // Raise color selected event
        public void RaiseColorSelected(SKColor color)
        {
            ColorSelected?.Invoke(this, color);
        }
        
        // Method to get the current drawing color
        public SKColor GetColor()
        {
            return _paint.Color;
        }
        
        // Get the active tool
        public ITool GetActiveTool()
        {
            return _activeTool;
        }
        
        // Method to get the current zoom factor
        public double GetZoomFactor()
        {
            // Try to find the canvas from the active tool
            var canvas = _activeTool?.GetCanvas();
            if (canvas != null)
            {
                return canvas.GetCurrentZoomFactor();
            }
            
            return 1.0; // Default to 1.0 if we can't get the zoom factor
        }
    }
} 