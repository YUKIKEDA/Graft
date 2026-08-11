namespace SampleTodoApp.Models;

public sealed class TodoItem
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Status { get; set; } = "未着手";

    public string Priority { get; set; } = "中";

    public bool IsDone { get; set; }
}
