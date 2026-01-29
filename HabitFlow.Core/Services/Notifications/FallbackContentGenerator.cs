using HabitFlow.Core.Abstractions.Notifications;
using HabitFlow.Data.Enums;

namespace HabitFlow.Core.Services.Notifications;

/// <summary>
/// Template-based fallback generator for miss-due notifications.
/// </summary>
public sealed class FallbackContentGenerator : INotificationContentGenerator
{
    private readonly Random _random;
    private readonly IReadOnlyDictionary<TemplateCategory, string[]> _templates;

    public FallbackContentGenerator()
        : this(Random.Shared, DefaultTemplates)
    {
    }

    public FallbackContentGenerator(Random random)
        : this(random, DefaultTemplates)
    {
    }

    public FallbackContentGenerator(Random random, IReadOnlyDictionary<TemplateCategory, string[]> templates)
    {
        _random = random ?? throw new ArgumentNullException(nameof(random));
        _templates = templates ?? throw new ArgumentNullException(nameof(templates));
    }

    public Task<NotificationContentResult> GenerateAsync(
        NotificationContentContext context,
        CancellationToken cancellationToken)
    {
        var habitName = string.IsNullOrWhiteSpace(context.HabitName)
            ? "twoj nawyk"
            : context.HabitName;

        var template = SelectTemplate(context);
        var content = template
            .Replace("{habitName}", habitName, StringComparison.Ordinal)
            .Replace("{streakDays}", Math.Max(0, context.StreakDays).ToString(), StringComparison.Ordinal)
            .Replace("{totalCompletions}", Math.Max(0, context.TotalCompletions).ToString(), StringComparison.Ordinal);

        return Task.FromResult(new NotificationContentResult(
            content,
            AiGenerationStatus.Fallback,
            "AI niedostepne - uzyto szablonu."));
    }

    private string SelectTemplate(NotificationContentContext context)
    {
        var category = ResolveCategory(context);
        if (!_templates.TryGetValue(category, out var templates) || templates.Length == 0)
            templates = DefaultTemplates[TemplateCategory.ShortStreak];

        var index = _random.Next(templates.Length);
        return templates[index];
    }

    private static TemplateCategory ResolveCategory(NotificationContentContext context)
    {
        if (context.StreakDays >= 10)
            return TemplateCategory.LongStreak;

        if (context.StreakDays >= 3)
            return TemplateCategory.MediumStreak;

        return TemplateCategory.ShortStreak;
    }

    public enum TemplateCategory
    {
        ShortStreak,
        MediumStreak,
        LongStreak
    }

    private static readonly IReadOnlyDictionary<TemplateCategory, string[]> DefaultTemplates =
        new Dictionary<TemplateCategory, string[]>
        {
            {
                TemplateCategory.ShortStreak,
                [
                    "Wczoraj nie udalo sie zrobic: '{habitName}'. Jeden dzien nie przekresla postepow. Gotowy sprobowac dzis?",
                    "Zdarza sie pominiety dzien. Zrob dzis '{habitName}' w najprostszej wersji i wracaj do rytmu.",
                    "Wczoraj '{habitName}' sie nie wydarzylo. Zacznij od malego kroku dzis.",
                    "Nie zrobiles wczoraj '{habitName}'? To nowy dzien. Jaki latwy krok mozesz zrobic teraz?",
                    "Zacznij od nowa. '{habitName}' moze byc dzis szybkie, by utrzymac tempo."
                ]
            },
            {
                TemplateCategory.MediumStreak,
                [
                    "Miales {streakDays}-dniowa serie w '{habitName}'. Jedno potkniecie jej nie kasuje. Kontynuuj dzis.",
                    "Swietnie budujesz nawyk '{habitName}'. Pominiety dzien to norma. Wroc do niego teraz.",
                    "Twoja {streakDays}-dniowa seria pokazuje, ze dasz rade z '{habitName}'. Zrob dzis maly krok.",
                    "Postep jest wazniejszy niz perfekcja. Wroc do '{habitName}' dzis i chroń serie.",
                    "Masz za soba {totalCompletions} dni z '{habitName}'. Utrzymaj ten postep dzis."
                ]
            },
            {
                TemplateCategory.LongStreak,
                [
                    "Dluga seria '{habitName}' pokazuje prawdziwe zaangazowanie. Jedno potkniecie to tylko pauza. Dzialaj dzis.",
                    "Zbudowales silna rutyna dla '{habitName}'. Podtrzymaj postep jednym krokiem dzis.",
                    "Seria {streakDays} dni robi wrazenie. Odtworz lancuch z '{habitName}' dzis.",
                    "Zrobiles '{habitName}' {totalCompletions} razy. Ten wysilek ma znaczenie. Kontynuuj dzis.",
                    "Twoja sila nawyku jest widoczna. Zrob '{habitName}' latwo dzis i trzymaj kurs."
                ]
            }
        };
}
