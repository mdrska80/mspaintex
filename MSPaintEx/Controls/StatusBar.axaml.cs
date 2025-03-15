using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace MSPaintEx.Controls
{
    public partial class StatusBar : UserControl
    {
        private TextBlock _coordinatesText;
        private TextBlock _canvasSizeText;
        
        public StatusBar()
        {
            InitializeComponent();
            
            _coordinatesText = this.FindControl<TextBlock>("CoordinatesText");
            _canvasSizeText = this.FindControl<TextBlock>("CanvasSizeText");
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
        
        /// <summary>
        /// Updates the coordinates display in the status bar
        /// </summary>
        /// <param name="x">X coordinate in bitmap space</param>
        /// <param name="y">Y coordinate in bitmap space</param>
        public void UpdateCoordinates(float x, float y)
        {
            if (_coordinatesText != null)
            {
                _coordinatesText.Text = $"{(int)x}, {(int)y} px";
            }
        }
        
        /// <summary>
        /// Updates the canvas size display in the status bar
        /// </summary>
        /// <param name="width">Canvas width in pixels</param>
        /// <param name="height">Canvas height in pixels</param>
        public void UpdateCanvasSize(int width, int height)
        {
            if (_canvasSizeText != null)
            {
                _canvasSizeText.Text = $"{width} x {height} px";
            }
        }
    }
} 