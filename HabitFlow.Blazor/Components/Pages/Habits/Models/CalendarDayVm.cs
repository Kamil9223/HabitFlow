using HabitFlow.Client;

namespace HabitFlow.Blazor.Components.Pages.Habits.Models;

public class CalendarDayVm
{
    public DateOnly Date { get; set; }
    public bool IsPlanned { get; set; }
    public int ActualValue { get; set; }
    public int? TargetValueSnapshot { get; set; }
    public CompletionMode? CompletionModeSnapshot { get; set; }
    public HabitType? HabitTypeSnapshot { get; set; }
    public double DailyScore { get; set; }

    public DayStatus Status
    {
        get
        {
            if (!IsPlanned)
                return DayStatus.NotPlanned;

            if (ActualValue == 0)
                return DayStatus.Miss;

            if (DailyScore >= 1.0)
                return DayStatus.Done;

            return DayStatus.Partial;
        }
    }
}

public enum DayStatus
{
    NotPlanned,
    Done,
    Miss,
    Partial
}
