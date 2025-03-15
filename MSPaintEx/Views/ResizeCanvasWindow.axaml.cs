using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Avalonia.Data;

namespace MSPaintEx.Views
{
    public partial class ResizeCanvasWindow : Window, INotifyPropertyChanged
    {
        private int _canvasWidth;
        private int _canvasHeight;
        private int _originalWidth;
        private int _originalHeight;
        private int _resizeMode = 0; // 0 = Pixels, 1 = Percentage
        private int _scalePercentage = 100;

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
                    NotifyPropertyChanged(nameof(NewSizeText));
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
                    NotifyPropertyChanged(nameof(NewSizeText));
                }
            }
        }
        
        public int ResizeMode
        {
            get => _resizeMode;
            set
            {
                if (_resizeMode != value)
                {
                    _resizeMode = value;
                    NotifyPropertyChanged();
                    NotifyPropertyChanged(nameof(IsPixelMode));
                    NotifyPropertyChanged(nameof(IsPercentageMode));
                    
                    // Update sizes based on the new mode
                    if (value == 1) // Percentage mode
                    {
                        // Reset to 100% when switching to percentage mode
                        ScalePercentage = 100;
                    }
                    else // Pixel mode
                    {
                        // When switching back to pixel mode, use the calculated values from percentage
                        if (_scalePercentage != 100)
                        {
                            CanvasWidth = CalculateWidthFromPercentage();
                            CanvasHeight = CalculateHeightFromPercentage();
                        }
                    }
                }
            }
        }
        
        public int ScalePercentage
        {
            get => _scalePercentage;
            set
            {
                if (_scalePercentage != value)
                {
                    _scalePercentage = value;
                    NotifyPropertyChanged();
                    NotifyPropertyChanged(nameof(NewSizeFromPercentageText));
                }
            }
        }
        
        public bool IsPixelMode => _resizeMode == 0;
        public bool IsPercentageMode => _resizeMode == 1;
        
        public string CurrentSizeText => $"{_originalWidth} × {_originalHeight} pixels";
        
        public string NewSizeText => $"{CanvasWidth} × {CanvasHeight} pixels";
        
        public string NewSizeFromPercentageText
        {
            get
            {
                int width = CalculateWidthFromPercentage();
                int height = CalculateHeightFromPercentage();
                return $"New size will be: {width} × {height} pixels";
            }
        }
        
        private int CalculateWidthFromPercentage()
        {
            return Math.Max(1, (int)Math.Round(_originalWidth * _scalePercentage / 100.0));
        }
        
        private int CalculateHeightFromPercentage()
        {
            return Math.Max(1, (int)Math.Round(_originalHeight * _scalePercentage / 100.0));
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
            _originalWidth = currentWidth;
            _originalHeight = currentHeight;
            CanvasWidth = currentWidth;
            CanvasHeight = currentHeight;
            NotifyPropertyChanged(nameof(CurrentSizeText));
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
            int finalWidth, finalHeight;
            
            if (IsPercentageMode)
            {
                finalWidth = CalculateWidthFromPercentage();
                finalHeight = CalculateHeightFromPercentage();
            }
            else
            {
                finalWidth = CanvasWidth;
                finalHeight = CanvasHeight;
            }
            
            // Validate dimensions
            if (finalWidth < 1 || finalWidth > 4096 || finalHeight < 1 || finalHeight > 4096)
            {
                // TODO: Show error message
                return;
            }

            var result = new ResizeResult
            {
                Width = finalWidth,
                Height = finalHeight,
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