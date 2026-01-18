namespace HabitFlow.Blazor.Components.Pages.Habits.Models;

public sealed class HabitListState
{
    public bool IsLoading { get; set; }
    public string? ErrorMessage { get; set; }
    public List<HabitListItemVm> Items { get; set; } = [];
    public int TotalCountFiltered { get; set; }
    public int TotalCountAll { get; set; }
    public HabitListFilterVm Filters { get; set; } = new();
}
