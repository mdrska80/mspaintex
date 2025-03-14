using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using System.Diagnostics;
using MSPaintEx.Services;

namespace MSPaintEx.Views
{
    public partial class AboutWindow : Window
    {
        private const string REPO_URL = "https://github.com/yourusername/MSPaintEx";
        private const string LOG_SOURCE = "AboutWindow";

        public AboutWindow()
        {
            InitializeComponent();
        }

        private void OnTitleBarPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            BeginMoveDrag(e);
        }

        private void OnCloseClick(object? sender, RoutedEventArgs e)
        {
            Close();
        }

        private void OnOKClick(object? sender, RoutedEventArgs e)
        {
            Close();
        }

        private void OnViewSourceClick(object? sender, RoutedEventArgs e)
        {
            try
            {
                LogService.LogInfo(LOG_SOURCE, "Opening repository URL");
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = REPO_URL,
                    UseShellExecute = true
                };
                Process.Start(psi);
            }
            catch (System.Exception ex)
            {
                LogService.LogError(LOG_SOURCE, "Failed to open repository URL", ex);
            }
        }
    }
} 