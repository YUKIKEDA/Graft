using Graft.Protocol;

namespace Graft.Core.Scenario;

/// <summary>
/// Executes a compiled <see cref="ScenarioDocument"/> via <see cref="Application.LaunchAsync"/>
/// and Fluent GetBy operations.
/// </summary>
/// <remarks>
/// Failures from Expect / Invoke / SetValue surface as <see cref="GraftException"/> with
/// <see cref="GraftException.Report"/> when Core attaches diagnostics.
/// </remarks>
public static class ScenarioRunner
{
    /// <summary>
    /// Runs all operations in order, disposing the launched session afterwards.
    /// </summary>
    /// <param name="scenario">Compiled scenario.</param>
    /// <param name="options">Optional path overrides for launch.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when the scenario finishes successfully.</returns>
    /// <exception cref="GraftException">Validation, launch, or step execution failed.</exception>
    public static async Task RunAsync(ScenarioDocument scenario, ScenarioRunOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(scenario);
        if (scenario.Operations.Count == 0)
        {
            throw new GraftException(GraftErrorCodes.ActionFailed, "Scenario has no operations to run.");
        }

        if (scenario.Operations[0] is not LaunchOperation)
        {
            throw new GraftException(GraftErrorCodes.ActionFailed, "Scenario must start with a launch step.");
        }

        GraftSession? session = null;
        try
        {
            foreach (var operation in scenario.Operations)
            {
                cancellationToken.ThrowIfCancellationRequested();
                switch (operation)
                {
                    case LaunchOperation launch:
                        if (session is not null)
                        {
                            throw new GraftException(GraftErrorCodes.ActionFailed, "Scenario may contain only one launch step.");
                        }

                        session = await Application.LaunchAsync(ToLaunchOptions(launch, options), cancellationToken).ConfigureAwait(false);
                        break;

                    case InvokeOperation invoke:
                        EnsureSession(session);
                        await session!.GetBy(invoke.Target).InvokeAsync(cancellationToken).ConfigureAwait(false);
                        break;

                    case RightClickOperation rightClick:
                        EnsureSession(session);
                        await session!.GetBy(rightClick.Target).RightClickAsync(cancellationToken).ConfigureAwait(false);
                        break;

                    case DoubleClickOperation doubleClick:
                        EnsureSession(session);
                        await session!.GetBy(doubleClick.Target).DoubleClickAsync(cancellationToken).ConfigureAwait(false);
                        break;

                    case HoverOperation hover:
                        EnsureSession(session);
                        await session!.GetBy(hover.Target).HoverAsync(cancellationToken).ConfigureAwait(false);
                        break;

                    case DragOperation drag:
                        EnsureSession(session);
                        await session!.GetBy(drag.Target).DragAsync(drag.To, cancellationToken).ConfigureAwait(false);
                        break;

                    case ClickAtOperation clickAt:
                        EnsureSession(session);
                        await session!.GetBy(clickAt.Target).ClickAtAsync(clickAt.OffsetX, clickAt.OffsetY, cancellationToken).ConfigureAwait(false);
                        break;

                    case WheelOperation wheel:
                        EnsureSession(session);
                        await session!.GetBy(wheel.Target).WheelAsync(wheel.Delta, cancellationToken).ConfigureAwait(false);
                        break;

                    case SetValueOperation setValue:
                        EnsureSession(session);
                        await session!.GetBy(setValue.Target).SetValueAsync(setValue.Value, cancellationToken).ConfigureAwait(false);
                        break;

                    case ToggleOperation toggle:
                        EnsureSession(session);
                        await session!.GetBy(toggle.Target).ToggleAsync(cancellationToken).ConfigureAwait(false);
                        break;

                    case SendKeysOperation sendKeys:
                        EnsureSession(session);
                        await session!.GetBy(sendKeys.Target).SendKeysAsync(sendKeys.Text, cancellationToken).ConfigureAwait(false);
                        break;

                    case TypeHumanOperation typeHuman:
                        EnsureSession(session);
                        await session!
                            .GetBy(typeHuman.Target)
                            .TypeHumanAsync(typeHuman.Text, TimeSpan.FromMilliseconds(typeHuman.DelayMs), cancellationToken)
                            .ConfigureAwait(false);
                        break;

                    case PressKeysOperation pressKeys:
                        EnsureSession(session);
                        await session!.GetBy(pressKeys.Target).PressAsync(pressKeys.Keys, cancellationToken).ConfigureAwait(false);
                        break;

                    case ScreenshotOperation screenshot:
                        EnsureSession(session);
                        var shot = screenshot.Target is { } shotTarget
                            ? await session!.GetBy(shotTarget).ScreenshotAsync(cancellationToken).ConfigureAwait(false)
                            : await session!.ScreenshotAsync(cancellationToken).ConfigureAwait(false);
                        await shot.SaveAsync(screenshot.Path, cancellationToken).ConfigureAwait(false);
                        break;

                    case ScrollIntoViewOperation scroll:
                        EnsureSession(session);
                        if (scroll.Index is { } scrollIndex)
                        {
                            await session!.GetBy(scroll.Target).ScrollIntoViewAsync(scrollIndex, cancellationToken).ConfigureAwait(false);
                        }
                        else
                        {
                            await session!.GetBy(scroll.Target).ScrollIntoViewAsync(cancellationToken).ConfigureAwait(false);
                        }

                        break;

                    case SelectOperation select:
                        EnsureSession(session);
                        if (select.Key is not null)
                        {
                            await session!.GetBy(select.Target).SelectAsync(select.Key, cancellationToken).ConfigureAwait(false);
                        }
                        else
                        {
                            await session!.GetBy(select.Target).SelectAsync(select.Index!.Value, cancellationToken).ConfigureAwait(false);
                        }

                        break;

                    case SelectManyOperation selectMany:
                        EnsureSession(session);
                        await session!.GetBy(selectMany.Target).SelectManyAsync(selectMany.Indexes, cancellationToken).ConfigureAwait(false);
                        break;

                    case SelectMenuOperation selectMenu:
                        EnsureSession(session);
                        await session!.GetBy(selectMenu.Target).SelectMenuAsync(selectMenu.Path, cancellationToken).ConfigureAwait(false);
                        break;

                    case SelectTreeOperation selectTree:
                        EnsureSession(session);
                        await session!.GetBy(selectTree.Target).SelectTreeAsync(selectTree.Path, cancellationToken).ConfigureAwait(false);
                        break;

                    case ExpandOperation expand:
                        EnsureSession(session);
                        await session!.GetBy(expand.Target).ExpandAsync(cancellationToken).ConfigureAwait(false);
                        break;

                    case CollapseOperation collapse:
                        EnsureSession(session);
                        await session!.GetBy(collapse.Target).CollapseAsync(cancellationToken).ConfigureAwait(false);
                        break;

                    case ExpectNameOperation expectName:
                        EnsureSession(session);
                        await session!.GetBy(expectName.Target).ExpectNameAsync(expectName.Name, cancellationToken).ConfigureAwait(false);
                        break;

                    case ExpectSelectedOperation expectSelected:
                        EnsureSession(session);
                        await session!
                            .GetBy(expectSelected.Target)
                            .ExpectSelectedAsync(expectSelected.Selected, cancellationToken)
                            .ConfigureAwait(false);
                        break;

                    case ExpectExpandedOperation expectExpanded:
                        EnsureSession(session);
                        await session!
                            .GetBy(expectExpanded.Target)
                            .ExpectExpandedAsync(expectExpanded.Expanded, cancellationToken)
                            .ConfigureAwait(false);
                        break;

                    case ExpectCheckedOperation expectChecked:
                        EnsureSession(session);
                        await session!.GetBy(expectChecked.Target).ExpectCheckedAsync(expectChecked.Checked, cancellationToken).ConfigureAwait(false);
                        break;

                    case ExpectEnabledOperation expectEnabled:
                        EnsureSession(session);
                        await session!.GetBy(expectEnabled.Target).ExpectEnabledAsync(expectEnabled.Enabled, cancellationToken).ConfigureAwait(false);
                        break;

                    case ExpectVisibleOperation expectVisible:
                        EnsureSession(session);
                        await session!.GetBy(expectVisible.Target).ExpectVisibleAsync(expectVisible.Visible, cancellationToken).ConfigureAwait(false);
                        break;

                    case ExpectFocusedOperation expectFocused:
                        EnsureSession(session);
                        await session!.GetBy(expectFocused.Target).ExpectFocusedAsync(cancellationToken).ConfigureAwait(false);
                        break;

                    case ExpectNameContainsOperation expectNameContains:
                        EnsureSession(session);
                        await session!
                            .GetBy(expectNameContains.Target)
                            .ExpectNameContainsAsync(expectNameContains.Substring, cancellationToken)
                            .ConfigureAwait(false);
                        break;

                    case ExpectNameMatchesOperation expectNameMatches:
                        EnsureSession(session);
                        await session!
                            .GetBy(expectNameMatches.Target)
                            .ExpectNameMatchesAsync(expectNameMatches.Pattern, cancellationToken)
                            .ConfigureAwait(false);
                        break;

                    case ExpectValueOperation expectValue:
                        EnsureSession(session);
                        await session!.GetBy(expectValue.Target).ExpectValueAsync(expectValue.Value, cancellationToken).ConfigureAwait(false);
                        break;

                    case ExpectToolTipOperation expectToolTip:
                        EnsureSession(session);
                        await session!.GetBy(expectToolTip.Target).ExpectToolTipAsync(expectToolTip.ToolTip, cancellationToken).ConfigureAwait(false);
                        break;

                    case WaitForOperation waitFor:
                        EnsureSession(session);
                        await session!.GetBy(waitFor.Target).WaitForAsync(cancellationToken).ConfigureAwait(false);
                        break;

                    case ExpectGoneOperation expectGone:
                        EnsureSession(session);
                        await session!.GetBy(expectGone.Target).ExpectGoneAsync(cancellationToken).ConfigureAwait(false);
                        break;

                    case GetCellTextOperation getCellText:
                        EnsureSession(session);
                        _ = getCellText.ColumnKey is null
                            ? await session!
                                .GetBy(getCellText.Target)
                                .GetCellTextAsync(getCellText.Row, getCellText.Column!.Value, cancellationToken)
                                .ConfigureAwait(false)
                            : await session!
                                .GetBy(getCellText.Target)
                                .GetCellTextAsync(getCellText.Row, getCellText.ColumnKey, cancellationToken)
                                .ConfigureAwait(false);
                        break;

                    case SetCellValueOperation setCellValue:
                        EnsureSession(session);
                        if (setCellValue.ColumnKey is null)
                        {
                            await session!
                                .GetBy(setCellValue.Target)
                                .SetCellValueAsync(setCellValue.Row, setCellValue.Column!.Value, setCellValue.Value, cancellationToken)
                                .ConfigureAwait(false);
                        }
                        else
                        {
                            await session!
                                .GetBy(setCellValue.Target)
                                .SetCellValueAsync(setCellValue.Row, setCellValue.ColumnKey, setCellValue.Value, cancellationToken)
                                .ConfigureAwait(false);
                        }

                        break;

                    case SelectCellOperation selectCell:
                        EnsureSession(session);
                        if (selectCell.ColumnKey is null)
                        {
                            await session!
                                .GetBy(selectCell.Target)
                                .SelectCellAsync(selectCell.Row, selectCell.Column!.Value, cancellationToken)
                                .ConfigureAwait(false);
                        }
                        else
                        {
                            await session!
                                .GetBy(selectCell.Target)
                                .SelectCellAsync(selectCell.Row, selectCell.ColumnKey, cancellationToken)
                                .ConfigureAwait(false);
                        }

                        break;

                    case SelectRowOperation selectRow:
                        EnsureSession(session);
                        await session!
                            .GetBy(selectRow.Target)
                            .SelectRowAsync(selectRow.ColumnKey, selectRow.Value, cancellationToken)
                            .ConfigureAwait(false);
                        break;

                    case ClickColumnHeaderOperation clickColumnHeader:
                        EnsureSession(session);
                        await session!
                            .GetBy(clickColumnHeader.Target)
                            .ClickColumnHeaderAsync(clickColumnHeader.ColumnKey, cancellationToken)
                            .ConfigureAwait(false);
                        break;

                    case AddRowOperation addRow:
                        EnsureSession(session);
                        await session!.GetBy(addRow.Target).AddRowAsync(cancellationToken).ConfigureAwait(false);
                        break;

                    case DeleteSelectedRowsOperation deleteSelectedRows:
                        EnsureSession(session);
                        await session!.GetBy(deleteSelectedRows.Target).DeleteSelectedRowsAsync(cancellationToken).ConfigureAwait(false);
                        break;

                    case ExpectCellTextOperation expectCellText:
                        EnsureSession(session);
                        if (expectCellText.ColumnKey is null)
                        {
                            await session!
                                .GetBy(expectCellText.Target)
                                .ExpectCellTextAsync(expectCellText.Row, expectCellText.Column!.Value, expectCellText.Text, cancellationToken)
                                .ConfigureAwait(false);
                        }
                        else
                        {
                            await session!
                                .GetBy(expectCellText.Target)
                                .ExpectCellTextAsync(expectCellText.Row, expectCellText.ColumnKey, expectCellText.Text, cancellationToken)
                                .ConfigureAwait(false);
                        }

                        break;

                    case ArmOpenFileOperation armOpenFile:
                        EnsureSession(session);
                        await session!.ArmOpenFileAsync(armOpenFile.Path, cancellationToken).ConfigureAwait(false);
                        break;

                    case ArmOpenFileCancelOperation:
                        EnsureSession(session);
                        await session!.ArmOpenFileCancelAsync(cancellationToken).ConfigureAwait(false);
                        break;

                    case ArmSaveFileOperation armSaveFile:
                        EnsureSession(session);
                        await session!.ArmSaveFileAsync(armSaveFile.Path, cancellationToken).ConfigureAwait(false);
                        break;

                    case ArmSaveFileCancelOperation:
                        EnsureSession(session);
                        await session!.ArmSaveFileCancelAsync(cancellationToken).ConfigureAwait(false);
                        break;

                    case ArmOpenFolderOperation armOpenFolder:
                        EnsureSession(session);
                        await session!.ArmOpenFolderAsync(armOpenFolder.Path, cancellationToken).ConfigureAwait(false);
                        break;

                    case ArmOpenFolderCancelOperation:
                        EnsureSession(session);
                        await session!.ArmOpenFolderCancelAsync(cancellationToken).ConfigureAwait(false);
                        break;

                    case ArmMessageBoxOperation armMessageBox:
                        EnsureSession(session);
                        await session!.ArmMessageBoxAsync(armMessageBox.Result, cancellationToken).ConfigureAwait(false);
                        break;

                    case ListWindowsOperation:
                        EnsureSession(session);
                        _ = await session!.ListWindowsAsync(cancellationToken).ConfigureAwait(false);
                        break;

                    case SwitchWindowOperation switchWindow:
                        EnsureSession(session);
                        await session!.SwitchToWindowAsync(switchWindow.WindowId, cancellationToken).ConfigureAwait(false);
                        break;

                    case WaitForWindowOperation waitForWindow:
                        EnsureSession(session);
                        _ = await session!
                            .WaitForWindowAsync(waitForWindow.Title, waitForWindow.AutomationId, waitForWindow.SwitchTo, cancellationToken)
                            .ConfigureAwait(false);
                        break;

                    case WaitForWindowClosedOperation waitForWindowClosed:
                        EnsureSession(session);
                        await session!
                            .WaitForWindowClosedAsync(waitForWindowClosed.Title, waitForWindowClosed.AutomationId, cancellationToken)
                            .ConfigureAwait(false);
                        break;

                    case InvokeOpeningWindowOperation invokeOpening:
                        EnsureSession(session);
                        _ = await session!
                            .GetBy(invokeOpening.Target)
                            .InvokeOpeningWindowAsync(invokeOpening.WaitForNewWindow, cancellationToken)
                            .ConfigureAwait(false);
                        break;

                    default:
                        throw new GraftException(GraftErrorCodes.ActionFailed, $"Unsupported Scenario operation '{operation.Action}'.");
                }
            }
        }
        finally
        {
            if (session is not null)
            {
                await session.DisposeAsync().ConfigureAwait(false);
            }
        }
    }

    private static void EnsureSession(GraftSession? session)
    {
        if (session is null)
        {
            throw new GraftException(GraftErrorCodes.ActionFailed, "Scenario step requires an active session; launch must run first.");
        }
    }

    private static LaunchOptions ToLaunchOptions(LaunchOperation launch, ScenarioRunOptions? options)
    {
        var appPath = ResolveAppPath(launch.AppPath, options);
        return new LaunchOptions
        {
            AppPath = appPath,
            Configuration = LaunchOptions.NormalizeConfiguration(launch.Configuration),
            Timeout = launch.Timeout ?? LaunchOptions.DefaultTimeout,
        };
    }

    private static string ResolveAppPath(string scenarioAppPath, ScenarioRunOptions? options)
    {
        if (!string.IsNullOrWhiteSpace(options?.AppPath))
        {
            return Path.GetFullPath(options.AppPath);
        }

        if (Path.IsPathRooted(scenarioAppPath))
        {
            return scenarioAppPath;
        }

        if (!string.IsNullOrWhiteSpace(options?.WorkingDirectory))
        {
            return Path.GetFullPath(Path.Combine(options.WorkingDirectory, scenarioAppPath));
        }

        return Path.GetFullPath(scenarioAppPath);
    }
}
