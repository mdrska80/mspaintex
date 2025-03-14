using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Interactivity;
using Avalonia.Media;
using System;
using System.Collections.Generic;

namespace MSPaintEx.Controls
{
    public partial class ColorPaletteSelector : UserControl
    {
        // Event that will be raised when a color is selected
        public event EventHandler<Color>? ColorSelected;
        
        // Track the currently selected button
        private Button? _selectedButton;

        // Base colors for each row
        private readonly Dictionary<string, Color> BaseColors = new()
        {
            // Original colors
            { "Gray", Color.Parse("#808080") },
            { "Red", Color.Parse("#CC0000") },
            { "Orange", Color.Parse("#E69138") },
            { "Yellow", Color.Parse("#F1C232") },
            { "Green", Color.Parse("#6AA84F") },
            { "Teal", Color.Parse("#45818E") },
            { "Blue", Color.Parse("#3D85C6") },
            
            // New colors - Warm spectrum
            { "Dark Red", Color.Parse("#990000") },
            { "Burgundy", Color.Parse("#800020") },
            { "Coral", Color.Parse("#FF7F50") },
            { "Salmon", Color.Parse("#FA8072") },
            { "Peach", Color.Parse("#FFCBA4") },
            { "Brown", Color.Parse("#8B4513") },
            { "Tan", Color.Parse("#D2B48C") },
            { "Gold", Color.Parse("#FFD700") },
            
            // New colors - Cool spectrum
            { "Navy", Color.Parse("#000080") },
            { "Royal Blue", Color.Parse("#4169E1") },
            { "Sky Blue", Color.Parse("#87CEEB") },
            { "Turquoise", Color.Parse("#40E0D0") },
            { "Mint", Color.Parse("#98FF98") },
            { "Forest Green", Color.Parse("#228B22") },
            { "Olive", Color.Parse("#808000") },
            
            // New colors - Purple spectrum
            { "Purple", Color.Parse("#800080") },
            { "Magenta", Color.Parse("#FF00FF") },
            { "Plum", Color.Parse("#DDA0DD") },
            { "Lavender", Color.Parse("#E6E6FA") },
            
            // New colors - Neutral spectrum
            { "Charcoal", Color.Parse("#36454F") },
            { "Warm Gray", Color.Parse("#808069") }
        };

        public ColorPaletteSelector()
        {
            InitializeComponent();
            InitializeColorPalette();
        }

        private void InitializeColorPalette()
        {
            // Get the StackPanel that will hold our color rows
            var colorGrid = this.FindControl<StackPanel>("ColorGrid");
            if (colorGrid == null) return;

            // Create rows for each base color
            foreach (var baseColor in BaseColors)
            {
                var grid = new Grid
                {
                    Margin = new Thickness(0),
                    ColumnDefinitions = new ColumnDefinitions("*,*,*,*,*,*,*")
                };

                // Generate gradient colors
                var colors = GenerateGradient(baseColor.Value);

                // Add buttons for each color in the gradient
                for (int i = 0; i < colors.Count; i++)
                {
                    var button = CreateColorButton(colors[i]);
                    button.SetValue(Grid.ColumnProperty, i);
                    grid.Children.Add(button);
                }

                colorGrid.Children.Add(grid);
            }
        }

        private List<Color> GenerateGradient(Color baseColor)
        {
            var colors = new List<Color>();
            
            // Add darkest color (30% of base)
            colors.Add(AdjustBrightness(baseColor, 0.3));
            // Add darker color (60% of base)
            colors.Add(AdjustBrightness(baseColor, 0.6));
            // Add slightly darker color (85% of base)
            colors.Add(AdjustBrightness(baseColor, 0.85));
            // Add base color
            colors.Add(baseColor);
            // Add lighter color (mix with 25% white)
            colors.Add(MixWithWhite(baseColor, 0.25));
            // Add lighter color (mix with 50% white)
            colors.Add(MixWithWhite(baseColor, 0.50));
            // Add lightest color (mix with 75% white)
            colors.Add(MixWithWhite(baseColor, 0.75));

            return colors;
        }

        private Color AdjustBrightness(Color color, double factor)
        {
            return Color.FromArgb(
                color.A,
                (byte)(color.R * factor),
                (byte)(color.G * factor),
                (byte)(color.B * factor)
            );
        }

        private Color MixWithWhite(Color color, double whiteAmount)
        {
            byte MixChannel(byte c) => (byte)(c + (255 - c) * whiteAmount);
            
            return Color.FromArgb(
                color.A,
                MixChannel(color.R),
                MixChannel(color.G),
                MixChannel(color.B)
            );
        }

        private void UpdateSelectedButton(Button newSelection)
        {
            // Remove selection from previous button
            if (_selectedButton != null)
            {
                if (_selectedButton.Content is Grid oldGrid)
                {
                    oldGrid.Children.RemoveAt(1); // Remove selection rectangle
                }
            }

            // Update selection on new button
            _selectedButton = newSelection;
            if (_selectedButton.Content is Grid newGrid)
            {
                // Add selection indicator
                var selectionRect = new Rectangle
                {
                    Stroke = Application.Current?.Resources["SystemControlForegroundBaseHighBrush"] as IBrush ?? Brushes.White,
                    StrokeThickness = 2,
                    Margin = new Thickness(2),
                    IsHitTestVisible = false
                };
                newGrid.Children.Add(selectionRect);
            }
        }

        private Button CreateColorButton(Color color)
        {
            var grid = new Grid
            {
                Background = Brushes.Transparent
            };

            var colorRectangle = new Rectangle
            {
                Fill = new SolidColorBrush(color),
                Height = 34
            };

            grid.Children.Add(colorRectangle);

            var button = new Button
            {
                Background = Brushes.Transparent,
                Padding = new Thickness(0),
                Margin = new Thickness(1),
                BorderThickness = new Thickness(0),
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch,
                Tag = color.ToString(),
                Content = grid
            };

            button.Click += OnColorButtonClick;
            return button;
        }

        private void OnColorButtonClick(object? sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string colorHex)
            {
                // Convert hex to Color
                var color = Color.Parse(colorHex);
                
                // Update selection
                UpdateSelectedButton(button);
                
                // Update the current color display
                if (CurrentColorDisplay != null)
                {
                    CurrentColorDisplay.Fill = new SolidColorBrush(color);
                }

                // Update color format displays
                if (RgbText != null)
                {
                    RgbText.Text = $"RGB({color.R},{color.G},{color.B})";
                }
                if (HexText != null)
                {
                    HexText.Text = color.ToString().ToUpper();
                }

                // Raise the color selected event
                ColorSelected?.Invoke(this, color);
            }
        }

        // Property for the current color
        public Color CurrentColor
        {
            get => CurrentColorDisplay?.Fill is SolidColorBrush brush ? brush.Color : Colors.Black;
            set
            {
                if (CurrentColorDisplay != null)
                {
                    CurrentColorDisplay.Fill = new SolidColorBrush(value);
                    
                    // Update color format displays
                    if (RgbText != null)
                    {
                        RgbText.Text = $"RGB({value.R},{value.G},{value.B})";
                    }
                    if (HexText != null)
                    {
                        HexText.Text = value.ToString().ToUpper();
                    }
                    
                    // Find and update button with matching color
                    if (_selectedButton?.Tag is string currentColorHex)
                    {
                        var newColorHex = value.ToString();
                        if (currentColorHex != newColorHex)
                        {
                            var colorGrid = this.FindControl<StackPanel>("ColorGrid");
                            if (colorGrid != null)
                            {
                                foreach (Grid row in colorGrid.Children)
                                {
                                    foreach (Button button in row.Children)
                                    {
                                        if (button.Tag?.ToString() == newColorHex)
                                        {
                                            UpdateSelectedButton(button);
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }
} 