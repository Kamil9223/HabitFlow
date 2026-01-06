using HabitFlow.Core.Features.Auth;
using Xunit;

namespace HabitFlow.Tests.UnitTests;

public class LoginValidatorTests
{
    [Fact]
    public void Validate_ValidCommand_ReturnsNoErrors()
    {
        // Arrange
        var command = new LoginCommand(
            Email: "test@example.com",
            Password: "Password123");

        // Act
        var errors = LoginValidator.Validate(command);

        // Assert
        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_EmptyEmail_ReturnsEmailRequiredError()
    {
        // Arrange
        var command = new LoginCommand(
            Email: "",
            Password: "Password123");

        // Act
        var errors = LoginValidator.Validate(command);

        // Assert
        Assert.Single(errors);
        Assert.Equal("Login.EmailRequired", errors[0].Code);
        Assert.Equal("Email is required.", errors[0].Description);
    }

    [Fact]
    public void Validate_NullEmail_ReturnsEmailRequiredError()
    {
        // Arrange
        var command = new LoginCommand(
            Email: null!,
            Password: "Password123");

        // Act
        var errors = LoginValidator.Validate(command);

        // Assert
        Assert.Single(errors);
        Assert.Equal("Login.EmailRequired", errors[0].Code);
    }

    [Fact]
    public void Validate_WhitespaceEmail_ReturnsEmailRequiredError()
    {
        // Arrange
        var command = new LoginCommand(
            Email: "   ",
            Password: "Password123");

        // Act
        var errors = LoginValidator.Validate(command);

        // Assert
        Assert.Single(errors);
        Assert.Equal("Login.EmailRequired", errors[0].Code);
    }

    [Theory]
    [InlineData("invalid-email")]
    [InlineData("@example.com")]
    [InlineData("test@")]
    [InlineData("test")]
    [InlineData("test@.com")]
    public void Validate_InvalidEmailFormat_ReturnsEmailInvalidError(string invalidEmail)
    {
        // Arrange
        var command = new LoginCommand(
            Email: invalidEmail,
            Password: "Password123");

        // Act
        var errors = LoginValidator.Validate(command);

        // Assert
        Assert.Single(errors);
        Assert.Equal("Login.EmailInvalid", errors[0].Code);
        Assert.Equal("Email format is invalid.", errors[0].Description);
    }

    [Fact]
    public void Validate_EmptyPassword_ReturnsPasswordRequiredError()
    {
        // Arrange
        var command = new LoginCommand(
            Email: "test@example.com",
            Password: "");

        // Act
        var errors = LoginValidator.Validate(command);

        // Assert
        Assert.Single(errors);
        Assert.Equal("Login.PasswordRequired", errors[0].Code);
        Assert.Equal("Password is required.", errors[0].Description);
    }

    [Fact]
    public void Validate_NullPassword_ReturnsPasswordRequiredError()
    {
        // Arrange
        var command = new LoginCommand(
            Email: "test@example.com",
            Password: null!);

        // Act
        var errors = LoginValidator.Validate(command);

        // Assert
        Assert.Single(errors);
        Assert.Equal("Login.PasswordRequired", errors[0].Code);
    }

    [Fact]
    public void Validate_WhitespacePassword_ReturnsPasswordRequiredError()
    {
        // Arrange
        var command = new LoginCommand(
            Email: "test@example.com",
            Password: "   ");

        // Act
        var errors = LoginValidator.Validate(command);

        // Assert
        Assert.Single(errors);
        Assert.Equal("Login.PasswordRequired", errors[0].Code);
    }

    [Theory]
    [InlineData("Pass1")]      // 5 characters
    [InlineData("Pass12")]     // 6 characters
    [InlineData("Pass123")]    // 7 characters
    public void Validate_PasswordTooShort_ReturnsPasswordTooShortError(string shortPassword)
    {
        // Arrange
        var command = new LoginCommand(
            Email: "test@example.com",
            Password: shortPassword);

        // Act
        var errors = LoginValidator.Validate(command);

        // Assert
        Assert.Single(errors);
        Assert.Equal("Login.PasswordTooShort", errors[0].Code);
        Assert.Contains("at least 8 characters", errors[0].Description);
    }

    [Fact]
    public void Validate_PasswordExactly8Characters_ReturnsNoErrors()
    {
        // Arrange
        var command = new LoginCommand(
            Email: "test@example.com",
            Password: "Pass1234");

        // Act
        var errors = LoginValidator.Validate(command);

        // Assert
        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_MultipleErrors_ReturnsAllErrors()
    {
        // Arrange
        var command = new LoginCommand(
            Email: "invalid-email",
            Password: "short");

        // Act
        var errors = LoginValidator.Validate(command);

        // Assert
        Assert.Equal(2, errors.Count);
        Assert.Contains(errors, e => e.Code == "Login.EmailInvalid");
        Assert.Contains(errors, e => e.Code == "Login.PasswordTooShort");
    }

    [Fact]
    public void Validate_BothFieldsMissing_ReturnsMultipleErrors()
    {
        // Arrange
        var command = new LoginCommand(
            Email: "",
            Password: "");

        // Act
        var errors = LoginValidator.Validate(command);

        // Assert
        Assert.Equal(2, errors.Count);
        Assert.Contains(errors, e => e.Code == "Login.EmailRequired");
        Assert.Contains(errors, e => e.Code == "Login.PasswordRequired");
    }
}
