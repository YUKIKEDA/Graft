using Graft.Instrumentation.Actions;
using Graft.Instrumentation.Elements;
using Graft.Instrumentation.Screenshot;
using Graft.Instrumentation.Tree;
using Graft.Instrumentation.Windows;
using Graft.Protocol.Messages;
using Graft.TestSupport;

namespace Graft.Instrumentation.Tests;

/// <summary>
/// Registers a no-op service in every agent slot so a params error is not hidden by a missing service.
/// </summary>
internal static class RecordingServices
{
    private static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    internal static void RegisterAll()
    {
        AgentServices.RegisterTreeProvider(new FakeTreeProvider());
        AgentServices.RegisterScreenshotProvider(new ScreenshotFake());
        AgentServices.RegisterElementInvoker(new FakeElementInvoker());
        AgentServices.RegisterElementValueSetter(new FakeElementValueSetter());
        AgentServices.RegisterElementToggler(new TogglerFake());
        AgentServices.RegisterElementKeySender(new KeySenderFake());
        AgentServices.RegisterElementScroller(new ScrollerFake());
        AgentServices.RegisterElementChooser(new ChooserFake());
        AgentServices.RegisterMenuSelector(new MenuFake());
        AgentServices.RegisterTreeSelector(new TreeFake());
        AgentServices.RegisterElementExpander(new ExpanderFake());
        AgentServices.RegisterElementCellAccessor(new CellFake());
        AgentServices.RegisterDataGridOperator(new GridFake());
        AgentServices.RegisterWindowCatalog(new WindowFake());
    }

    private sealed class ScreenshotFake : IScreenshotProvider
    {
        public ScreenshotCapture Capture(ScreenshotOptions options) =>
            new()
            {
                Meta = new ScreenshotResult
                {
                    Format = "png",
                    Width = 1,
                    Height = 1,
                    ByteLength = Png.Length,
                },
                PngBytes = Png,
            };
    }

    private sealed class TogglerFake : IElementToggler
    {
        public void Toggle(ElementSelector selector) { }
    }

    private sealed class KeySenderFake : IElementKeySender
    {
        public void SendKeys(ElementSelector selector, string text) { }

        public void TypeHuman(ElementSelector selector, string text, TimeSpan delay) { }

        public void PressKeys(ElementSelector selector, string keys) { }
    }

    private sealed class ScrollerFake : IElementScroller
    {
        public ElementIdentity ScrollIntoView(ElementSelector selector, int? index) => new() { AutomationId = "scrolled" };
    }

    private sealed class ChooserFake : IElementChooser
    {
        public void Select(ElementSelector selector, int index) { }

        public void Select(ElementSelector selector, string key) { }

        public void SelectMany(ElementSelector selector, IReadOnlyList<int> indexes) { }
    }

    private sealed class MenuFake : IMenuSelector
    {
        public void SelectMenu(ElementSelector selector, string path) { }
    }

    private sealed class TreeFake : ITreeSelector
    {
        public void SelectTree(ElementSelector selector, string path) { }
    }

    private sealed class ExpanderFake : IElementExpander
    {
        public void Expand(ElementSelector selector) { }

        public void Collapse(ElementSelector selector) { }
    }

    private sealed class CellFake : IElementCellAccessor
    {
        public string GetCellText(ElementSelector selector, int row, int? column, string? columnKey) => string.Empty;

        public void SetCellValue(ElementSelector selector, int row, int? column, string? columnKey, string value) { }
    }

    private sealed class GridFake : IDataGridOperator
    {
        public void SelectCell(ElementSelector selector, int row, int? column, string? columnKey) { }

        public void SelectRow(ElementSelector selector, string columnKey, string value) { }

        public void ClickColumnHeader(ElementSelector selector, string columnKey) { }

        public void AddRow(ElementSelector selector) { }

        public void DeleteSelectedRows(ElementSelector selector) { }
    }

    private sealed class WindowFake : IWindowCatalog
    {
        public ListWindowsResult ListWindows() => new() { Windows = Array.Empty<WindowInfo>() };

        public void SwitchWindow(int windowId) { }
    }
}
