using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using Graft.Instrumentation.Actions;
using Graft.Protocol;

namespace Graft.Instrumentation.Wpf;

/// <summary>
/// Shared DataGrid column, row, and cell-text helpers.
/// </summary>
internal static class WpfDataGridCells
{
    /// <summary>
    /// Returns the column index named by exactly one of <paramref name="column"/> or <paramref name="columnKey"/>.
    /// </summary>
    /// <param name="dataGrid">Target grid.</param>
    /// <param name="column">Zero-based column index, or <see langword="null"/> when using a header.</param>
    /// <param name="columnKey">Column Header string, or <see langword="null"/> when using an index.</param>
    /// <returns>The column index.</returns>
    /// <exception cref="ElementActionException">The column arguments are invalid, or the column was not found.</exception>
    internal static int ResolveColumnIndex(DataGrid dataGrid, int? column, string? columnKey)
    {
        var hasColumn = column is not null;
        var hasKey = !string.IsNullOrWhiteSpace(columnKey);
        if (hasColumn == hasKey)
        {
            throw new ElementActionException(GraftErrorCodes.SelectorInvalid, "Exactly one of params.column or params.columnKey is required.");
        }

        if (hasColumn)
        {
            var index = column!.Value;
            if (index < 0)
            {
                throw new ElementActionException(GraftErrorCodes.SelectorInvalid, "params.column must be >= 0.");
            }

            if (index >= dataGrid.Columns.Count)
            {
                throw new ElementActionException(
                    GraftErrorCodes.ElementNotFound,
                    $"Column index {index} is out of range (count={dataGrid.Columns.Count})."
                );
            }

            return index;
        }

        var key = columnKey!.Trim();
        var matches = new List<int>();
        for (var i = 0; i < dataGrid.Columns.Count; i++)
        {
            if (string.Equals(FormatHeader(dataGrid.Columns[i].Header), key, StringComparison.Ordinal))
            {
                matches.Add(i);
            }
        }

        if (matches.Count == 0)
        {
            throw new ElementActionException(GraftErrorCodes.ElementNotFound, $"No DataGrid column Header matched columnKey '{key}'.");
        }

        if (matches.Count > 1)
        {
            throw new ElementActionException(
                GraftErrorCodes.ElementAmbiguous,
                $"Multiple DataGrid columns matched columnKey '{key}' ({matches.Count})."
            );
        }

        return matches[0];
    }

    /// <summary>
    /// Throws when <paramref name="row"/> is outside the grid's items.
    /// </summary>
    /// <param name="dataGrid">Target grid.</param>
    /// <param name="row">Zero-based row index.</param>
    /// <exception cref="ElementActionException">The row index is invalid or out of range.</exception>
    internal static void EnsureRowIndex(DataGrid dataGrid, int row)
    {
        if (row < 0)
        {
            throw new ElementActionException(GraftErrorCodes.SelectorInvalid, "params.row must be >= 0.");
        }

        if (row >= dataGrid.Items.Count)
        {
            throw new ElementActionException(GraftErrorCodes.ElementNotFound, $"Row index {row} is out of range (count={dataGrid.Items.Count}).");
        }
    }

    /// <summary>
    /// Returns the realized row container at <paramref name="row"/>.
    /// </summary>
    /// <param name="dataGrid">Target grid.</param>
    /// <param name="row">Zero-based row index.</param>
    /// <returns>The row container.</returns>
    /// <exception cref="ElementActionException">The row container was not realized.</exception>
    internal static DataGridRow RequireRow(DataGrid dataGrid, int row)
    {
        if (dataGrid.ItemContainerGenerator.ContainerFromIndex(row) is not DataGridRow rowContainer)
        {
            throw new ElementActionException(GraftErrorCodes.ActionFailed, $"Failed to realize DataGrid row at index {row}.");
        }

        return rowContainer;
    }

    /// <summary>
    /// Returns the header text used to match a column key.
    /// </summary>
    /// <param name="header">Column header value.</param>
    /// <returns>The header string. An empty string when <paramref name="header"/> is <see langword="null"/>.</returns>
    internal static string FormatHeader(object? header) =>
        header switch
        {
            null => string.Empty,
            string text => text,
            _ => Convert.ToString(header, CultureInfo.InvariantCulture) ?? string.Empty,
        };

    /// <summary>
    /// Returns the text shown in a text cell.
    /// </summary>
    /// <param name="content">Cell content element.</param>
    /// <returns>The display text.</returns>
    internal static string ReadDisplayText(FrameworkElement content) =>
        content switch
        {
            TextBlock textBlock => textBlock.Text ?? string.Empty,
            TextBox textBox => textBox.Text ?? string.Empty,
            _ => WpfVisualTree.FindVisualChild<TextBlock>(content)?.Text
                ?? WpfVisualTree.FindVisualChild<TextBox>(content)?.Text
                ?? content.ToString()
                ?? string.Empty,
        };

    /// <summary>
    /// Returns <c>True</c> or <c>False</c> for a check-box cell.
    /// </summary>
    /// <param name="content">Cell content element.</param>
    /// <returns><c>True</c> when the check box is checked. Otherwise <c>False</c>.</returns>
    /// <exception cref="ElementActionException">The cell has no check box.</exception>
    internal static string ReadCheckBoxText(FrameworkElement content)
    {
        var checkBox = content as CheckBox ?? WpfVisualTree.FindVisualChild<CheckBox>(content);
        if (checkBox is null)
        {
            throw new ElementActionException(GraftErrorCodes.ActionFailed, "Failed to read CheckBox cell content.");
        }

        return checkBox.IsChecked == true ? "True" : "False";
    }

    /// <summary>
    /// Returns the text of a template cell, using the check box when the template has no text block.
    /// </summary>
    /// <param name="content">Cell content element.</param>
    /// <returns>The display text.</returns>
    internal static string ReadTemplateText(FrameworkElement content)
    {
        var checkBox = content as CheckBox ?? WpfVisualTree.FindVisualChild<CheckBox>(content);
        if (checkBox is not null && WpfVisualTree.FindVisualChild<TextBlock>(content) is null)
        {
            return checkBox.IsChecked == true ? "True" : "False";
        }

        return ReadDisplayText(content);
    }
}
