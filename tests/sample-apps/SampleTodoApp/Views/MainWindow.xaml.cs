using System.Windows;
using System.Windows.Input;
using R3;
using SampleTodoApp.ViewModels;

namespace SampleTodoApp.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void TodoGrid_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm && vm.EditCommand.CanExecute())
        {
            vm.EditCommand.Execute(Unit.Default);
        }
    }
}
