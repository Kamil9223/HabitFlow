using HabitFlow.Blazor.Components.Pages.Habits.Helpers;
using HabitFlow.Blazor.Components.Pages.Habits.Models;
using HabitFlow.Client;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using SortDirection = HabitFlow.Client.SortDirection;

namespace HabitFlow.Blazor.Components.Pages.Habits;

public partial class HabitDetails : IDisposable
{
    [Parameter]
    public int Id { get; set; }

    private readonly HabitDetailsState _state = new();
    private List<HabitListItemVm> _allHabits = new();
    private CancellationTokenSource? _cts;

    protected override async Task OnInitializedAsync()
    {
        await LoadAllHabitsForDropdownAsync();
        await LoadHabitDetailsAsync();
    }

    protected override async Task OnParametersSetAsync()
    {
        if (_state.Habit != null && _state.Habit.Id != Id)
        {
            await LoadHabitDetailsAsync();
        }
    }

    private async Task LoadAllHabitsForDropdownAsync()
    {
        try
        {
            var response = await ApiClient.GetHabitsAsync(
                page: 1,
                pageSize: 100,
                type: null,
                completionMode: null,
                active: true,
                search: null,
                sortField: HabitSortField.Title,
                sortDirection: SortDirection.Asc
            );

            _allHabits = response.Items.Select(h => h.ToListItemVm()).ToList();
        }
        catch
        {
            // Fail silently for dropdown - not critical
        }
    }

    private async Task LoadHabitDetailsAsync()
    {
        _cts?.Cancel();
        _cts = new CancellationTokenSource();
        var token = _cts.Token;

        _state.IsLoading = true;
        _state.ErrorMessage = null;

        try
        {
            var habitResponse = await ApiClient.GetHabitAsync(Id, token);

            if (token.IsCancellationRequested)
                return;

            _state.Habit = habitResponse.ToDetailsVm();

            // Load calendar and progress in parallel
            var calendarTask = LoadCalendarAsync(token);
            var progressTask = LoadProgressAsync(token);

            await Task.WhenAll(calendarTask, progressTask);
        }
        catch (ApiException ex) when (ex.StatusCode == 401)
        {
            Navigation.NavigateTo($"/auth/login?returnUrl=/habits/{Id}");
        }
        catch (ApiException ex) when (ex.StatusCode == 404)
        {
            _state.ErrorMessage = "Nawyk nie istnieje lub nie masz do niego dostępu.";
        }
        catch (ApiException ex)
        {
            _state.ErrorMessage = $"Błąd podczas ładowania nawyku: {ex.Message}";
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

    private async Task LoadCalendarAsync(CancellationToken token)
    {
        _state.IsLoadingCalendar = true;
        StateHasChanged();

        try
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            var from = today.AddDays(-30);
            var to = today.AddDays(30);

            var calendarResponse = await ApiClient.GetHabitCalendarAsync(
                id: Id,
                from: new DateTimeOffset(from.ToDateTime(TimeOnly.MinValue)),
                to: new DateTimeOffset(to.ToDateTime(TimeOnly.MinValue)),
                cancellationToken: token
            );

            if (!token.IsCancellationRequested)
            {
                _state.Calendar = calendarResponse.ToCalendarVm();
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Calendar load failure is not critical
            Snackbar.Add("Nie udało się załadować kalendarza", Severity.Warning);
        }
        finally
        {
            _state.IsLoadingCalendar = false;
            StateHasChanged();
        }
    }

    private async Task LoadProgressAsync(CancellationToken token)
    {
        _state.IsLoadingProgress = true;
        StateHasChanged();

        try
        {
            var progressResponse = await ApiClient.GetProgressRollingAsync(
                habitId: Id,
                windowDays: _state.WindowDays,
                until: null,
                cancellationToken: token
            );

            if (!token.IsCancellationRequested)
            {
                _state.Progress = progressResponse.ToProgressVm();
                
                // Update success rate in header
                var latestPoint = progressResponse.Points?.LastOrDefault();
                if (_state.Habit != null && latestPoint != null)
                {
                    _state.Habit.SuccessRate = HabitMappingExtensions.FormatSuccessRate(latestPoint, _state.WindowDays);
                }
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Progress load failure is not critical
            Snackbar.Add("Nie udało się załadować danych postępu", Severity.Warning);
        }
        finally
        {
            _state.IsLoadingProgress = false;
            StateHasChanged();
        }
    }

    private async Task HandleHabitChange(int newHabitId)
    {
        if (newHabitId != Id)
        {
            Navigation.NavigateTo($"/habits/{newHabitId}");
        }
    }

    private async Task HandleWindowChange(int newWindowDays)
    {
        _state.WindowDays = newWindowDays;
        
        _cts?.Cancel();
        _cts = new CancellationTokenSource();
        
        await LoadProgressAsync(_cts.Token);
    }

    public void Dispose()
    {
        _cts?.Cancel();
        _cts?.Dispose();
    }
}
