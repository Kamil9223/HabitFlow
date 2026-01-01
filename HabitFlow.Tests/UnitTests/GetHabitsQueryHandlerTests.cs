using HabitFlow.Core.Abstractions;
using HabitFlow.Core.Common;
using HabitFlow.Core.Features.Habits;
using HabitFlow.Data;
using HabitFlow.Data.Entities;
using HabitFlow.Data.Enums;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HabitFlow.Tests.UnitTests;

public class GetHabitsQueryHandlerTests
{
    private HabitFlowDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<HabitFlowDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new HabitFlowDbContext(options);
    }

    [Fact]
    public async Task Handle_ValidQuery_ReturnsPagedHabits()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        var userId = "user-123";

        var habit1 = new Habit
        {
            UserId = userId,
            Title = "Read books",
            Type = HabitType.Start,
            CompletionMode = CompletionMode.Quantitative,
            DaysOfWeekMask = 127,
            TargetValue = 10,
            TargetUnit = "pages",
            CreatedAtUtc = DateTime.UtcNow.AddDays(-2)
        };
        var habit2 = new Habit
        {
            UserId = userId,
            Title = "Exercise",
            Type = HabitType.Start,
            CompletionMode = CompletionMode.Binary,
            DaysOfWeekMask = 85,
            TargetValue = 1,
            CreatedAtUtc = DateTime.UtcNow.AddDays(-1)
        };

        context.Habits.AddRange(habit1, habit2);
        await context.SaveChangesAsync();

        var handler = new GetHabitsQueryHandler(context);
        var query = new GetHabitsQuery(userId, Page: 1, PageSize: 10);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(2, result.Value.TotalCount);
        Assert.Equal(2, result.Value.Items.Count);
    }

    [Fact]
    public async Task Handle_EmptyDatabase_ReturnsEmptyPagedResult()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        var handler = new GetHabitsQueryHandler(context);
        var query = new GetHabitsQuery("user-123", Page: 1, PageSize: 10);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(0, result.Value.TotalCount);
        Assert.Empty(result.Value.Items);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task Handle_InvalidUserId_ReturnsValidationError(string? invalidUserId)
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        var handler = new GetHabitsQueryHandler(context);
        var query = new GetHabitsQuery(invalidUserId!);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("User.InvalidId", result.Error.Code);
    }

    [Fact]
    public async Task Handle_Pagination_ReturnsCorrectPage()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        var userId = "user-123";

        for (int i = 1; i <= 25; i++)
        {
            context.Habits.Add(new Habit
            {
                UserId = userId,
                Title = $"Habit {i}",
                Type = HabitType.Start,
                CompletionMode = CompletionMode.Binary,
                DaysOfWeekMask = 127,
                TargetValue = 1,
                CreatedAtUtc = DateTime.UtcNow.AddDays(-i)
            });
        }
        await context.SaveChangesAsync();

        var handler = new GetHabitsQueryHandler(context);
        var query = new GetHabitsQuery(userId, Page: 2, PageSize: 10);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(25, result.Value.TotalCount);
        Assert.Equal(10, result.Value.Items.Count);
    }

    [Fact]
    public async Task Handle_PageSizeExceedsMax_ClampsToMaxPageSize()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        var userId = "user-123";

        for (int i = 1; i <= 150; i++)
        {
            context.Habits.Add(new Habit
            {
                UserId = userId,
                Title = $"Habit {i}",
                Type = HabitType.Start,
                CompletionMode = CompletionMode.Binary,
                DaysOfWeekMask = 127,
                TargetValue = 1,
                CreatedAtUtc = DateTime.UtcNow
            });
        }
        await context.SaveChangesAsync();

        var handler = new GetHabitsQueryHandler(context);
        var query = new GetHabitsQuery(userId, Page: 1, PageSize: 200);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(150, result.Value.TotalCount);
        Assert.Equal(100, result.Value.Items.Count); // Clamped to max 100
    }

    [Fact]
    public async Task Handle_FilterByType_ReturnsOnlyMatchingHabits()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        var userId = "user-123";

        context.Habits.AddRange(
            new Habit { UserId = userId, Title = "Start habit 1", Type = HabitType.Start, CompletionMode = CompletionMode.Binary, DaysOfWeekMask = 127, TargetValue = 1, CreatedAtUtc = DateTime.UtcNow },
            new Habit { UserId = userId, Title = "Stop habit 1", Type = HabitType.Stop, CompletionMode = CompletionMode.Binary, DaysOfWeekMask = 127, TargetValue = 1, CreatedAtUtc = DateTime.UtcNow },
            new Habit { UserId = userId, Title = "Start habit 2", Type = HabitType.Start, CompletionMode = CompletionMode.Binary, DaysOfWeekMask = 127, TargetValue = 1, CreatedAtUtc = DateTime.UtcNow }
        );
        await context.SaveChangesAsync();

        var handler = new GetHabitsQueryHandler(context);
        var query = new GetHabitsQuery(userId, Type: HabitType.Start);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(2, result.Value.TotalCount);
        Assert.All(result.Value.Items, h => Assert.Equal(HabitType.Start, h.Type));
    }

    [Fact]
    public async Task Handle_FilterByCompletionMode_ReturnsOnlyMatchingHabits()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        var userId = "user-123";

        context.Habits.AddRange(
            new Habit { UserId = userId, Title = "Binary habit", Type = HabitType.Start, CompletionMode = CompletionMode.Binary, DaysOfWeekMask = 127, TargetValue = 1, CreatedAtUtc = DateTime.UtcNow },
            new Habit { UserId = userId, Title = "Quantitative habit", Type = HabitType.Start, CompletionMode = CompletionMode.Quantitative, DaysOfWeekMask = 127, TargetValue = 10, CreatedAtUtc = DateTime.UtcNow }
        );
        await context.SaveChangesAsync();

        var handler = new GetHabitsQueryHandler(context);
        var query = new GetHabitsQuery(userId, CompletionMode: CompletionMode.Quantitative);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(1, result.Value.TotalCount);
        Assert.Equal(CompletionMode.Quantitative, result.Value.Items[0].CompletionMode);
    }

    [Fact]
    public async Task Handle_FilterByActiveTrue_ReturnsOnlyActiveHabits()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        var userId = "user-123";
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        context.Habits.AddRange(
            new Habit { UserId = userId, Title = "Active 1", Type = HabitType.Start, CompletionMode = CompletionMode.Binary, DaysOfWeekMask = 127, TargetValue = 1, DeadlineDate = null, CreatedAtUtc = DateTime.UtcNow },
            new Habit { UserId = userId, Title = "Active 2", Type = HabitType.Start, CompletionMode = CompletionMode.Binary, DaysOfWeekMask = 127, TargetValue = 1, DeadlineDate = today.AddDays(10), CreatedAtUtc = DateTime.UtcNow },
            new Habit { UserId = userId, Title = "Expired", Type = HabitType.Start, CompletionMode = CompletionMode.Binary, DaysOfWeekMask = 127, TargetValue = 1, DeadlineDate = today.AddDays(-1), CreatedAtUtc = DateTime.UtcNow }
        );
        await context.SaveChangesAsync();

        var handler = new GetHabitsQueryHandler(context);
        var query = new GetHabitsQuery(userId, Active: true);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(2, result.Value.TotalCount);
        Assert.All(result.Value.Items, h =>
            Assert.True(h.DeadlineDate == null || h.DeadlineDate >= today));
    }

    [Fact]
    public async Task Handle_FilterByActiveFalse_ReturnsOnlyInactiveHabits()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        var userId = "user-123";
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        context.Habits.AddRange(
            new Habit { UserId = userId, Title = "Active", Type = HabitType.Start, CompletionMode = CompletionMode.Binary, DaysOfWeekMask = 127, TargetValue = 1, DeadlineDate = null, CreatedAtUtc = DateTime.UtcNow },
            new Habit { UserId = userId, Title = "Expired 1", Type = HabitType.Start, CompletionMode = CompletionMode.Binary, DaysOfWeekMask = 127, TargetValue = 1, DeadlineDate = today.AddDays(-1), CreatedAtUtc = DateTime.UtcNow },
            new Habit { UserId = userId, Title = "Expired 2", Type = HabitType.Start, CompletionMode = CompletionMode.Binary, DaysOfWeekMask = 127, TargetValue = 1, DeadlineDate = today.AddDays(-10), CreatedAtUtc = DateTime.UtcNow }
        );
        await context.SaveChangesAsync();

        var handler = new GetHabitsQueryHandler(context);
        var query = new GetHabitsQuery(userId, Active: false);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(2, result.Value.TotalCount);
        Assert.All(result.Value.Items, h =>
            Assert.True(h.DeadlineDate != null && h.DeadlineDate < today));
    }

    [Fact]
    public async Task Handle_SearchByTitle_ReturnsMatchingHabits()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        var userId = "user-123";

        context.Habits.AddRange(
            new Habit { UserId = userId, Title = "Read books", Type = HabitType.Start, CompletionMode = CompletionMode.Binary, DaysOfWeekMask = 127, TargetValue = 1, CreatedAtUtc = DateTime.UtcNow },
            new Habit { UserId = userId, Title = "Exercise daily", Type = HabitType.Start, CompletionMode = CompletionMode.Binary, DaysOfWeekMask = 127, TargetValue = 1, CreatedAtUtc = DateTime.UtcNow },
            new Habit { UserId = userId, Title = "Write journal", Type = HabitType.Start, CompletionMode = CompletionMode.Binary, DaysOfWeekMask = 127, TargetValue = 1, CreatedAtUtc = DateTime.UtcNow }
        );
        await context.SaveChangesAsync();

        var handler = new GetHabitsQueryHandler(context);
        var query = new GetHabitsQuery(userId, Search: "daily");

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(1, result.Value.TotalCount);
        Assert.Contains("daily", result.Value.Items[0].Title);
    }

    [Fact]
    public async Task Handle_SortByTitleAsc_ReturnsSortedHabits()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        var userId = "user-123";

        context.Habits.AddRange(
            new Habit { UserId = userId, Title = "Zebra", Type = HabitType.Start, CompletionMode = CompletionMode.Binary, DaysOfWeekMask = 127, TargetValue = 1, CreatedAtUtc = DateTime.UtcNow },
            new Habit { UserId = userId, Title = "Apple", Type = HabitType.Start, CompletionMode = CompletionMode.Binary, DaysOfWeekMask = 127, TargetValue = 1, CreatedAtUtc = DateTime.UtcNow },
            new Habit { UserId = userId, Title = "Mango", Type = HabitType.Start, CompletionMode = CompletionMode.Binary, DaysOfWeekMask = 127, TargetValue = 1, CreatedAtUtc = DateTime.UtcNow }
        );
        await context.SaveChangesAsync();

        var handler = new GetHabitsQueryHandler(context);
        var query = new GetHabitsQuery(userId, SortField: HabitSortField.Title, SortDirection: SortDirection.Asc);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(3, result.Value.TotalCount);
        Assert.Equal("Apple", result.Value.Items[0].Title);
        Assert.Equal("Mango", result.Value.Items[1].Title);
        Assert.Equal("Zebra", result.Value.Items[2].Title);
    }

    [Fact]
    public async Task Handle_SortByCreatedAtUtcDesc_ReturnsSortedHabits()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        var userId = "user-123";

        context.Habits.AddRange(
            new Habit { UserId = userId, Title = "Oldest", Type = HabitType.Start, CompletionMode = CompletionMode.Binary, DaysOfWeekMask = 127, TargetValue = 1, CreatedAtUtc = DateTime.UtcNow.AddDays(-10) },
            new Habit { UserId = userId, Title = "Newest", Type = HabitType.Start, CompletionMode = CompletionMode.Binary, DaysOfWeekMask = 127, TargetValue = 1, CreatedAtUtc = DateTime.UtcNow },
            new Habit { UserId = userId, Title = "Middle", Type = HabitType.Start, CompletionMode = CompletionMode.Binary, DaysOfWeekMask = 127, TargetValue = 1, CreatedAtUtc = DateTime.UtcNow.AddDays(-5) }
        );
        await context.SaveChangesAsync();

        var handler = new GetHabitsQueryHandler(context);
        var query = new GetHabitsQuery(userId, SortField: HabitSortField.CreatedAtUtc, SortDirection: SortDirection.Desc);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(3, result.Value.TotalCount);
        Assert.Equal("Newest", result.Value.Items[0].Title);
        Assert.Equal("Middle", result.Value.Items[1].Title);
        Assert.Equal("Oldest", result.Value.Items[2].Title);
    }

    [Fact]
    public async Task Handle_MultipleFiltersAndSorting_ReturnsCorrectResults()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        var userId = "user-123";
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        context.Habits.AddRange(
            new Habit { UserId = userId, Title = "Read Start", Type = HabitType.Start, CompletionMode = CompletionMode.Quantitative, DaysOfWeekMask = 127, TargetValue = 10, DeadlineDate = today.AddDays(10), CreatedAtUtc = DateTime.UtcNow.AddDays(-2) },
            new Habit { UserId = userId, Title = "Write Start", Type = HabitType.Start, CompletionMode = CompletionMode.Quantitative, DaysOfWeekMask = 127, TargetValue = 5, DeadlineDate = today.AddDays(5), CreatedAtUtc = DateTime.UtcNow.AddDays(-1) },
            new Habit { UserId = userId, Title = "Exercise Stop", Type = HabitType.Stop, CompletionMode = CompletionMode.Quantitative, DaysOfWeekMask = 127, TargetValue = 3, DeadlineDate = today.AddDays(20), CreatedAtUtc = DateTime.UtcNow }
        );
        await context.SaveChangesAsync();

        var handler = new GetHabitsQueryHandler(context);
        var query = new GetHabitsQuery(userId, Type: HabitType.Start, CompletionMode: CompletionMode.Quantitative, Active: true, SortField: HabitSortField.Title, SortDirection: SortDirection.Asc);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(2, result.Value.TotalCount);
        Assert.Equal("Read Start", result.Value.Items[0].Title);
        Assert.Equal("Write Start", result.Value.Items[1].Title);
    }

    [Fact]
    public async Task Handle_UserIsolation_ReturnsOnlyCurrentUserHabits()
    {
        // Arrange
        await using var context = CreateInMemoryContext();

        context.Habits.AddRange(
            new Habit { UserId = "user-123", Title = "User 123 Habit", Type = HabitType.Start, CompletionMode = CompletionMode.Binary, DaysOfWeekMask = 127, TargetValue = 1, CreatedAtUtc = DateTime.UtcNow },
            new Habit { UserId = "user-456", Title = "User 456 Habit", Type = HabitType.Start, CompletionMode = CompletionMode.Binary, DaysOfWeekMask = 127, TargetValue = 1, CreatedAtUtc = DateTime.UtcNow },
            new Habit { UserId = "user-123", Title = "Another User 123 Habit", Type = HabitType.Start, CompletionMode = CompletionMode.Binary, DaysOfWeekMask = 127, TargetValue = 1, CreatedAtUtc = DateTime.UtcNow }
        );
        await context.SaveChangesAsync();

        var handler = new GetHabitsQueryHandler(context);
        var query = new GetHabitsQuery("user-123");

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(2, result.Value.TotalCount);
        Assert.Contains(result.Value.Items, h => h.Title == "User 123 Habit");
        Assert.Contains(result.Value.Items, h => h.Title == "Another User 123 Habit");
        Assert.DoesNotContain(result.Value.Items, h => h.Title == "User 456 Habit");
    }
}
