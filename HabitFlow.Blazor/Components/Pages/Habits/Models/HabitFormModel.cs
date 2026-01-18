using System.ComponentModel.DataAnnotations;
using HabitFlow.Client;

namespace HabitFlow.Blazor.Components.Pages.Habits.Models;

public sealed class HabitFormModel
{
    [Required(ErrorMessage = "Tytuł jest wymagany")]
    [StringLength(80, ErrorMessage = "Tytuł nie może przekraczać 80 znaków")]
    public string Title { get; set; } = string.Empty;

    [StringLength(280, ErrorMessage = "Opis nie może przekraczać 280 znaków")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Typ nawyku jest wymagany")]
    public HabitType Type { get; set; } = HabitType.Start;

    [Required(ErrorMessage = "Tryb uzupełniania jest wymagany")]
    public CompletionMode CompletionMode { get; set; } = CompletionMode.Binary;

    [Required(ErrorMessage = "Musisz wybrać przynajmniej jeden dzień tygodnia")]
    [Range(1, 127, ErrorMessage = "Musisz wybrać przynajmniej jeden dzień tygodnia")]
    public byte DaysOfWeekMask { get; set; } = 127; // All days by default

    [Range(1, 100, ErrorMessage = "Liczba powtórzeń musi być z zakresu 1-100")]
    public int? TargetValue { get; set; }

    [StringLength(20, ErrorMessage = "Jednostka nie może przekraczać 20 znaków")]
    public string? TargetUnit { get; set; }

    public DateOnly? DeadlineDate { get; set; }

    public bool ClearDeadline { get; set; }
    
    public int TargetValueProxy
    {
        get => TargetValue ?? 0;
        set => TargetValue = value;
    }
    
    public DateTime? DeadlineDateTime
    {
        get => DeadlineDate?.ToDateTime(TimeOnly.MinValue);
        set => DeadlineDate = value.HasValue
            ? DateOnly.FromDateTime(value.Value)
            : null;
    }
}
