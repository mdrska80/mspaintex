using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MSPaintEx.Views
{
    public partial class ResizeCanvasWindow : Window, INotifyPropertyChanged
    {
        private int _canvasWidth;
        private int _canvasHeight;
        private bool _maintainAspectRatio;
        private readonly double _aspectRatio;

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void NotifyPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public int CanvasWidth
        {
            get => _canvasWidth;
            set
            {
                if (_canvasWidth != value)
                {
                    _canvasWidth = value;
                    NotifyPropertyChanged();
                    if (MaintainAspectRatio)
                    {
                        _canvasHeight = (int)(value / _aspectRatio);
                        NotifyPropertyChanged(nameof(CanvasHeight));
                    }
                }
            }
        }

        public int CanvasHeight
        {
            get => _canvasHeight;
            set
            {
                if (_canvasHeight != value)
                {
                    _canvasHeight = value;
                    NotifyPropertyChanged();
                    if (MaintainAspectRatio)
                    {
                        _canvasWidth = (int)(value * _aspectRatio);
                        NotifyPropertyChanged(nameof(CanvasWidth));
                    }
                }
            }
        }

        public bool MaintainAspectRatio
        {
            get => _maintainAspectRatio;
            set
            {
                if (_maintainAspectRatio != value)
                {
                    _maintainAspectRatio = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public ResizeCanvasWindow()
        {
            InitializeComponent();
            DataContext = this;

            // Add keyboard event handling
            KeyDown += (s, e) =>
            {
                if (e.Key == Key.Enter)
                {
                    OnResizeClick(this, new RoutedEventArgs());
                    e.Handled = true;
                }
                else if (e.Key == Key.Escape)
                {
                    OnCancelClick(this, new RoutedEventArgs());
                    e.Handled = true;
                }
            };
        }

        public ResizeCanvasWindow(int currentWidth, int currentHeight) : this()
        {
            CanvasWidth = currentWidth;
            CanvasHeight = currentHeight;
            _aspectRatio = (double)currentWidth / currentHeight;
        }

        private void OnTitleBarPointerPressed(object sender, PointerPressedEventArgs e)
        {
            BeginMoveDrag(e);
        }

        private void OnCloseClick(object sender, RoutedEventArgs e)
        {
            Close(null);
        }

        private void OnCancelClick(object sender, RoutedEventArgs e)
        {
            Close(null);
        }

        private void OnResizeClick(object sender, RoutedEventArgs e)
        {
            // Validate dimensions
            if (CanvasWidth < 1 || CanvasWidth > 4096 || CanvasHeight < 1 || CanvasHeight > 4096)
            {
                // TODO: Show error message
                return;
            }

            var result = new ResizeResult
            {
                Width = CanvasWidth,
                Height = CanvasHeight,
                Confirmed = true
            };
            Close(result);
        }
    }

    public class ResizeResult
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public bool Confirmed { get; set; }
    }
} 