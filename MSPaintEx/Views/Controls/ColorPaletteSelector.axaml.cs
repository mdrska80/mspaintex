using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Controls.Shapes;
using Avalonia.Interactivity;

namespace MSPaintEx.Views.Controls
{
    public partial class ColorPaletteSelector : UserControl
    {
        private Button CreateColorButton(Color color)
        {
            var colorRectangle = new Rectangle
            {
                Height = 24,
                Fill = new SolidColorBrush(color)
            };

            var border = new Border
            {
                Child = colorRectangle,
                BoxShadow = new BoxShadows(new BoxShadow
                {
                    OffsetX = 0,
                    OffsetY = 1,
                    Blur = 2,
                    Color = Color.FromArgb(20, 0, 0, 0)
                })
            };

            var button = new Button
            {
                Padding = new Thickness(1),
                Margin = new Thickness(1),
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(1),
                BorderBrush = new SolidColorBrush(Color.FromArgb(40, 0, 0, 0)),
                Content = border
            };

            button.Click += (s, e) => ColorSelected?.Invoke(this, new ColorSelectedEventArgs(color));
            return button;
        }

        public event EventHandler<ColorSelectedEventArgs>? ColorSelected;
    }

    public class ColorSelectedEventArgs : EventArgs
    {
        public Color Color { get; }

        public ColorSelectedEventArgs(Color color)
        {
            Color = color;
        }
    }
} 