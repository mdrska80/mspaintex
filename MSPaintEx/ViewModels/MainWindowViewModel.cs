using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace MSPaintEx.ViewModels;

// Main window view model that handles canvas operations
public partial class MainWindowViewModel : ObservableObject
{
    [ObservableProperty]
    private int _canvasWidth = 800;

    [ObservableProperty]
    private int _canvasHeight = 600;

    public MainWindowViewModel()
    {
        // Initialize any required properties or commands here
    }
}
