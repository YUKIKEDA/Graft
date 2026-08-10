using System.Windows;

namespace SampleWpfApp;

public partial class ModalWindow : Window
{
    public ModalWindow()
    {
        InitializeComponent();
    }

    private void CloseModalButton_OnClick(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }
}
