using System.Text.Json;
using Graft.Core.Diagnostics;
using Graft.Protocol;

namespace Graft.Core.Tests;

/// <summary>
/// Guards the scope that stores Expect failures and reports them together.
/// </summary>
public sealed class SoftAssertScopeTests
{
    /// <summary>
    /// Two failed checks and one success become one exception with both reports.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - A fresh soft-assert scope
    /// - Two tasks that fault with expect.failed reports
    /// - One completed task
    ///
    /// Steps:
    /// - Check the failing task, the completed task, then the second failing task
    /// - Dispose the scope
    /// - Serialize the aggregate report
    ///
    /// Expected:
    /// - Dispose throws expect.failed with step softAssert and two nested failures
    /// - The success is not included
    /// - The first failure is the inner exception
    /// - A report without failures omits the failures property
    /// </remarks>
    [Fact]
    public async Task Dispose_ThrowsAggregate_WithEachFailureReport()
    {
        var soft = new SoftAssertScope();
        await soft.Check(Task.FromException(Fail("expect.name", "Ready", "Busy")));
        await soft.Check(Task.CompletedTask);
        await soft.Check(Task.FromException<int>(Fail("expect.value", "1", "2")));

        var ex = await Assert.ThrowsAsync<GraftException>(async () => await soft.DisposeAsync());

        Assert.Equal(GraftErrorCodes.ExpectFailed, ex.Code);
        Assert.Equal(2, soft.FailureCount);
        Assert.Contains("Ready", ex.Message, StringComparison.Ordinal);
        Assert.Contains("2", ex.Message, StringComparison.Ordinal);
        Assert.Equal("expect.name", ex.InnerException!.Message);
        var report = ex.Report!;
        Assert.Equal(FailureSteps.SoftAssert, report.Step);
        Assert.Equal("0 failures", report.Expected);
        Assert.Equal("2 failures", report.Actual);
        Assert.True(report.TimedOut);
        Assert.Equal(["expect.name", "expect.value"], report.Failures!.Select(item => item.Step).ToArray());
        Assert.Equal("Busy", report.Failures![0].Actual);

        var decoded = FailureReportJson.Deserialize(FailureReportJson.Serialize(report));
        Assert.Equal(2, decoded.Failures!.Count);

        var single = new FailureReport
        {
            Step = FailureSteps.ExpectName,
            TimedOut = false,
            Selector = new FailureReportSelector { AutomationId = "Status" },
        };
        using var doc = JsonDocument.Parse(FailureReportJson.Serialize(single));
        Assert.False(doc.RootElement.TryGetProperty("failures", out _));

        await soft.DisposeAsync();
    }

    /// <summary>
    /// A scope with only successful checks disposes quietly.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - A fresh soft-assert scope
    ///
    /// Steps:
    /// - Check a completed task inside await using
    ///
    /// Expected:
    /// - Dispose does not throw and the failure count stays zero
    /// </remarks>
    [Fact]
    public async Task Dispose_DoesNotThrow_WhenEveryCheckPasses()
    {
        var soft = new SoftAssertScope();
        await using (soft)
        {
            await soft.Check(Task.CompletedTask);
            Assert.Equal(0, soft.FailureCount);
        }
    }

    /// <summary>
    /// Exceptions other than GraftException leave the scope and are not stored.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - A fresh soft-assert scope
    /// - A task that faults with InvalidOperationException
    ///
    /// Steps:
    /// - Check that task
    /// - Dispose the scope
    /// - Check again after dispose
    ///
    /// Expected:
    /// - Check throws InvalidOperationException and stores nothing
    /// - Dispose does not throw
    /// - A later check throws ObjectDisposedException
    /// </remarks>
    [Fact]
    public async Task Check_PropagatesNonGraftExceptions_AndRejectsUseAfterDispose()
    {
        var soft = new SoftAssertScope();

        await Assert.ThrowsAsync<InvalidOperationException>(() => soft.Check(Task.FromException(new InvalidOperationException("nope"))));

        Assert.Equal(0, soft.FailureCount);
        await soft.DisposeAsync();
        await Assert.ThrowsAsync<ObjectDisposedException>(() => soft.Check(Task.CompletedTask));
    }

    /// <summary>
    /// A GraftException without a report still appears in the aggregate.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - A GraftException that has a code and message but no FailureReport
    ///
    /// Steps:
    /// - Check the faulted task and dispose
    ///
    /// Expected:
    /// - The nested failure uses the exception code as expected and the message as actual
    /// </remarks>
    [Fact]
    public async Task Dispose_FillsFallbackReport_WhenFailureHasNoReport()
    {
        var soft = new SoftAssertScope();
        await soft.Check(Task.FromException(new GraftException(GraftErrorCodes.PipeDisconnected, "pipe closed")));

        var ex = await Assert.ThrowsAsync<GraftException>(async () => await soft.DisposeAsync());

        var nested = Assert.Single(ex.Report!.Failures!);
        Assert.Equal(FailureSteps.SoftAssert, nested.Step);
        Assert.Equal(GraftErrorCodes.PipeDisconnected, nested.Expected);
        Assert.Equal("pipe closed", nested.Actual);
        Assert.False(nested.TimedOut);
    }

    private static GraftException Fail(string step, string expected, string actual) =>
        new(
            GraftErrorCodes.ExpectFailed,
            step,
            new FailureReport
            {
                Step = step,
                Expected = expected,
                Actual = actual,
                TimedOut = true,
                Selector = new FailureReportSelector { AutomationId = "Status" },
            }
        );
}
