using Avalonia.ReactiveUI;
using FileCopyFinder.UI.RxUI.ViewModels;

namespace FileCopyFinder.UI.RxUI.Views;

public partial class MainWindow : ReactiveWindow<MainWindowViewModel>
{
    public MainWindow()
    {
        InitializeComponent();
    }
}