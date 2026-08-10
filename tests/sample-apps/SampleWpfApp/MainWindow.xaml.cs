using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Win32;

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

        var multiItems = new ObservableCollection<SampleListItem>();
        for (var i = 0; i < 20; i++)
        {
            multiItems.Add(new SampleListItem($"MultiListItem-{i:D2}", $"Multi {i}"));
        }

        SampleMultiList.ItemsSource = multiItems;

        var comboItems = new ObservableCollection<SampleListItem>
        {
            new SampleListItem("ComboItem-00", "Alpha"),
            new SampleListItem("ComboItem-01", "Beta"),
            new SampleListItem("ComboItem-02", "Gamma"),
        };
        SampleCombo.ItemsSource = comboItems;

        var gridItems = new ObservableCollection<SampleListItem>();
        for (var i = 0; i < 50; i++)
        {
            gridItems.Add(new SampleListItem($"GridRow-{i:D2}", $"Row {i}", active: false));
        }

        SampleGrid.ItemsSource = gridItems;

        var multiGridItems = new ObservableCollection<SampleListItem>();
        for (var i = 0; i < 20; i++)
        {
            multiGridItems.Add(new SampleListItem($"MultiGridRow-{i:D2}", $"MultiRow {i}"));
        }

        SampleMultiGrid.ItemsSource = multiGridItems;
    }

    private void SampleButton_OnClick(object sender, RoutedEventArgs e)
    {
        clickCount++;
        StatusText.Text = $"Clicked {clickCount}";
    }

    private void SampleMenuPing_OnClick(object sender, RoutedEventArgs e)
    {
        StatusText.Text = "MenuPing";
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

    private void ContextMenuPing_OnClick(object sender, RoutedEventArgs e)
    {
        StatusText.Text = "ContextMenuPing";
    }

    private void SampleSlider_OnValueChanged(
        object sender,
        RoutedPropertyChangedEventArgs<double> e
    )
    {
        StatusText.Text = string.Create(CultureInfo.InvariantCulture, $"Slider {e.NewValue}");
    }

    private void SampleTabs_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (SampleTabs.SelectedItem is TabItem { Header: string header })
        {
            StatusText.Text = $"Tab {header}";
        }
    }

    private void SampleList_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (SampleList.SelectedItem is SampleListItem item)
        {
            StatusText.Text = $"Selected {item.Name}";
        }
    }

    private void SampleMultiList_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        StatusText.Text = string.Create(
            CultureInfo.InvariantCulture,
            $"Multi {SampleMultiList.SelectedItems.Count}"
        );
    }

    private void SampleGrid_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (SampleGrid.SelectedItem is SampleListItem item)
        {
            StatusText.Text = $"Grid {item.Name}";
        }
    }

    private void SampleMultiGrid_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        StatusText.Text = string.Create(
            CultureInfo.InvariantCulture,
            $"MultiGrid {SampleMultiGrid.SelectedItems.Count}"
        );
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

    private void OpenChildWindowButton_OnClick(object sender, RoutedEventArgs e)
    {
        var child = new ChildWindow { Owner = this };
        child.Show();
        StatusText.Text = "ChildOpened";
    }

    private void OpenModalWindowButton_OnClick(object sender, RoutedEventArgs e)
    {
        var modal = new ModalWindow { Owner = this };
        StatusText.Text = "ModalOpening";
        modal.ShowDialog();
        StatusText.Text = "ModalClosed";
    }

    private void OpenProgressWindowButton_OnClick(object sender, RoutedEventArgs e)
    {
        NextScreenPanel.Visibility = Visibility.Collapsed;
        var progress = new ProgressWindow { Owner = this };
        progress.Closed += (_, _) =>
        {
            NextScreenPanel.Visibility = Visibility.Visible;
            StatusText.Text = "ProgressClosed";
        };
        progress.Show();
        StatusText.Text = "ProgressOpened";
    }

    private void OpenFileButton_OnClick(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog();
        var result = dialog.ShowDialog(this);
        StatusText.Text = result == true ? $"OpenFile {dialog.FileName}" : "OpenFileCancel";
    }

    private void SaveFileButton_OnClick(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog();
        var result = dialog.ShowDialog(this);
        StatusText.Text = result == true ? $"SaveFile {dialog.FileName}" : "SaveFileCancel";
    }

    private void OpenFolderButton_OnClick(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog();
        var result = dialog.ShowDialog(this);
        StatusText.Text = result == true ? $"OpenFolder {dialog.FolderName}" : "OpenFolderCancel";
    }

    private void MessageBoxButton_OnClick(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show(
            this,
            "Continue?",
            "Graft Sample",
            System.Windows.MessageBoxButton.YesNo,
            MessageBoxImage.Question
        );
        StatusText.Text = $"MessageBox {result}";
    }
}
