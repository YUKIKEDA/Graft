using Graft.Instrumentation.Actions;
using Graft.Instrumentation.Elements;

namespace Graft.TestSupport;

/// <summary>
/// Value setter that records the last automation id and value.
/// </summary>
internal sealed class FakeElementValueSetter : IElementValueSetter
{
    public string? LastAutomationId { get; private set; }

    public string? LastValue { get; private set; }

    public void SetValue(ElementSelector selector, string value)
    {
        LastAutomationId = selector.AutomationId;
        LastValue = value;
    }
}
