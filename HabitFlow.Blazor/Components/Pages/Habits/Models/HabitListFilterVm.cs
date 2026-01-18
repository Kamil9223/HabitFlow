using HabitFlow.Client;

namespace HabitFlow.Blazor.Components.Pages.Habits.Models;

public sealed class HabitListFilterVm
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public HabitType? Type { get; set; }
    public CompletionMode? CompletionMode { get; set; }
    public bool? Active { get; set; }
    public string? Search { get; set; }
    public HabitSortField SortField { get; set; } = HabitSortField.CreatedAtUtc;
    public SortDirection SortDirection { get; set; } = SortDirection.Desc;

    public void Reset()
    {
        Page = 1;
        PageSize = 20;
        Type = null;
        CompletionMode = null;
        Active = null;
        Search = null;
        SortField = HabitSortField.CreatedAtUtc;
        SortDirection = SortDirection.Desc;
    }
}
