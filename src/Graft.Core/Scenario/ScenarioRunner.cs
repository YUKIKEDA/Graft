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
    public static async Task RunAsync(
        ScenarioDocument scenario,
        ScenarioRunOptions? options = null,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(scenario);
        if (scenario.Operations.Count == 0)
        {
            throw new GraftException(
                GraftErrorCodes.ActionFailed,
                "Scenario has no operations to run."
            );
        }

        if (scenario.Operations[0] is not LaunchOperation)
        {
            throw new GraftException(
                GraftErrorCodes.ActionFailed,
                "Scenario must start with a launch step."
            );
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
                            throw new GraftException(
                                GraftErrorCodes.ActionFailed,
                                "Scenario may contain only one launch step."
                            );
                        }

                        session = await Application
                            .LaunchAsync(ToLaunchOptions(launch, options), cancellationToken)
                            .ConfigureAwait(false);
                        break;

                    case InvokeOperation invoke:
                        EnsureSession(session);
                        await session!
                            .GetByAutomationId(invoke.AutomationId)
                            .InvokeAsync(cancellationToken)
                            .ConfigureAwait(false);
                        break;

                    case RightClickOperation rightClick:
                        EnsureSession(session);
                        await session!
                            .GetByAutomationId(rightClick.AutomationId)
                            .RightClickAsync(cancellationToken)
                            .ConfigureAwait(false);
                        break;

                    case DoubleClickOperation doubleClick:
                        EnsureSession(session);
                        await session!
                            .GetByAutomationId(doubleClick.AutomationId)
                            .DoubleClickAsync(cancellationToken)
                            .ConfigureAwait(false);
                        break;

                    case HoverOperation hover:
                        EnsureSession(session);
                        await session!
                            .GetByAutomationId(hover.AutomationId)
                            .HoverAsync(cancellationToken)
                            .ConfigureAwait(false);
                        break;

                    case DragOperation drag:
                        EnsureSession(session);
                        await session!
                            .GetByAutomationId(drag.AutomationId)
                            .DragAsync(drag.ToAutomationId, cancellationToken)
                            .ConfigureAwait(false);
                        break;

                    case ClickAtOperation clickAt:
                        EnsureSession(session);
                        await session!
                            .GetByAutomationId(clickAt.AutomationId)
                            .ClickAtAsync(clickAt.OffsetX, clickAt.OffsetY, cancellationToken)
                            .ConfigureAwait(false);
                        break;

                    case WheelOperation wheel:
                        EnsureSession(session);
                        await session!
                            .GetByAutomationId(wheel.AutomationId)
                            .WheelAsync(wheel.Delta, cancellationToken)
                            .ConfigureAwait(false);
                        break;

                    case SetValueOperation setValue:
                        EnsureSession(session);
                        await session!
                            .GetByAutomationId(setValue.AutomationId)
                            .SetValueAsync(setValue.Value, cancellationToken)
                            .ConfigureAwait(false);
                        break;

                    case ToggleOperation toggle:
                        EnsureSession(session);
                        await session!
                            .GetByAutomationId(toggle.AutomationId)
                            .ToggleAsync(cancellationToken)
                            .ConfigureAwait(false);
                        break;

                    case SendKeysOperation sendKeys:
                        EnsureSession(session);
                        await session!
                            .GetByAutomationId(sendKeys.AutomationId)
                            .SendKeysAsync(sendKeys.Text, cancellationToken)
                            .ConfigureAwait(false);
                        break;

                    case PressKeysOperation pressKeys:
                        EnsureSession(session);
                        await session!
                            .GetByAutomationId(pressKeys.AutomationId)
                            .PressAsync(pressKeys.Keys, cancellationToken)
                            .ConfigureAwait(false);
                        break;

                    case ScreenshotOperation screenshot:
                        EnsureSession(session);
                        var shot = await session!
                            .ScreenshotAsync(cancellationToken)
                            .ConfigureAwait(false);
                        await shot.SaveAsync(screenshot.Path, cancellationToken)
                            .ConfigureAwait(false);
                        break;

                    case ScrollIntoViewOperation scroll:
                        EnsureSession(session);
                        if (scroll.Index is { } scrollIndex)
                        {
                            await session!
                                .GetByAutomationId(scroll.AutomationId)
                                .ScrollIntoViewAsync(scrollIndex, cancellationToken)
                                .ConfigureAwait(false);
                        }
                        else
                        {
                            await session!
                                .GetByAutomationId(scroll.AutomationId)
                                .ScrollIntoViewAsync(cancellationToken)
                                .ConfigureAwait(false);
                        }

                        break;

                    case SelectOperation select:
                        EnsureSession(session);
                        if (select.Key is not null)
                        {
                            await session!
                                .GetByAutomationId(select.AutomationId)
                                .SelectAsync(select.Key, cancellationToken)
                                .ConfigureAwait(false);
                        }
                        else
                        {
                            await session!
                                .GetByAutomationId(select.AutomationId)
                                .SelectAsync(select.Index!.Value, cancellationToken)
                                .ConfigureAwait(false);
                        }

                        break;

                    case SelectManyOperation selectMany:
                        EnsureSession(session);
                        await session!
                            .GetByAutomationId(selectMany.AutomationId)
                            .SelectManyAsync(selectMany.Indexes, cancellationToken)
                            .ConfigureAwait(false);
                        break;

                    case SelectMenuOperation selectMenu:
                        EnsureSession(session);
                        await session!
                            .GetByAutomationId(selectMenu.AutomationId)
                            .SelectMenuAsync(selectMenu.Path, cancellationToken)
                            .ConfigureAwait(false);
                        break;

                    case SelectTreeOperation selectTree:
                        EnsureSession(session);
                        await session!
                            .GetByAutomationId(selectTree.AutomationId)
                            .SelectTreeAsync(selectTree.Path, cancellationToken)
                            .ConfigureAwait(false);
                        break;

                    case ExpandOperation expand:
                        EnsureSession(session);
                        await session!
                            .GetByAutomationId(expand.AutomationId)
                            .ExpandAsync(cancellationToken)
                            .ConfigureAwait(false);
                        break;

                    case CollapseOperation collapse:
                        EnsureSession(session);
                        await session!
                            .GetByAutomationId(collapse.AutomationId)
                            .CollapseAsync(cancellationToken)
                            .ConfigureAwait(false);
                        break;

                    case ExpectNameOperation expectName:
                        EnsureSession(session);
                        await session!
                            .GetByAutomationId(expectName.AutomationId)
                            .ExpectNameAsync(expectName.Name, cancellationToken)
                            .ConfigureAwait(false);
                        break;

                    case ExpectSelectedOperation expectSelected:
                        EnsureSession(session);
                        await session!
                            .GetByAutomationId(expectSelected.AutomationId)
                            .ExpectSelectedAsync(expectSelected.Selected, cancellationToken)
                            .ConfigureAwait(false);
                        break;

                    case ExpectExpandedOperation expectExpanded:
                        EnsureSession(session);
                        await session!
                            .GetByAutomationId(expectExpanded.AutomationId)
                            .ExpectExpandedAsync(expectExpanded.Expanded, cancellationToken)
                            .ConfigureAwait(false);
                        break;

                    case ExpectCheckedOperation expectChecked:
                        EnsureSession(session);
                        await session!
                            .GetByAutomationId(expectChecked.AutomationId)
                            .ExpectCheckedAsync(expectChecked.Checked, cancellationToken)
                            .ConfigureAwait(false);
                        break;

                    case ExpectEnabledOperation expectEnabled:
                        EnsureSession(session);
                        await session!
                            .GetByAutomationId(expectEnabled.AutomationId)
                            .ExpectEnabledAsync(expectEnabled.Enabled, cancellationToken)
                            .ConfigureAwait(false);
                        break;

                    case ExpectVisibleOperation expectVisible:
                        EnsureSession(session);
                        await session!
                            .GetByAutomationId(expectVisible.AutomationId)
                            .ExpectVisibleAsync(expectVisible.Visible, cancellationToken)
                            .ConfigureAwait(false);
                        break;

                    case ExpectFocusedOperation expectFocused:
                        EnsureSession(session);
                        await session!
                            .GetByAutomationId(expectFocused.AutomationId)
                            .ExpectFocusedAsync(cancellationToken)
                            .ConfigureAwait(false);
                        break;

                    case ExpectNameContainsOperation expectNameContains:
                        EnsureSession(session);
                        await session!
                            .GetByAutomationId(expectNameContains.AutomationId)
                            .ExpectNameContainsAsync(
                                expectNameContains.Substring,
                                cancellationToken
                            )
                            .ConfigureAwait(false);
                        break;

                    case ExpectNameMatchesOperation expectNameMatches:
                        EnsureSession(session);
                        await session!
                            .GetByAutomationId(expectNameMatches.AutomationId)
                            .ExpectNameMatchesAsync(expectNameMatches.Pattern, cancellationToken)
                            .ConfigureAwait(false);
                        break;

                    case ExpectValueOperation expectValue:
                        EnsureSession(session);
                        await session!
                            .GetByAutomationId(expectValue.AutomationId)
                            .ExpectValueAsync(expectValue.Value, cancellationToken)
                            .ConfigureAwait(false);
                        break;

                    case WaitForOperation waitFor:
                        EnsureSession(session);
                        await session!
                            .GetByAutomationId(waitFor.AutomationId)
                            .WaitForAsync(cancellationToken)
                            .ConfigureAwait(false);
                        break;

                    case ExpectGoneOperation expectGone:
                        EnsureSession(session);
                        await session!
                            .GetByAutomationId(expectGone.AutomationId)
                            .ExpectGoneAsync(cancellationToken)
                            .ConfigureAwait(false);
                        break;

                    case GetCellTextOperation getCellText:
                        EnsureSession(session);
                        _ = getCellText.ColumnKey is null
                            ? await session!
                                .GetByAutomationId(getCellText.AutomationId)
                                .GetCellTextAsync(
                                    getCellText.Row,
                                    getCellText.Column!.Value,
                                    cancellationToken
                                )
                                .ConfigureAwait(false)
                            : await session!
                                .GetByAutomationId(getCellText.AutomationId)
                                .GetCellTextAsync(
                                    getCellText.Row,
                                    getCellText.ColumnKey,
                                    cancellationToken
                                )
                                .ConfigureAwait(false);
                        break;

                    case SetCellValueOperation setCellValue:
                        EnsureSession(session);
                        if (setCellValue.ColumnKey is null)
                        {
                            await session!
                                .GetByAutomationId(setCellValue.AutomationId)
                                .SetCellValueAsync(
                                    setCellValue.Row,
                                    setCellValue.Column!.Value,
                                    setCellValue.Value,
                                    cancellationToken
                                )
                                .ConfigureAwait(false);
                        }
                        else
                        {
                            await session!
                                .GetByAutomationId(setCellValue.AutomationId)
                                .SetCellValueAsync(
                                    setCellValue.Row,
                                    setCellValue.ColumnKey,
                                    setCellValue.Value,
                                    cancellationToken
                                )
                                .ConfigureAwait(false);
                        }

                        break;

                    case SelectCellOperation selectCell:
                        EnsureSession(session);
                        if (selectCell.ColumnKey is null)
                        {
                            await session!
                                .GetByAutomationId(selectCell.AutomationId)
                                .SelectCellAsync(
                                    selectCell.Row,
                                    selectCell.Column!.Value,
                                    cancellationToken
                                )
                                .ConfigureAwait(false);
                        }
                        else
                        {
                            await session!
                                .GetByAutomationId(selectCell.AutomationId)
                                .SelectCellAsync(
                                    selectCell.Row,
                                    selectCell.ColumnKey,
                                    cancellationToken
                                )
                                .ConfigureAwait(false);
                        }

                        break;

                    case SelectRowOperation selectRow:
                        EnsureSession(session);
                        await session!
                            .GetByAutomationId(selectRow.AutomationId)
                            .SelectRowAsync(selectRow.ColumnKey, selectRow.Value, cancellationToken)
                            .ConfigureAwait(false);
                        break;

                    case ClickColumnHeaderOperation clickColumnHeader:
                        EnsureSession(session);
                        await session!
                            .GetByAutomationId(clickColumnHeader.AutomationId)
                            .ClickColumnHeaderAsync(clickColumnHeader.ColumnKey, cancellationToken)
                            .ConfigureAwait(false);
                        break;

                    case AddRowOperation addRow:
                        EnsureSession(session);
                        await session!
                            .GetByAutomationId(addRow.AutomationId)
                            .AddRowAsync(cancellationToken)
                            .ConfigureAwait(false);
                        break;

                    case DeleteSelectedRowsOperation deleteSelectedRows:
                        EnsureSession(session);
                        await session!
                            .GetByAutomationId(deleteSelectedRows.AutomationId)
                            .DeleteSelectedRowsAsync(cancellationToken)
                            .ConfigureAwait(false);
                        break;

                    case ExpectCellTextOperation expectCellText:
                        EnsureSession(session);
                        if (expectCellText.ColumnKey is null)
                        {
                            await session!
                                .GetByAutomationId(expectCellText.AutomationId)
                                .ExpectCellTextAsync(
                                    expectCellText.Row,
                                    expectCellText.Column!.Value,
                                    expectCellText.Text,
                                    cancellationToken
                                )
                                .ConfigureAwait(false);
                        }
                        else
                        {
                            await session!
                                .GetByAutomationId(expectCellText.AutomationId)
                                .ExpectCellTextAsync(
                                    expectCellText.Row,
                                    expectCellText.ColumnKey,
                                    expectCellText.Text,
                                    cancellationToken
                                )
                                .ConfigureAwait(false);
                        }

                        break;

                    case ArmOpenFileOperation armOpenFile:
                        EnsureSession(session);
                        await session!
                            .ArmOpenFileAsync(armOpenFile.Path, cancellationToken)
                            .ConfigureAwait(false);
                        break;

                    case ArmOpenFileCancelOperation:
                        EnsureSession(session);
                        await session!
                            .ArmOpenFileCancelAsync(cancellationToken)
                            .ConfigureAwait(false);
                        break;

                    case ArmSaveFileOperation armSaveFile:
                        EnsureSession(session);
                        await session!
                            .ArmSaveFileAsync(armSaveFile.Path, cancellationToken)
                            .ConfigureAwait(false);
                        break;

                    case ArmSaveFileCancelOperation:
                        EnsureSession(session);
                        await session!
                            .ArmSaveFileCancelAsync(cancellationToken)
                            .ConfigureAwait(false);
                        break;

                    case ArmOpenFolderOperation armOpenFolder:
                        EnsureSession(session);
                        await session!
                            .ArmOpenFolderAsync(armOpenFolder.Path, cancellationToken)
                            .ConfigureAwait(false);
                        break;

                    case ArmOpenFolderCancelOperation:
                        EnsureSession(session);
                        await session!
                            .ArmOpenFolderCancelAsync(cancellationToken)
                            .ConfigureAwait(false);
                        break;

                    case ArmMessageBoxOperation armMessageBox:
                        EnsureSession(session);
                        await session!
                            .ArmMessageBoxAsync(armMessageBox.Result, cancellationToken)
                            .ConfigureAwait(false);
                        break;

                    case ListWindowsOperation:
                        EnsureSession(session);
                        _ = await session!
                            .ListWindowsAsync(cancellationToken)
                            .ConfigureAwait(false);
                        break;

                    case SwitchWindowOperation switchWindow:
                        EnsureSession(session);
                        await session!
                            .SwitchToWindowAsync(switchWindow.WindowId, cancellationToken)
                            .ConfigureAwait(false);
                        break;

                    case WaitForWindowOperation waitForWindow:
                        EnsureSession(session);
                        _ = await session!
                            .WaitForWindowAsync(
                                waitForWindow.Title,
                                waitForWindow.AutomationId,
                                waitForWindow.SwitchTo,
                                cancellationToken
                            )
                            .ConfigureAwait(false);
                        break;

                    case WaitForWindowClosedOperation waitForWindowClosed:
                        EnsureSession(session);
                        await session!
                            .WaitForWindowClosedAsync(
                                waitForWindowClosed.Title,
                                waitForWindowClosed.AutomationId,
                                cancellationToken
                            )
                            .ConfigureAwait(false);
                        break;

                    case InvokeOpeningWindowOperation invokeOpening:
                        EnsureSession(session);
                        _ = await session!
                            .GetByAutomationId(invokeOpening.AutomationId)
                            .InvokeOpeningWindowAsync(
                                invokeOpening.WaitForNewWindow,
                                cancellationToken
                            )
                            .ConfigureAwait(false);
                        break;

                    default:
                        throw new GraftException(
                            GraftErrorCodes.ActionFailed,
                            $"Unsupported Scenario operation '{operation.Action}'."
                        );
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
            throw new GraftException(
                GraftErrorCodes.ActionFailed,
                "Scenario step requires an active session; launch must run first."
            );
        }
    }

    private static LaunchOptions ToLaunchOptions(
        LaunchOperation launch,
        ScenarioRunOptions? options
    )
    {
        var appPath = ResolveAppPath(launch.AppPath, options);
        return new LaunchOptions
        {
            AppPath = appPath,
            Configuration = string.IsNullOrWhiteSpace(launch.Configuration)
                ? "GraftTest"
                : launch.Configuration!,
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
