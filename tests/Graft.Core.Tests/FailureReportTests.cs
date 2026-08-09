using System.Text.Json;
using Graft.Core.Diagnostics;
using Graft.Core.Selectors;

namespace Graft.Core.Tests;

public sealed class FailureReportTests
{
    /// <summary>
    /// FailureReport JSON round-trips the Phase 2 minimum fields.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Report with step expectName, expected/actual, timedOut true, selector automationId
    ///
    /// Steps:
    /// - Serialize then Deserialize via FailureReportJson
    ///
    /// Expected:
    /// - All minimum fields match the original
    /// </remarks>
    [Fact]
    public void Serialize_ThenDeserialize_PreservesMinimumFields()
    {
        var original = new FailureReport
        {
            Step = FailureSteps.ExpectName,
            Expected = "Clicked 1",
            Actual = "Ready",
            TimedOut = true,
            Selector = new FailureReportSelector { AutomationId = "StatusText" },
        };

        var json = FailureReportJson.Serialize(original);
        var decoded = FailureReportJson.Deserialize(json);

        Assert.Equal(FailureSteps.ExpectName, decoded.Step);
        Assert.Equal("Clicked 1", decoded.Expected);
        Assert.Equal("Ready", decoded.Actual);
        Assert.True(decoded.TimedOut);
        Assert.Equal("StatusText", decoded.Selector.AutomationId);
        Assert.Null(decoded.Selector.Name);
    }

    /// <summary>
    /// Null optional fields are omitted from JSON and FromSelector copies criteria.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Report with only step, timedOut false, selector from Selector.ByAutomationId
    ///
    /// Steps:
    /// - Serialize; parse as JsonDocument; FromSelector on a composite Selector
    ///
    /// Expected:
    /// - expected/actual keys absent; FromSelector mirrors AutomationId and Name
    /// </remarks>
    [Fact]
    public void Serialize_OmitsNullOptionals_AndFromSelectorCopiesCriteria()
    {
        var report = new FailureReport
        {
            Step = FailureSteps.Invoke,
            TimedOut = false,
            Selector = FailureReportSelector.FromSelector(Selector.ByAutomationId("SampleButton")),
        };

        using var doc = JsonDocument.Parse(FailureReportJson.Serialize(report));
        var root = doc.RootElement;
        Assert.Equal("invoke", root.GetProperty("step").GetString());
        Assert.False(root.GetProperty("timedOut").GetBoolean());
        Assert.False(root.TryGetProperty("expected", out _));
        Assert.False(root.TryGetProperty("actual", out _));
        Assert.Equal(
            "SampleButton",
            root.GetProperty("selector").GetProperty("automationId").GetString()
        );

        var fromComposite = FailureReportSelector.FromSelector(
            new Selector
            {
                AutomationId = "Box",
                Name = "Hello",
                ControlType = "TextBox",
            }
        );
        Assert.Equal("Box", fromComposite.AutomationId);
        Assert.Equal("Hello", fromComposite.Name);
        Assert.Equal("TextBox", fromComposite.ControlType);
    }

    /// <summary>
    /// HealingCandidates round-trip through FailureReport JSON.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Report with one healing candidate (score, selector, reason)
    ///
    /// Steps:
    /// - Serialize then Deserialize via FailureReportJson
    ///
    /// Expected:
    /// - healingCandidates preserved (score / automationId / reason)
    /// </remarks>
    [Fact]
    public void Serialize_ThenDeserialize_PreservesHealingCandidates()
    {
        var original = new FailureReport
        {
            Step = FailureSteps.Wait,
            TimedOut = true,
            Selector = new FailureReportSelector { AutomationId = "Gone" },
            HealingCandidates =
            [
                new HealingCandidate
                {
                    Score = 75,
                    Selector = new FailureReportSelector
                    {
                        Name = "Click Me",
                        ControlType = "Button",
                        NearAutomationId = "Main",
                    },
                    Reason = "stableIdentity",
                },
            ],
        };

        var decoded = FailureReportJson.Deserialize(FailureReportJson.Serialize(original));
        Assert.NotNull(decoded.HealingCandidates);
        Assert.Single(decoded.HealingCandidates);
        Assert.Equal(75, decoded.HealingCandidates[0].Score);
        Assert.Equal("Click Me", decoded.HealingCandidates[0].Selector.Name);
        Assert.Equal("Button", decoded.HealingCandidates[0].Selector.ControlType);
        Assert.Equal("Main", decoded.HealingCandidates[0].Selector.NearAutomationId);
        Assert.Equal("stableIdentity", decoded.HealingCandidates[0].Reason);
    }
}
