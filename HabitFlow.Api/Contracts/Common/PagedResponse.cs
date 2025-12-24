namespace HabitFlow.Api.Contracts.Common;

public record PagedResponse<T>(
    int TotalCount,
    IReadOnlyList<T> Items
);
