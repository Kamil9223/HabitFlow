using HabitFlow.Core.Features.Progress;
using Xunit;

namespace HabitFlow.Tests.UnitTests;

public class GetProgressRollingValidatorTests
{
    [Fact]
    public void Validate_ValidQuery_ReturnsNoErrors()
    {
        // Arrange
        var query = new GetProgressRollingQuery(
            HabitId: 1,
            WindowDays: 7,
            Until: null);

        // Act
        var errors = GetProgressRollingValidator.Validate(query);

        // Assert
        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_ValidQueryWith30DayWindow_ReturnsNoErrors()
    {
        // Arrange
        var query = new GetProgressRollingQuery(
            HabitId: 1,
            WindowDays: 30,
            Until: new DateOnly(2025, 12, 7));

        // Act
        var errors = GetProgressRollingValidator.Validate(query);

        // Assert
        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_HabitIdZero_ReturnsValidationError()
    {
        // Arrange
        var query = new GetProgressRollingQuery(
            HabitId: 0,
            WindowDays: 7,
            Until: null);

        // Act
        var errors = GetProgressRollingValidator.Validate(query);

        // Assert
        Assert.Single(errors);
        Assert.Equal("HabitId", errors[0].Code);
        Assert.Contains("greater than 0", errors[0].Description);
    }

    [Fact]
    public void Validate_HabitIdNegative_ReturnsValidationError()
    {
        // Arrange
        var query = new GetProgressRollingQuery(
            HabitId: -1,
            WindowDays: 7,
            Until: null);

        // Act
        var errors = GetProgressRollingValidator.Validate(query);

        // Assert
        Assert.Single(errors);
        Assert.Equal("HabitId", errors[0].Code);
    }

    [Fact]
    public void Validate_WindowDays15_ReturnsValidationError()
    {
        // Arrange
        var query = new GetProgressRollingQuery(
            HabitId: 1,
            WindowDays: 15,
            Until: null);

        // Act
        var errors = GetProgressRollingValidator.Validate(query);

        // Assert
        Assert.Single(errors);
        Assert.Equal("WindowDays", errors[0].Code);
        Assert.Contains("7 or 30", errors[0].Description);
    }

    [Fact]
    public void Validate_WindowDaysZero_ReturnsValidationError()
    {
        // Arrange
        var query = new GetProgressRollingQuery(
            HabitId: 1,
            WindowDays: 0,
            Until: null);

        // Act
        var errors = GetProgressRollingValidator.Validate(query);

        // Assert
        Assert.Single(errors);
        Assert.Equal("WindowDays", errors[0].Code);
    }

    [Fact]
    public void Validate_WindowDaysNegative_ReturnsValidationError()
    {
        // Arrange
        var query = new GetProgressRollingQuery(
            HabitId: 1,
            WindowDays: -7,
            Until: null);

        // Act
        var errors = GetProgressRollingValidator.Validate(query);

        // Assert
        Assert.Single(errors);
        Assert.Equal("WindowDays", errors[0].Code);
    }

    [Fact]
    public void Validate_WindowDays60_ReturnsValidationError()
    {
        // Arrange
        var query = new GetProgressRollingQuery(
            HabitId: 1,
            WindowDays: 60,
            Until: null);

        // Act
        var errors = GetProgressRollingValidator.Validate(query);

        // Assert
        Assert.Single(errors);
        Assert.Equal("WindowDays", errors[0].Code);
    }

    [Fact]
    public void Validate_MultipleErrors_ReturnsAllErrors()
    {
        // Arrange
        var query = new GetProgressRollingQuery(
            HabitId: 0,
            WindowDays: 15,
            Until: null);

        // Act
        var errors = GetProgressRollingValidator.Validate(query);

        // Assert
        Assert.Equal(2, errors.Count);
        Assert.Contains(errors, e => e.Code == "HabitId");
        Assert.Contains(errors, e => e.Code == "WindowDays");
    }
}
