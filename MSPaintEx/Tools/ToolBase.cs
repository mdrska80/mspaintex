using Avalonia.Input;
using SkiaSharp;
using MSPaintEx.Controls;

namespace MSPaintEx.Tools
{
    public abstract class ToolBase : ITool
    {
        protected ToolManager _toolManager;
        protected SKPoint? _lastPoint;
        protected bool _isDrawing;
        protected DrawingCanvas? _canvas;
        
        public ToolBase(ToolManager toolManager)
        {
            _toolManager = toolManager;
        }
        
        public virtual void OnPointerPressed(SKPoint point, SKBitmap bitmap)
        {
            _lastPoint = point;
            _isDrawing = true;
        }
        
        public virtual void OnPointerMoved(SKPoint point, SKPoint? lastPoint, SKBitmap bitmap)
        {
            _lastPoint = point;
        }
        
        public virtual void OnPointerReleased(SKPoint point, SKBitmap bitmap)
        {
            _isDrawing = false;
            _lastPoint = null;
        }
        
        public virtual void DrawOverlay(SKCanvas canvas, SKBitmap bitmap)
        {
            // Default implementation does nothing
        }
        
        public virtual StandardCursorType GetCursor(SKPoint point)
        {
            return StandardCursorType.Cross;
        }
        
        public virtual DrawingCanvas? GetCanvas()
        {
            return _canvas;
        }
        
        public virtual void SetCanvas(DrawingCanvas canvas)
        {
            _canvas = canvas;
        }
    }
} 