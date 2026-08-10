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
    private bool suppressStatusFromSelectionChanged = true;

    public MainWindow()
    {
        InitializeComponent();
        DoubleClickTarget.MouseDoubleClick += DoubleClickTarget_OnMouseDoubleClick;
        PreviewKeyDown += MainWindow_OnPreviewKeyDown;
        Loaded += MainWindow_OnLoaded;
        LoadListItems();
    }

    private void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
    {
        ToolTipService.SetInitialShowDelay(SamplePhase29bTipHost, 0);
        ToolTipService.SetBetweenShowDelay(SamplePhase29bTipHost, 0);
        ToolTipService.SetShowDuration(SamplePhase29bTipHost, 60000);
        // Tab/List/Grid SelectionChanged can fire during init and overwrite StatusText=Ready.
        suppressStatusFromSelectionChanged = false;
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

        var phase28Items = new ObservableCollection<SampleListItem>();
        for (var i = 0; i < 10; i++)
        {
            phase28Items.Add(
                new SampleListItem(
                    $"Phase28Row-{i:D2}",
                    $"P28-{i}",
                    active: i % 2 == 0,
                    notes: $"N{i}"
                )
            );
        }

        SamplePhase28Grid.ItemsSource = phase28Items;

        var phase29bCombo = new ObservableCollection<SampleListItem>
        {
            new SampleListItem("Phase29bCombo-00", "One"),
            new SampleListItem("Phase29bCombo-01", "Two"),
            new SampleListItem("Phase29bCombo-02", "Three"),
        };
        SamplePhase29bCombo.ItemsSource = phase29bCombo;

        var phase29bList = new ObservableCollection<SampleListItem>
        {
            new SampleListItem("Phase29bRow-00", "Alice", notes: "A1"),
            new SampleListItem("Phase29bRow-01", "Bob", notes: "B2"),
            new SampleListItem("Phase29bRow-02", "Carol", notes: "C3"),
        };
        SamplePhase29bListView.ItemsSource = phase29bList;
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

    private void SampleMenuOpenRecent_OnClick(object sender, RoutedEventArgs e)
    {
        StatusText.Text = "MenuOpenRecent";
    }

    private void RelativeChildA_OnClick(object sender, RoutedEventArgs e)
    {
        StatusText.Text = "RelA";
    }

    private void RelativeChildB_OnClick(object sender, RoutedEventArgs e)
    {
        StatusText.Text = "RelB";
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

    private void DoubleClickTarget_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        StatusText.Text = "DoubleClicked";
        AutomationProperties.SetName(DoubleClickTarget, "DoubleClicked");
        e.Handled = true;
    }

    private void HoverTarget_OnMouseEnter(object sender, MouseEventArgs e)
    {
        StatusText.Text = "Hovered";
        AutomationProperties.SetName(HoverTarget, "Hovered");
    }

    private void DragSource_OnMouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed)
        {
            return;
        }

        DragDrop.DoDragDrop(DragSource, "graft-drag", DragDropEffects.Copy);
    }

    private void DropTarget_OnDragOver(object sender, DragEventArgs e)
    {
        e.Effects = DragDropEffects.Copy;
        e.Handled = true;
    }

    private void DropTarget_OnDrop(object sender, DragEventArgs e)
    {
        StatusText.Text = "Dropped";
        AutomationProperties.SetName(DropTarget, "Dropped");
    }

    private void ClickAtPad_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        var pos = e.GetPosition(ClickAtPad);
        var label = pos.X < ClickAtPad.ActualWidth / 2 ? "ClickAtLeft" : "ClickAtRight";
        StatusText.Text = label;
        AutomationProperties.SetName(ClickAtPad, label);
    }

    private void WheelScroller_OnScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        if (e.VerticalChange == 0 && e.VerticalOffset == 0)
        {
            return;
        }

        if (WheelScroller.VerticalOffset > 0)
        {
            StatusText.Text = "WheelScrolled";
            AutomationProperties.SetName(WheelBottomLabel, "WheelScrolled");
        }
    }

    private void ContextMenuPing_OnClick(object sender, RoutedEventArgs e)
    {
        StatusText.Text = "ContextMenuPing";
    }

    private void ContextMenuSubPing_OnClick(object sender, RoutedEventArgs e)
    {
        StatusText.Text = "ContextMenuSubPing";
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
        if (suppressStatusFromSelectionChanged)
        {
            return;
        }

        if (SampleTabs.SelectedItem is TabItem { Header: string header })
        {
            StatusText.Text = $"Tab {header}";
        }
    }

    private void SampleList_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (suppressStatusFromSelectionChanged)
        {
            return;
        }

        if (SampleList.SelectedItem is SampleListItem item)
        {
            StatusText.Text = $"Selected {item.Name}";
        }
    }

    private void SampleMultiList_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (suppressStatusFromSelectionChanged)
        {
            return;
        }

        StatusText.Text = string.Create(
            CultureInfo.InvariantCulture,
            $"Multi {SampleMultiList.SelectedItems.Count}"
        );
    }

    private void SampleGrid_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (suppressStatusFromSelectionChanged)
        {
            return;
        }

        if (SampleGrid.SelectedItem is SampleListItem item)
        {
            StatusText.Text = $"Grid {item.Name}";
        }
    }

    private void SampleMultiGrid_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (suppressStatusFromSelectionChanged)
        {
            return;
        }

        StatusText.Text = string.Create(
            CultureInfo.InvariantCulture,
            $"MultiGrid {SampleMultiGrid.SelectedItems.Count}"
        );
    }

    private void SamplePhase28Grid_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (suppressStatusFromSelectionChanged)
        {
            return;
        }

        StatusText.Text = string.Create(
            CultureInfo.InvariantCulture,
            $"Phase28Sel {SamplePhase28Grid.SelectedItems.Count}"
        );
    }

    private void SamplePhase28Grid_OnCurrentCellChanged(object? sender, EventArgs e)
    {
        if (SamplePhase28Grid.CurrentCell.IsValid)
        {
            var col = SamplePhase28Grid.CurrentCell.Column?.Header?.ToString() ?? "?";
            var row = SamplePhase28Grid.Items.IndexOf(SamplePhase28Grid.CurrentCell.Item);
            StatusText.Text = string.Create(
                CultureInfo.InvariantCulture,
                $"Phase28Cell {row}:{col}"
            );
        }
    }

    private void SamplePhase29aPassword_OnPasswordChanged(object sender, RoutedEventArgs e)
    {
        StatusText.Text = string.Create(
            CultureInfo.InvariantCulture,
            $"Phase29aPassword len={SamplePhase29aPassword.Password.Length}"
        );
    }

    private void SamplePhase29bToolBarButton_OnClick(object sender, RoutedEventArgs e)
    {
        StatusText.Text = "Phase29bToolBar";
        SamplePhase29bStatusBarText.Text = "TB-clicked";
        AutomationProperties.SetName(SamplePhase29bStatusBarText, "TB-clicked");
    }

    private void SamplePhase29bOpenPopup_OnClick(object sender, RoutedEventArgs e)
    {
        SamplePhase29bPopup.IsOpen = true;
        StatusText.Text = "Phase29bPopupOpen";
    }

    private void SamplePhase29bPopupButton_OnClick(object sender, RoutedEventArgs e)
    {
        StatusText.Text = "Phase29bPopupBtn";
        SamplePhase29bPopup.IsOpen = false;
    }

    private void SamplePhase29bHyperlink_OnClick(object sender, RoutedEventArgs e)
    {
        StatusText.Text = "Phase29bHyperlink";
        e.Handled = true;
    }

    private void MainWindow_OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key is Key.F5)
        {
            StatusText.Text = "Phase29aKey F5";
            e.Handled = true;
        }
    }

    private void SampleCombo_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (suppressStatusFromSelectionChanged)
        {
            return;
        }

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
