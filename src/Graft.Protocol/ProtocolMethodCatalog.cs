using Graft.Protocol.Messages;

namespace Graft.Protocol;

/// <summary>
/// Pairs each <see cref="ProtocolMethods"/> name with the params type on the wire.
/// </summary>
public static class ProtocolMethodCatalog
{
    /// <summary>
    /// Gets the params type for every wire method, keyed by <see cref="ProtocolMethods"/> value.
    /// </summary>
    public static IReadOnlyDictionary<string, Type> ParamTypes { get; } =
        new Dictionary<string, Type>(StringComparer.Ordinal)
        {
            [ProtocolMethods.Handshake] = typeof(HandshakeParams),
            [ProtocolMethods.GetTree] = typeof(GetTreeParams),
            [ProtocolMethods.ListWindows] = typeof(EmptyParams),
            [ProtocolMethods.SwitchWindow] = typeof(WindowIdParams),
            [ProtocolMethods.InvokeOpeningWindow] = typeof(ElementTargetParams),
            [ProtocolMethods.Screenshot] = typeof(ElementTargetParams),
            [ProtocolMethods.Invoke] = typeof(ElementTargetParams),
            [ProtocolMethods.RightClick] = typeof(ElementTargetParams),
            [ProtocolMethods.DoubleClick] = typeof(ElementTargetParams),
            [ProtocolMethods.Hover] = typeof(ElementTargetParams),
            [ProtocolMethods.Drag] = typeof(DragParams),
            [ProtocolMethods.ClickAt] = typeof(ClickAtParams),
            [ProtocolMethods.Wheel] = typeof(WheelParams),
            [ProtocolMethods.SetValue] = typeof(SetValueParams),
            [ProtocolMethods.Toggle] = typeof(ElementTargetParams),
            [ProtocolMethods.SendKeys] = typeof(SendKeysParams),
            [ProtocolMethods.TypeHuman] = typeof(TypeHumanParams),
            [ProtocolMethods.PressKeys] = typeof(PressKeysParams),
            [ProtocolMethods.ScrollIntoView] = typeof(ScrollIntoViewParams),
            [ProtocolMethods.Select] = typeof(SelectParams),
            [ProtocolMethods.SelectMany] = typeof(SelectManyParams),
            [ProtocolMethods.SelectMenu] = typeof(ElementPathParams),
            [ProtocolMethods.SelectTree] = typeof(ElementPathParams),
            [ProtocolMethods.Expand] = typeof(ElementTargetParams),
            [ProtocolMethods.Collapse] = typeof(ElementTargetParams),
            [ProtocolMethods.GetCellText] = typeof(CellParams),
            [ProtocolMethods.SetCellValue] = typeof(CellParams),
            [ProtocolMethods.SelectCell] = typeof(CellParams),
            [ProtocolMethods.SelectRow] = typeof(SelectRowParams),
            [ProtocolMethods.ClickColumnHeader] = typeof(ColumnKeyParams),
            [ProtocolMethods.AddRow] = typeof(ElementTargetParams),
            [ProtocolMethods.DeleteSelectedRows] = typeof(ElementTargetParams),
            [ProtocolMethods.ArmOpenFile] = typeof(DialogPathParams),
            [ProtocolMethods.ArmOpenFileCancel] = typeof(EmptyParams),
            [ProtocolMethods.ArmSaveFile] = typeof(DialogPathParams),
            [ProtocolMethods.ArmSaveFileCancel] = typeof(EmptyParams),
            [ProtocolMethods.ArmOpenFolder] = typeof(DialogPathParams),
            [ProtocolMethods.ArmOpenFolderCancel] = typeof(EmptyParams),
            [ProtocolMethods.ArmMessageBox] = typeof(MessageBoxArmParams),
        };
}
