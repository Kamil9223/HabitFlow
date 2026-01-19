using HabitFlow.Blazor.Components.Pages.Habits.Helpers;
using HabitFlow.Blazor.Components.Pages.Habits.Models;
using HabitFlow.Blazor.Components.Shared;
using HabitFlow.Client;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace HabitFlow.Blazor.Components.Pages.Habits;

public partial class Habits : IDisposable
{
    private const int MaxHabits = 20;
    private readonly HabitListState _state = new();
    private CancellationTokenSource? _cts;
    private readonly Dictionary<int, string?> _successRateCache = new();

    protected override async Task OnInitializedAsync()
    {
        await LoadHabitsAsync();
    }

    private async Task LoadHabitsAsync()
    {
        _cts?.Cancel();
        _cts = new CancellationTokenSource();
        var token = _cts.Token;

        _state.IsLoading = true;
        _state.ErrorMessage = null;

        try
        {
            var response = await ApiClient.GetHabitsAsync(
                page: _state.Filters.Page,
                pageSize: _state.Filters.PageSize,
                type: _state.Filters.Type,
                completionMode: _state.Filters.CompletionMode,
                active: _state.Filters.Active,
                search: _state.Filters.Search,
                sortField: _state.Filters.SortField,
                sortDirection: _state.Filters.SortDirection,
                cancellationToken: token
            );

            if (token.IsCancellationRequested)
                return;

            _state.Items = response.Items.Select(h => h.ToListItemVm()).ToList();
            _state.TotalCountFiltered = response.TotalCount;
            _state.TotalCountAll = response.TotalCount;

            _ = LoadSuccessRatesAsync(token);
        }
        catch (ApiException ex) when (ex.StatusCode == 401)
        {
            Navigation.NavigateTo($"/auth/login?returnUrl=/habits");
        }
        catch (ApiException ex)
        {
            _state.ErrorMessage = $"Błąd podczas ładowania nawyków: {ex.Message}";
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

    private async Task LoadSuccessRatesAsync(CancellationToken token)
    {
        foreach (var item in _state.Items.Where(i => !_successRateCache.ContainsKey(i.Id)))
        {
            if (token.IsCancellationRequested)
                return;

            try
            {
                var progressResponse = await ApiClient.GetProgressRollingAsync(
                    habitId: item.Id,
                    windowDays: 7,
                    until: null,
                    cancellationToken: token
                );

                var latestPoint = progressResponse.Points?.LastOrDefault();
                var formattedRate = HabitMappingExtensions.FormatSuccessRate(latestPoint, 7);

                _successRateCache[item.Id] = formattedRate;
                item.SuccessRateShort = formattedRate;

                StateHasChanged();
            }
            catch
            {
                _successRateCache[item.Id] = "-";
                item.SuccessRateShort = "-";
            }
        }
    }

    private async Task HandleFiltersChanged(HabitListFilterVm filters)
    {
        _state.Filters = filters;
        await LoadHabitsAsync();
    }

    private async Task HandlePageChanged(int newPage)
    {
        _state.Filters.Page = newPage;
        await LoadHabitsAsync();
    }

    private async Task HandlePageSizeChanged(int newPageSize)
    {
        _state.Filters.PageSize = newPageSize;
        _state.Filters.Page = 1;
        await LoadHabitsAsync();
    }

    private async Task HandleCreateHabit()
    {
        var model = new HabitFormModel();
        var parameters = new DialogParameters<HabitFormDialog>
        {
            { x => x.Mode, HabitFormDialog.FormMode.Create },
            { x => x.Model, model },
            { x => x.OnSubmit, EventCallback.Factory.Create<HabitFormModel>(this, SaveNewHabitAsync) }
        };

        var options = new DialogOptions
        {
            CloseButton = true,
            MaxWidth = MaxWidth.Medium,
            FullWidth = true
        };

        await DialogService.ShowAsync<HabitFormDialog>("Nowy nawyk", parameters, options);
    }

    private async Task SaveNewHabitAsync(HabitFormModel model)
    {
        try
        {
            var request = model.ToCreateRequest();
            await ApiClient.CreateHabitAsync(request);

            Snackbar.Add("Nawyk został utworzony", Severity.Success);
            await LoadHabitsAsync();
        }
        catch (ApiException ex) when (ex.StatusCode == 409)
        {
            Snackbar.Add("Osiągnięto limit 20 nawyków", Severity.Error);
        }
        catch (ApiException ex) when (ex.StatusCode == 400)
        {
            Snackbar.Add($"Błąd walidacji: {ex.Message}", Severity.Error);
            throw;
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Błąd podczas tworzenia nawyku: {ex.Message}", Severity.Error);
            throw;
        }
    }

    private async Task HandleEditHabit(HabitListItemVm item)
    {
        item.IsBusy = true;
        StateHasChanged();

        try
        {
            var habitResponse = await ApiClient.GetHabitAsync(item.Id);
            var model = habitResponse.ToFormModel();

            var parameters = new DialogParameters<HabitFormDialog>
            {
                { x => x.Mode, HabitFormDialog.FormMode.Edit },
                { x => x.Model, model },
                { x => x.OnSubmit, EventCallback.Factory.Create<HabitFormModel>(
                    this, async (m) => await SaveEditedHabitAsync(item.Id, m)) }
            };

            var options = new DialogOptions
            {
                CloseButton = true,
                MaxWidth = MaxWidth.Medium,
                FullWidth = true
            };

            await DialogService.ShowAsync<HabitFormDialog>("Edytuj nawyk", parameters, options);
        }
        catch (ApiException ex) when (ex.StatusCode == 404)
        {
            Snackbar.Add("Nawyk nie istnieje", Severity.Error);
            await LoadHabitsAsync();
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Błąd podczas ładowania nawyku: {ex.Message}", Severity.Error);
        }
        finally
        {
            item.IsBusy = false;
            StateHasChanged();
        }
    }

    private async Task SaveEditedHabitAsync(int habitId, HabitFormModel model)
    {
        try
        {
            var request = model.ToUpdateRequest();
            await ApiClient.UpdateHabitAsync(habitId, request);

            Snackbar.Add("Nawyk został zaktualizowany", Severity.Success);
            await LoadHabitsAsync();
        }
        catch (ApiException ex) when (ex.StatusCode == 404)
        {
            Snackbar.Add("Nawyk nie istnieje", Severity.Error);
            await LoadHabitsAsync();
        }
        catch (ApiException ex) when (ex.StatusCode == 400)
        {
            Snackbar.Add($"Błąd walidacji: {ex.Message}", Severity.Error);
            throw;
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Błąd podczas aktualizacji nawyku: {ex.Message}", Severity.Error);
            throw;
        }
    }

    private async Task HandleDeleteHabit(HabitListItemVm item)
    {
        var options = new ConfirmDialogOptions
        {
            Title = "Usuń nawyk",
            Message = $"Czy na pewno chcesz usunąć nawyk \"{item.Title}\"? Ta operacja jest nieodwracalna i usunie wszystkie powiązane check-iny.",
            ConfirmButtonText = "Usuń",
            CancelButtonText = "Anuluj"
        };

        var parameters = new DialogParameters<ConfirmDialog>
        {
            { x => x.Options, options },
            { x => x.OnConfirm, EventCallback.Factory.Create(this, async () => await DeleteHabitAsync(item)) }
        };

        var dialogOptions = new DialogOptions
        {
            CloseButton = true,
            MaxWidth = MaxWidth.Small
        };

        await DialogService.ShowAsync<ConfirmDialog>("Potwierdź usunięcie", parameters, dialogOptions);
    }

    private async Task DeleteHabitAsync(HabitListItemVm item)
    {
        item.IsBusy = true;
        StateHasChanged();

        try
        {
            await ApiClient.DeleteHabitAsync(item.Id);
            Snackbar.Add("Nawyk został usunięty", Severity.Success);
            _successRateCache.Remove(item.Id);
            await LoadHabitsAsync();
        }
        catch (ApiException ex) when (ex.StatusCode == 404)
        {
            Snackbar.Add("Nawyk nie istnieje", Severity.Error);
            await LoadHabitsAsync();
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Błąd podczas usuwania nawyku: {ex.Message}", Severity.Error);
            item.IsBusy = false;
            StateHasChanged();
        }
    }

    private async Task HandleCheckinHabit(HabitListItemVm item)
    {
        var target = item.ToCheckinTarget();
        var localDate = DateOnly.FromDateTime(DateTime.Today);

        var parameters = new DialogParameters<CheckinDialog>
        {
            { x => x.HabitId, target.HabitId },
            { x => x.Title, target.Title },
            { x => x.CompletionMode, target.CompletionMode },
            { x => x.TargetValue, target.TargetValue },
            { x => x.TargetUnit, target.TargetUnit },
            { x => x.LocalDate, localDate },
            { x => x.OnSubmit, EventCallback.Factory.Create<CheckinDialogResult>(
                this, async (result) => await SaveCheckinAsync(item, result)) }
        };

        var options = new DialogOptions
        {
            CloseButton = true,
            MaxWidth = MaxWidth.Small
        };

        await DialogService.ShowAsync<CheckinDialog>("Check-in", parameters, options);
    }

    private async Task SaveCheckinAsync(HabitListItemVm item, CheckinDialogResult result)
    {
        item.IsBusy = true;
        StateHasChanged();

        try
        {
            var request = new CreateCheckinRequest
            {
                LocalDate = result.SelectedDate.ToDateTime(TimeOnly.MinValue),
                ActualValue = result.ActualValue
            };

            await ApiClient.CreateCheckinAsync(result.HabitId, request);
            Snackbar.Add("Check-in został zapisany", Severity.Success);

            _successRateCache.Remove(item.Id);
            await LoadHabitsAsync();
        }
        catch (ApiException ex) when (ex.StatusCode == 409)
        {
            Snackbar.Add("Check-in dla tego nawyku i daty już istnieje", Severity.Warning);
        }
        catch (ApiException ex) when (ex.StatusCode == 422)
        {
            Snackbar.Add("Check-in poza zakresem lub nawyk nie jest zaplanowany na ten dzień", Severity.Error);
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Błąd podczas zapisywania check-in: {ex.Message}", Severity.Error);
        }
        finally
        {
            item.IsBusy = false;
            StateHasChanged();
        }
    }

    public void Dispose()
    {
        _cts?.Cancel();
        _cts?.Dispose();
    }
}
