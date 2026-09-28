using Graft.Instrumentation.Actions;
using Graft.Instrumentation.Elements;

namespace Graft.TestSupport;

/// <summary>
/// Invoker that records the last automation id and can throw a resolve error.
/// </summary>
internal sealed class FakeElementInvoker : IElementInvoker
{
    private readonly string? _throwCode;

    public FakeElementInvoker(string? throwCode = null) => _throwCode = throwCode;

    public string? LastAutomationId { get; private set; }

    public string? LastName { get; private set; }

    public void Invoke(ElementSelector selector)
    {
        LastAutomationId = selector.AutomationId;
        LastName = selector.Name;
        if (_throwCode is not null)
        {
            throw new ElementResolveException(_throwCode, "fake failure");
        }
    }

    public void BeginInvoke(ElementSelector selector) => Invoke(selector);

    public void RightClick(ElementSelector selector) => Invoke(selector);

    public void DoubleClick(ElementSelector selector) => Invoke(selector);

    public void Hover(ElementSelector selector) => Invoke(selector);

    public void Drag(ElementSelector from, ElementSelector to) => Invoke(from);

    public void ClickAt(ElementSelector selector, double offsetX, double offsetY) => Invoke(selector);

    public void Wheel(ElementSelector selector, int delta) => Invoke(selector);
}
