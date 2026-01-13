using HabitFlow.Core.Features.Checkins;
using Xunit;

namespace HabitFlow.Tests.UnitTests;

public class CreateCheckinCommandValidatorTests
{
    [Fact]
    public void Validate_ValidCommand_ReturnsNoErrors()
    {
        // Arrange
        var command = new CreateCheckinCommand(
            HabitId: 1,
            LocalDate: DateOnly.FromDateTime(DateTime.UtcNow),
            ActualValue: 5);

        // Act
        var errors = CreateCheckinCommandValidator.Validate(command);

        // Assert
        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_HabitIdZero_ReturnsValidationError()
    {
        // Arrange
        var command = new CreateCheckinCommand(
            HabitId: 0,
            LocalDate: DateOnly.FromDateTime(DateTime.UtcNow),
            ActualValue: 5);

        // Act
        var errors = CreateCheckinCommandValidator.Validate(command);

        // Assert
        Assert.Single(errors);
        Assert.Equal("HabitId", errors[0].Code);
        Assert.Contains("greater than 0", errors[0].Description);
    }

    [Fact]
    public void Validate_HabitIdNegative_ReturnsValidationError()
    {
        // Arrange
        var command = new CreateCheckinCommand(
            HabitId: -1,
            LocalDate: DateOnly.FromDateTime(DateTime.UtcNow),
            ActualValue: 5);

        // Act
        var errors = CreateCheckinCommandValidator.Validate(command);

        // Assert
        Assert.Single(errors);
        Assert.Equal("HabitId", errors[0].Code);
    }

    [Fact]
    public void Validate_ActualValueNegative_ReturnsValidationError()
    {
        // Arrange
        var command = new CreateCheckinCommand(
            HabitId: 1,
            LocalDate: DateOnly.FromDateTime(DateTime.UtcNow),
            ActualValue: -5);

        // Act
        var errors = CreateCheckinCommandValidator.Validate(command);

        // Assert
        Assert.Single(errors);
        Assert.Equal("ActualValue", errors[0].Code);
        Assert.Contains("non-negative", errors[0].Description);
    }

    [Fact]
    public void Validate_ActualValueZero_IsValid()
    {
        // Arrange
        var command = new CreateCheckinCommand(
            HabitId: 1,
            LocalDate: DateOnly.FromDateTime(DateTime.UtcNow),
            ActualValue: 0);

        // Act
        var errors = CreateCheckinCommandValidator.Validate(command);

        // Assert
        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_LocalDateInFuture_ReturnsValidationError()
    {
        // Arrange
        var command = new CreateCheckinCommand(
            HabitId: 1,
            LocalDate: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)),
            ActualValue: 5);

        // Act
        var errors = CreateCheckinCommandValidator.Validate(command);

        // Assert
        Assert.Single(errors);
        Assert.Equal("LocalDate", errors[0].Code);
        Assert.Contains("future", errors[0].Description);
    }

    [Fact]
    public void Validate_LocalDateExactly7DaysBack_IsValid()
    {
        // Arrange
        var command = new CreateCheckinCommand(
            HabitId: 1,
            LocalDate: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-7)),
            ActualValue: 5);

        // Act
        var errors = CreateCheckinCommandValidator.Validate(command);

        // Assert
        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_LocalDateMoreThan7DaysBack_ReturnsValidationError()
    {
        // Arrange
        var command = new CreateCheckinCommand(
            HabitId: 1,
            LocalDate: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-8)),
            ActualValue: 5);

        // Act
        var errors = CreateCheckinCommandValidator.Validate(command);

        // Assert
        Assert.Single(errors);
        Assert.Equal("LocalDate", errors[0].Code);
        Assert.Contains("7 days", errors[0].Description);
    }

    [Fact]
    public void Validate_LocalDateToday_IsValid()
    {
        // Arrange
        var command = new CreateCheckinCommand(
            HabitId: 1,
            LocalDate: DateOnly.FromDateTime(DateTime.UtcNow),
            ActualValue: 5);

        // Act
        var errors = CreateCheckinCommandValidator.Validate(command);

        // Assert
        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_MultipleErrors_ReturnsAllErrors()
    {
        // Arrange
        var command = new CreateCheckinCommand(
            HabitId: 0,
            LocalDate: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)),
            ActualValue: -5);

        // Act
        var errors = CreateCheckinCommandValidator.Validate(command);

        // Assert
        Assert.Equal(3, errors.Count);
        Assert.Contains(errors, e => e.Code == "HabitId");
        Assert.Contains(errors, e => e.Code == "ActualValue");
        Assert.Contains(errors, e => e.Code == "LocalDate");
    }

    [Fact]
    public void Validate_LocalDate1DayBack_IsValid()
    {
        // Arrange
        var command = new CreateCheckinCommand(
            HabitId: 1,
            LocalDate: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)),
            ActualValue: 5);

        // Act
        var errors = CreateCheckinCommandValidator.Validate(command);

        // Assert
        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_LargeActualValue_IsValid()
    {
        // Arrange (clamping is done in handler, not validator)
        var command = new CreateCheckinCommand(
            HabitId: 1,
            LocalDate: DateOnly.FromDateTime(DateTime.UtcNow),
            ActualValue: 1000);

        // Act
        var errors = CreateCheckinCommandValidator.Validate(command);

        // Assert
        Assert.Empty(errors);
    }
}
