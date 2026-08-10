using System.Windows;

namespace SampleWpfApp;

public partial class ProgressWindow : Window
{
    public ProgressWindow()
    {
        InitializeComponent();
    }

    private async void ProgressWindow_OnLoaded(object sender, RoutedEventArgs e)
    {
        for (var value = 0; value <= 100; value += 25)
        {
            SampleProgress.Value = value;
            await Task.Delay(40);
        }

        ProgressStatus.Text = "ProgressDone";
        CloseProgressButton.IsEnabled = true;
    }

    private void CloseProgressButton_OnClick(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
