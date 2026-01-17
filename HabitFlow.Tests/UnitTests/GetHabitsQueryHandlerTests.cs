using HabitFlow.Core.Abstractions.Services;
using HabitFlow.Core.Common;
using HabitFlow.Core.Features.Habits;
using HabitFlow.Core.Services;
using HabitFlow.Data;
using HabitFlow.Data.Entities;
using HabitFlow.Data.Enums;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Xunit;

namespace HabitFlow.Tests.UnitTests;

public class GetHabitsQueryHandlerTests
{
    private readonly HabitFlowDbContext _dbContext;
    private readonly ILoggedUserContext _userContext;

    private readonly Guid _userId = Guid.NewGuid();
    public GetHabitsQueryHandlerTests()
    {
        var options = new DbContextOptionsBuilder<HabitFlowDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _dbContext = new HabitFlowDbContext(options);
        _userContext = Substitute.For<ILoggedUserContext>();
        _userContext.GetUser().Returns(x => new CurrentUser(_userId, "UTC", "user-123@test.pl"));
    }

    [Fact]
    public async Task Handle_ValidQuery_ReturnsPagedHabits()
    {
        // Arrange
        var habit1 = new Habit
        {
            UserId = _userId,
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
            UserId = _userId,
            Title = "Exercise",
            Type = HabitType.Start,
            CompletionMode = CompletionMode.Binary,
            DaysOfWeekMask = 85,
            TargetValue = 1,
            CreatedAtUtc = DateTime.UtcNow.AddDays(-1)
        };

        _dbContext.Habits.AddRange(habit1, habit2);
        await _dbContext.SaveChangesAsync();

        var handler = new GetHabitsQueryHandler(_dbContext, _userContext);
        var query = new GetHabitsQuery(Page: 1, PageSize: 10);

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
        var handler = new GetHabitsQueryHandler(_dbContext, _userContext);
        var query = new GetHabitsQuery(Page: 1, PageSize: 10);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(0, result.Value.TotalCount);
        Assert.Empty(result.Value.Items);
    }

    [Fact]
    public async Task Handle_Pagination_ReturnsCorrectPage()
    {
        // Arrange
        for (int i = 1; i <= 25; i++)
        {
            _dbContext.Habits.Add(new Habit
            {
                UserId = _userId,
                Title = $"Habit {i}",
                Type = HabitType.Start,
                CompletionMode = CompletionMode.Binary,
                DaysOfWeekMask = 127,
                TargetValue = 1,
                CreatedAtUtc = DateTime.UtcNow.AddDays(-i)
            });
        }
        await _dbContext.SaveChangesAsync();

        var handler = new GetHabitsQueryHandler(_dbContext, _userContext);
        var query = new GetHabitsQuery(Page: 2, PageSize: 10);

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
        for (int i = 1; i <= 150; i++)
        {
            _dbContext.Habits.Add(new Habit
            {
                UserId = _userId,
                Title = $"Habit {i}",
                Type = HabitType.Start,
                CompletionMode = CompletionMode.Binary,
                DaysOfWeekMask = 127,
                TargetValue = 1,
                CreatedAtUtc = DateTime.UtcNow
            });
        }
        await _dbContext.SaveChangesAsync();

        var handler = new GetHabitsQueryHandler(_dbContext, _userContext);
        var query = new GetHabitsQuery(Page: 1, PageSize: 200);

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
        _dbContext.Habits.AddRange(
            new Habit { UserId = _userId, Title = "Start habit 1", Type = HabitType.Start, CompletionMode = CompletionMode.Binary, DaysOfWeekMask = 127, TargetValue = 1, CreatedAtUtc = DateTime.UtcNow },
            new Habit { UserId = _userId, Title = "Stop habit 1", Type = HabitType.Stop, CompletionMode = CompletionMode.Binary, DaysOfWeekMask = 127, TargetValue = 1, CreatedAtUtc = DateTime.UtcNow },
            new Habit { UserId = _userId, Title = "Start habit 2", Type = HabitType.Start, CompletionMode = CompletionMode.Binary, DaysOfWeekMask = 127, TargetValue = 1, CreatedAtUtc = DateTime.UtcNow }
        );
        await _dbContext.SaveChangesAsync();

        var handler = new GetHabitsQueryHandler(_dbContext, _userContext);
        var query = new GetHabitsQuery(Type: HabitType.Start);

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
        _dbContext.Habits.AddRange(
            new Habit { UserId = _userId, Title = "Binary habit", Type = HabitType.Start, CompletionMode = CompletionMode.Binary, DaysOfWeekMask = 127, TargetValue = 1, CreatedAtUtc = DateTime.UtcNow },
            new Habit { UserId = _userId, Title = "Quantitative habit", Type = HabitType.Start, CompletionMode = CompletionMode.Quantitative, DaysOfWeekMask = 127, TargetValue = 10, CreatedAtUtc = DateTime.UtcNow }
        );
        await _dbContext.SaveChangesAsync();

        var handler = new GetHabitsQueryHandler(_dbContext, _userContext);
        var query = new GetHabitsQuery(CompletionMode: CompletionMode.Quantitative);

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
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        _dbContext.Habits.AddRange(
            new Habit { UserId = _userId, Title = "Active 1", Type = HabitType.Start, CompletionMode = CompletionMode.Binary, DaysOfWeekMask = 127, TargetValue = 1, DeadlineDate = null, CreatedAtUtc = DateTime.UtcNow },
            new Habit { UserId = _userId, Title = "Active 2", Type = HabitType.Start, CompletionMode = CompletionMode.Binary, DaysOfWeekMask = 127, TargetValue = 1, DeadlineDate = today.AddDays(10), CreatedAtUtc = DateTime.UtcNow },
            new Habit { UserId = _userId, Title = "Expired", Type = HabitType.Start, CompletionMode = CompletionMode.Binary, DaysOfWeekMask = 127, TargetValue = 1, DeadlineDate = today.AddDays(-1), CreatedAtUtc = DateTime.UtcNow }
        );
        await _dbContext.SaveChangesAsync();

        var handler = new GetHabitsQueryHandler(_dbContext, _userContext);
        var query = new GetHabitsQuery(Active: true);

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
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        _dbContext.Habits.AddRange(
            new Habit { UserId = _userId, Title = "Active", Type = HabitType.Start, CompletionMode = CompletionMode.Binary, DaysOfWeekMask = 127, TargetValue = 1, DeadlineDate = null, CreatedAtUtc = DateTime.UtcNow },
            new Habit { UserId = _userId, Title = "Expired 1", Type = HabitType.Start, CompletionMode = CompletionMode.Binary, DaysOfWeekMask = 127, TargetValue = 1, DeadlineDate = today.AddDays(-1), CreatedAtUtc = DateTime.UtcNow },
            new Habit { UserId = _userId, Title = "Expired 2", Type = HabitType.Start, CompletionMode = CompletionMode.Binary, DaysOfWeekMask = 127, TargetValue = 1, DeadlineDate = today.AddDays(-10), CreatedAtUtc = DateTime.UtcNow }
        );
        await _dbContext.SaveChangesAsync();

        var handler = new GetHabitsQueryHandler(_dbContext, _userContext);
        var query = new GetHabitsQuery(Active: false);

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
        _dbContext.Habits.AddRange(
            new Habit { UserId = _userId, Title = "Read books", Type = HabitType.Start, CompletionMode = CompletionMode.Binary, DaysOfWeekMask = 127, TargetValue = 1, CreatedAtUtc = DateTime.UtcNow },
            new Habit { UserId = _userId, Title = "Exercise daily", Type = HabitType.Start, CompletionMode = CompletionMode.Binary, DaysOfWeekMask = 127, TargetValue = 1, CreatedAtUtc = DateTime.UtcNow },
            new Habit { UserId = _userId, Title = "Write journal", Type = HabitType.Start, CompletionMode = CompletionMode.Binary, DaysOfWeekMask = 127, TargetValue = 1, CreatedAtUtc = DateTime.UtcNow }
        );
        await _dbContext.SaveChangesAsync();

        var handler = new GetHabitsQueryHandler(_dbContext, _userContext);
        var query = new GetHabitsQuery(Search: "daily");

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
        _dbContext.Habits.AddRange(
            new Habit { UserId = _userId, Title = "Zebra", Type = HabitType.Start, CompletionMode = CompletionMode.Binary, DaysOfWeekMask = 127, TargetValue = 1, CreatedAtUtc = DateTime.UtcNow },
            new Habit { UserId = _userId, Title = "Apple", Type = HabitType.Start, CompletionMode = CompletionMode.Binary, DaysOfWeekMask = 127, TargetValue = 1, CreatedAtUtc = DateTime.UtcNow },
            new Habit { UserId = _userId, Title = "Mango", Type = HabitType.Start, CompletionMode = CompletionMode.Binary, DaysOfWeekMask = 127, TargetValue = 1, CreatedAtUtc = DateTime.UtcNow }
        );
        await _dbContext.SaveChangesAsync();

        var handler = new GetHabitsQueryHandler(_dbContext, _userContext);
        var query = new GetHabitsQuery(SortField: HabitSortField.Title, SortDirection: SortDirection.Asc);

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
        _dbContext.Habits.AddRange(
            new Habit { UserId = _userId, Title = "Oldest", Type = HabitType.Start, CompletionMode = CompletionMode.Binary, DaysOfWeekMask = 127, TargetValue = 1, CreatedAtUtc = DateTime.UtcNow.AddDays(-10) },
            new Habit { UserId = _userId, Title = "Newest", Type = HabitType.Start, CompletionMode = CompletionMode.Binary, DaysOfWeekMask = 127, TargetValue = 1, CreatedAtUtc = DateTime.UtcNow },
            new Habit { UserId = _userId, Title = "Middle", Type = HabitType.Start, CompletionMode = CompletionMode.Binary, DaysOfWeekMask = 127, TargetValue = 1, CreatedAtUtc = DateTime.UtcNow.AddDays(-5) }
        );
        await _dbContext.SaveChangesAsync();

        var handler = new GetHabitsQueryHandler(_dbContext, _userContext);
        var query = new GetHabitsQuery(SortField: HabitSortField.CreatedAtUtc, SortDirection: SortDirection.Desc);

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
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        _dbContext.Habits.AddRange(
            new Habit { UserId = _userId, Title = "Read Start", Type = HabitType.Start, CompletionMode = CompletionMode.Quantitative, DaysOfWeekMask = 127, TargetValue = 10, DeadlineDate = today.AddDays(10), CreatedAtUtc = DateTime.UtcNow.AddDays(-2) },
            new Habit { UserId = _userId, Title = "Write Start", Type = HabitType.Start, CompletionMode = CompletionMode.Quantitative, DaysOfWeekMask = 127, TargetValue = 5, DeadlineDate = today.AddDays(5), CreatedAtUtc = DateTime.UtcNow.AddDays(-1) },
            new Habit { UserId = _userId, Title = "Exercise Stop", Type = HabitType.Stop, CompletionMode = CompletionMode.Quantitative, DaysOfWeekMask = 127, TargetValue = 3, DeadlineDate = today.AddDays(20), CreatedAtUtc = DateTime.UtcNow }
        );
        await _dbContext.SaveChangesAsync();

        var handler = new GetHabitsQueryHandler(_dbContext, _userContext);
        var query = new GetHabitsQuery(Type: HabitType.Start, CompletionMode: CompletionMode.Quantitative, Active: true, SortField: HabitSortField.Title, SortDirection: SortDirection.Asc);

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
        var user2 = Guid.NewGuid();
        _dbContext.Habits.AddRange(
            new Habit { UserId = _userId, Title = "User 123 Habit", Type = HabitType.Start, CompletionMode = CompletionMode.Binary, DaysOfWeekMask = 127, TargetValue = 1, CreatedAtUtc = DateTime.UtcNow },
            new Habit { UserId = user2, Title = "User 456 Habit", Type = HabitType.Start, CompletionMode = CompletionMode.Binary, DaysOfWeekMask = 127, TargetValue = 1, CreatedAtUtc = DateTime.UtcNow },
            new Habit { UserId = _userId, Title = "Another User 123 Habit", Type = HabitType.Start, CompletionMode = CompletionMode.Binary, DaysOfWeekMask = 127, TargetValue = 1, CreatedAtUtc = DateTime.UtcNow }
        );
        await _dbContext.SaveChangesAsync();

        var handler = new GetHabitsQueryHandler(_dbContext, _userContext);
        var query = new GetHabitsQuery();

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
