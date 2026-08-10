using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;

namespace SampleWpfApp;

public partial class MainWindow : Window
{
    private int clickCount;

    public MainWindow()
    {
        InitializeComponent();
        LoadListItems();
    }

    private void LoadListItems()
    {
        var items = new ObservableCollection<SampleListItem>();
        for (var i = 0; i < 50; i++)
        {
            items.Add(new SampleListItem($"ListItem-{i:D2}", $"Item {i}"));
        }

        SampleList.ItemsSource = items;

        var comboItems = new ObservableCollection<SampleListItem>
        {
            new("ComboItem-00", "Alpha"),
            new("ComboItem-01", "Beta"),
            new("ComboItem-02", "Gamma"),
        };
        SampleCombo.ItemsSource = comboItems;
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

    private void SampleList_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (SampleList.SelectedItem is SampleListItem item)
        {
            StatusText.Text = $"Selected {item.Name}";
        }
    }

    private void SampleCombo_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (SampleCombo.SelectedItem is SampleListItem item)
        {
            StatusText.Text = $"Combo {item.Name}";
        }
    }

    private void SampleTreeRoot_OnExpanded(object sender, RoutedEventArgs e)
    {
        StatusText.Text = "Expanded";
    }

    private void SampleTreeRoot_OnCollapsed(object sender, RoutedEventArgs e)
    {
        StatusText.Text = "Collapsed";
    }
}
