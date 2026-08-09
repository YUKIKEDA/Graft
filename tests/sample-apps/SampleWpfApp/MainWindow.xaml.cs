using System.Windows;

namespace SampleWpfApp;

public partial class MainWindow : Window
{
    private int clickCount;

    public MainWindow()
    {
        InitializeComponent();
    }

    private void SampleButton_OnClick(object sender, RoutedEventArgs e)
    {
        clickCount++;
        StatusText.Text = $"Clicked {clickCount}";
    }
}
