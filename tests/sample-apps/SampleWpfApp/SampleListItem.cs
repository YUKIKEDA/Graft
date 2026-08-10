namespace SampleWpfApp;

/// <summary>
/// Sample list/combo/grid row item. <see cref="Name"/> / <see cref="Active"/> / <see cref="Notes"/>
/// are mutable for DataGrid cell edits.
/// </summary>
public sealed class SampleListItem
{
    /// <summary>
    /// Parameterless ctor for DataGrid <c>AddNew</c> / Activator paths.
    /// </summary>
    public SampleListItem()
        : this($"NewRow-{Guid.NewGuid():N}"[..8], "New", active: false) { }

    public SampleListItem(string automationId, string name, bool active = false, string notes = "")
    {
        AutomationId = automationId;
        Name = name;
        Active = active;
        Notes = notes;
    }

    public string AutomationId { get; set; }

    public string Name { get; set; }

    public bool Active { get; set; }

    public string Notes { get; set; }
}
