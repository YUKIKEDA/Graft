namespace SampleWpfApp;

/// <summary>
/// Sample list/combo/grid row item. <see cref="Name"/> / <see cref="Active"/> are mutable for DataGrid cell edits.
/// </summary>
public sealed class SampleListItem
{
    public SampleListItem(string automationId, string name, bool active = false)
    {
        AutomationId = automationId;
        Name = name;
        Active = active;
    }

    public string AutomationId { get; }

    public string Name { get; set; }

    public bool Active { get; set; }
}
