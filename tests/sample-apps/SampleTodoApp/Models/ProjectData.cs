namespace SampleTodoApp.Models;

public sealed class ProjectData
{
    public List<TodoItem> Items { get; set; } = [];

    public bool IsDarkTheme { get; set; }
}
