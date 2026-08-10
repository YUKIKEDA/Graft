namespace SampleWpfApp;

/// <summary>
/// Sample list/combo/grid row item. <see cref="Name"/> is mutable for DataGrid cell edits.
/// </summary>
public sealed class SampleListItem
{
    public SampleListItem(string automationId, string name)
    {
        AutomationId = automationId;
        Name = name;
    }

    public string AutomationId { get; }

    public string Name { get; set; }
}
