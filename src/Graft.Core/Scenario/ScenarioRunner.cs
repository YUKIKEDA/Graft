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
                        await session!
                            .GetByAutomationId(select.AutomationId)
                            .SelectAsync(select.Index, cancellationToken)
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

                    case GetCellTextOperation getCellText:
                        EnsureSession(session);
                        _ = await session!
                            .GetByAutomationId(getCellText.AutomationId)
                            .GetCellTextAsync(
                                getCellText.Row,
                                getCellText.Column,
                                cancellationToken
                            )
                            .ConfigureAwait(false);
                        break;

                    case SetCellValueOperation setCellValue:
                        EnsureSession(session);
                        await session!
                            .GetByAutomationId(setCellValue.AutomationId)
                            .SetCellValueAsync(
                                setCellValue.Row,
                                setCellValue.Column,
                                setCellValue.Value,
                                cancellationToken
                            )
                            .ConfigureAwait(false);
                        break;

                    case ExpectCellTextOperation expectCellText:
                        EnsureSession(session);
                        await session!
                            .GetByAutomationId(expectCellText.AutomationId)
                            .ExpectCellTextAsync(
                                expectCellText.Row,
                                expectCellText.Column,
                                expectCellText.Text,
                                cancellationToken
                            )
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

                    case InvokeOpeningWindowOperation invokeOpening:
                        EnsureSession(session);
                        _ = await session!
                            .GetByAutomationId(invokeOpening.AutomationId)
                            .InvokeOpeningWindowAsync(cancellationToken)
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
