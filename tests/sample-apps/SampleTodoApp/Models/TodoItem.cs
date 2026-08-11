using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace SampleTodoApp.Models;

public sealed class TodoItem : INotifyPropertyChanged
{
    private bool _isChecked;

    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Status { get; set; } = "未着手";

    public string Priority { get; set; } = "中";

    public bool IsDone { get; set; }

    /// <summary>
    /// UI-only checkbox state (not persisted). Independent from DataGrid row selection.
    /// </summary>
    [JsonIgnore]
    public bool IsChecked
    {
        get => _isChecked;
        set
        {
            if (_isChecked == value)
            {
                return;
            }

            _isChecked = value;
            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
