using HabitFlow.Core.Features.Auth;
using Xunit;

namespace HabitFlow.Tests.UnitTests;

public class RegisterValidatorTests
{
    [Fact]
    public void Validate_ValidCommand_ReturnsNoErrors()
    {
        // Arrange
        var command = new RegisterCommand(
            Email: "user@example.com",
            Password: "Password123",
            DisplayName: "John Doe");

        // Act
        var errors = RegisterValidator.Validate(command);

        // Assert
        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_ValidCommandWithoutDisplayName_ReturnsNoErrors()
    {
        // Arrange
        var command = new RegisterCommand(
            Email: "user@example.com",
            Password: "Password123",
            DisplayName: null);

        // Act
        var errors = RegisterValidator.Validate(command);

        // Assert
        Assert.Empty(errors);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_EmptyEmail_ReturnsEmailRequiredError(string? email)
    {
        // Arrange
        var command = new RegisterCommand(
            Email: email!,
            Password: "Password123",
            DisplayName: null);

        // Act
        var errors = RegisterValidator.Validate(command);

        // Assert
        Assert.Single(errors);
        Assert.Equal("User.EmailRequired", errors[0].Code);
    }

    [Theory]
    [InlineData("notanemail")]
    [InlineData("@example.com")]
    [InlineData("user@")]
    [InlineData("user@.com")]
    [InlineData("user @example.com")]
    [InlineData("user@example")]
    public void Validate_InvalidEmailFormat_ReturnsEmailInvalidFormatError(string email)
    {
        // Arrange
        var command = new RegisterCommand(
            Email: email,
            Password: "Password123",
            DisplayName: null);

        // Act
        var errors = RegisterValidator.Validate(command);

        // Assert
        Assert.Contains(errors, e => e.Code == "User.EmailInvalidFormat");
    }

    [Fact]
    public void Validate_EmailTooLong_ReturnsEmailTooLongError()
    {
        // Arrange
        var command = new RegisterCommand(
            Email: new string('a', 247) + "@test.com", // 256 chars total
            Password: "Password123",
            DisplayName: null);

        // Act
        var errors = RegisterValidator.Validate(command);

        // Assert
        Assert.Contains(errors, e => e.Code == "User.EmailTooLong");
        // Note: May also contain EmailInvalidFormat due to extremely long local part
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_EmptyPassword_ReturnsPasswordRequiredError(string? password)
    {
        // Arrange
        var command = new RegisterCommand(
            Email: "user@example.com",
            Password: password!,
            DisplayName: null);

        // Act
        var errors = RegisterValidator.Validate(command);

        // Assert
        Assert.Single(errors);
        Assert.Equal("User.PasswordRequired", errors[0].Code);
    }

    [Fact]
    public void Validate_PasswordTooShort_ReturnsPasswordTooShortError()
    {
        // Arrange
        var command = new RegisterCommand(
            Email: "user@example.com",
            Password: "Pass1",
            DisplayName: null);

        // Act
        var errors = RegisterValidator.Validate(command);

        // Assert
        Assert.Contains(errors, e => e.Code == "User.PasswordTooShort");
    }

    [Fact]
    public void Validate_PasswordTooLong_ReturnsPasswordTooLongError()
    {
        // Arrange
        var command = new RegisterCommand(
            Email: "user@example.com",
            Password: "Password1" + new string('a', 100),
            DisplayName: null);

        // Act
        var errors = RegisterValidator.Validate(command);

        // Assert
        Assert.Contains(errors, e => e.Code == "User.PasswordTooLong");
    }

    [Fact]
    public void Validate_PasswordMissingUppercase_ReturnsPasswordMissingUppercaseError()
    {
        // Arrange
        var command = new RegisterCommand(
            Email: "user@example.com",
            Password: "password123",
            DisplayName: null);

        // Act
        var errors = RegisterValidator.Validate(command);

        // Assert
        Assert.Contains(errors, e => e.Code == "User.PasswordMissingUppercase");
    }

    [Fact]
    public void Validate_PasswordMissingLowercase_ReturnsPasswordMissingLowercaseError()
    {
        // Arrange
        var command = new RegisterCommand(
            Email: "user@example.com",
            Password: "PASSWORD123",
            DisplayName: null);

        // Act
        var errors = RegisterValidator.Validate(command);

        // Assert
        Assert.Contains(errors, e => e.Code == "User.PasswordMissingLowercase");
    }

    [Fact]
    public void Validate_PasswordMissingDigit_ReturnsPasswordMissingDigitError()
    {
        // Arrange
        var command = new RegisterCommand(
            Email: "user@example.com",
            Password: "PasswordOnly",
            DisplayName: null);

        // Act
        var errors = RegisterValidator.Validate(command);

        // Assert
        Assert.Contains(errors, e => e.Code == "User.PasswordMissingDigit");
    }

    [Fact]
    public void Validate_PasswordMissingMultipleRequirements_ReturnsMultipleErrors()
    {
        // Arrange
        var command = new RegisterCommand(
            Email: "user@example.com",
            Password: "pass",
            DisplayName: null);

        // Act
        var errors = RegisterValidator.Validate(command);

        // Assert
        Assert.Contains(errors, e => e.Code == "User.PasswordTooShort");
        Assert.Contains(errors, e => e.Code == "User.PasswordMissingUppercase");
        Assert.Contains(errors, e => e.Code == "User.PasswordMissingDigit");
    }

    [Fact]
    public void Validate_DisplayNameTooShort_ReturnsDisplayNameTooShortError()
    {
        // Arrange
        var command = new RegisterCommand(
            Email: "user@example.com",
            Password: "Password123",
            DisplayName: "A");

        // Act
        var errors = RegisterValidator.Validate(command);

        // Assert
        Assert.Single(errors);
        Assert.Equal("User.DisplayNameTooShort", errors[0].Code);
    }

    [Fact]
    public void Validate_DisplayNameTooLong_ReturnsDisplayNameTooLongError()
    {
        // Arrange
        var command = new RegisterCommand(
            Email: "user@example.com",
            Password: "Password123",
            DisplayName: new string('A', 51));

        // Act
        var errors = RegisterValidator.Validate(command);

        // Assert
        Assert.Single(errors);
        Assert.Equal("User.DisplayNameTooLong", errors[0].Code);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_DisplayNameWhitespace_ReturnsNoErrors(string? displayName)
    {
        // Arrange
        var command = new RegisterCommand(
            Email: "user@example.com",
            Password: "Password123",
            DisplayName: displayName);

        // Act
        var errors = RegisterValidator.Validate(command);

        // Assert
        Assert.Empty(errors); // Whitespace-only is treated as null/empty and is valid (optional field)
    }
}
