using HabitFlow.Blazor.Components.Pages.Habits.Models;
using HabitFlow.Client;

namespace HabitFlow.Blazor.Components.Pages.Habits.Helpers;

public static class HabitMappingExtensions
{
    private static readonly string[] DayNames = ["Ndz", "Pn", "Wt", "Śr", "Czw", "Pt", "Sob"];

    public static HabitListItemVm ToListItemVm(this HabitResponse response)
    {
        return new HabitListItemVm
        {
            Id = response.Id,
            Title = response.Title,
            Description = response.Description,
            Type = response.Type,
            CompletionMode = response.CompletionMode,
            DaysOfWeekMask = response.DaysOfWeekMask,
            ScheduleLabel = GetScheduleLabel(response.DaysOfWeekMask),
            TargetValue = response.TargetValue,
            TargetUnit = response.TargetUnit,
            DeadlineDate = response.DeadlineDate is null ? null : DateOnly.FromDateTime(response.DeadlineDate.Value.DateTime),
            CreatedAtUtc = response.CreatedAtUtc,
            SuccessRateShort = null, // Lazy loaded
            IsBusy = false
        };
    }

    public static HabitFormModel ToFormModel(this HabitResponse response)
    {
        return new HabitFormModel
        {
            Title = response.Title,
            Description = response.Description,
            Type = response.Type,
            CompletionMode = response.CompletionMode,
            DaysOfWeekMask = (byte)response.DaysOfWeekMask,
            TargetValue = response.CompletionMode == CompletionMode.Quantitative 
                ? response.TargetValue 
                : null,
            TargetUnit = response.TargetUnit,
            DeadlineDate = response.DeadlineDate is null ? null : DateOnly.FromDateTime(response.DeadlineDate.Value.DateTime),
            ClearDeadline = false
        };
    }

    public static CreateHabitRequest ToCreateRequest(this HabitFormModel model)
    {
        return new CreateHabitRequest
        {
            Title = model.Title.Trim(),
            Description = string.IsNullOrWhiteSpace(model.Description) 
                ? null 
                : model.Description.Trim(),
            Type = model.Type,
            CompletionMode = model.CompletionMode,
            DaysOfWeekMask = model.DaysOfWeekMask,
            TargetValue = model.CompletionMode == CompletionMode.Quantitative 
                ? model.TargetValue ?? 1 
                : 1,
            TargetUnit = model.CompletionMode == CompletionMode.Quantitative 
                ? model.TargetUnit?.Trim() 
                : null,
            DeadlineDate = model.DeadlineDate is null ? null : new DateTimeOffset(model.DeadlineDate.Value.ToDateTime(TimeOnly.MinValue))
        };
    }

    public static UpdateHabitRequest ToUpdateRequest(this HabitFormModel model)
    {
        return new UpdateHabitRequest
        {
            Title = model.Title.Trim(),
            Description = string.IsNullOrWhiteSpace(model.Description) 
                ? null 
                : model.Description.Trim(),
            Type = model.Type,
            CompletionMode = model.CompletionMode,
            DaysOfWeekMask = model.DaysOfWeekMask,
            TargetValue = model.CompletionMode == CompletionMode.Quantitative 
                ? model.TargetValue ?? 1 
                : 1,
            TargetUnit = model.CompletionMode == CompletionMode.Quantitative 
                ? model.TargetUnit?.Trim() 
                : null,
            DeadlineDate = model.DeadlineDate is null ? null : new DateTimeOffset(model.DeadlineDate.Value.ToDateTime(TimeOnly.MinValue)),
            ClearDeadline = model.ClearDeadline
        };
    }

    public static CheckinTargetVm ToCheckinTarget(this HabitListItemVm item)
    {
        return new CheckinTargetVm
        {
            HabitId = item.Id,
            Title = item.Title,
            Type = item.Type,
            CompletionMode = item.CompletionMode,
            TargetValue = item.TargetValue,
            TargetUnit = item.TargetUnit
        };
    }

    public static string GetScheduleLabel(int daysOfWeekMask)
    {
        if (daysOfWeekMask == 0)
            return "-";

        if (daysOfWeekMask == 127) // All days (0b1111111)
            return "Codziennie";

        var selectedDays = new List<string>();
        for (int i = 0; i < 7; i++)
        {
            if ((daysOfWeekMask & (1 << i)) != 0)
            {
                selectedDays.Add(DayNames[i]);
            }
        }

        return string.Join(", ", selectedDays);
    }

    public static string FormatSuccessRate(ProgressRollingPoint? point, int windowDays)
    {
        if (point == null)
            return "-";

        var percentage = $"{Math.Round(point.SuccessRate * 100)}%";
        return $"{percentage} / {windowDays}d";
    }
}
