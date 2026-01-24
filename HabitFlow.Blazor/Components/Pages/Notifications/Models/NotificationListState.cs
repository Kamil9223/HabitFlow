using HabitFlow.Client;

namespace HabitFlow.Blazor.Components.Pages.Notifications.Models;

/// <summary>
/// Stan widoku listy powiadomień.
/// </summary>
public sealed class NotificationListState
{
    public bool IsLoading { get; set; }
    public string? ErrorMessage { get; set; }
    public List<NotificationListItemVm> Items { get; set; } = [];
    public int TotalCount { get; set; }
    public int CurrentPage { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public NotificationSortField SortField { get; set; } = NotificationSortField.CreatedAtUtc;
    public SortDirection SortDirection { get; set; } = SortDirection.Desc;
}
