using HabitFlow.Blazor.Components.Pages.Notifications.Helpers;
using HabitFlow.Blazor.Components.Pages.Notifications.Models;
using HabitFlow.Client;
using Microsoft.AspNetCore.Components;

namespace HabitFlow.Blazor.Components.Pages.Notifications;

public partial class Notifications : IDisposable
{
    private readonly NotificationListState _state = new();
    private CancellationTokenSource? _cts;

    protected override async Task OnInitializedAsync()
    {
        await LoadNotificationsAsync();
    }

    private async Task LoadNotificationsAsync()
    {
        _cts?.Cancel();
        _cts = new CancellationTokenSource();
        var token = _cts.Token;

        _state.IsLoading = true;
        _state.ErrorMessage = null;

        try
        {
            var response = await ApiClient.GetNotificationsAsync(
                page: _state.CurrentPage,
                pageSize: _state.PageSize,
                sortField: _state.SortField,
                sortDirection: _state.SortDirection,
                cancellationToken: token
            );

            if (token.IsCancellationRequested)
                return;

            _state.Items = response.Items.Select(n => n.ToListItemVm()).ToList();
            _state.TotalCount = response.TotalCount;
        }
        catch (ApiException ex) when (ex.StatusCode == 401)
        {
            Navigation.NavigateTo($"/auth/login?returnUrl=/notifications");
        }
        catch (ApiException ex)
        {
            _state.ErrorMessage = $"Błąd podczas ładowania powiadomień: {ex.Message}";
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _state.ErrorMessage = "Wystąpił nieoczekiwany błąd. Spróbuj odświeżyć stronę.";
        }
        finally
        {
            _state.IsLoading = false;
            StateHasChanged();
        }
    }

    private async Task HandlePageChanged(int newPage)
    {
        _state.CurrentPage = newPage;
        await LoadNotificationsAsync();
    }

    private async Task HandlePageSizeChanged(int newPageSize)
    {
        _state.PageSize = newPageSize;
        _state.CurrentPage = 1;
        await LoadNotificationsAsync();
    }

    public void Dispose()
    {
        _cts?.Cancel();
        _cts?.Dispose();
    }
}
