using System.Windows;

namespace SampleWpfApp;

public partial class ChildWindow : Window
{
    public ChildWindow()
    {
        InitializeComponent();
    }

    private void CloseChildButton_OnClick(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
