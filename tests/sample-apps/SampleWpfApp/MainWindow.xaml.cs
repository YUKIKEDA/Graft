using System.Windows;
using System.Windows.Automation;
using System.Windows.Input;

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

    private void SampleCheckBox_OnChecked(object sender, RoutedEventArgs e)
    {
        SampleCheckBox.Content = "On";
    }

    private void SampleCheckBox_OnUnchecked(object sender, RoutedEventArgs e)
    {
        SampleCheckBox.Content = "Off";
    }

    private void SampleMouseTarget_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        StatusText.Text = "MouseHit";
        AutomationProperties.SetName(SampleMouseTarget, "MouseHit");
    }
}
