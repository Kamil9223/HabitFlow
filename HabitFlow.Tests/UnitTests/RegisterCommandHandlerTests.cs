using HabitFlow.Core.Abstractions;
using HabitFlow.Core.Features.Auth;
using HabitFlow.Data.Entities;
using Microsoft.AspNetCore.Identity;
using NSubstitute;
using Xunit;

namespace HabitFlow.Tests.UnitTests;

public class RegisterCommandHandlerTests
{
    private UserManager<ApplicationUser> CreateMockUserManager()
    {
        var store = Substitute.For<IUserStore<ApplicationUser>>();
        var passwordHasher = Substitute.For<IPasswordHasher<ApplicationUser>>();
        var userValidators = new List<IUserValidator<ApplicationUser>>();
        var passwordValidators = new List<IPasswordValidator<ApplicationUser>>();
        var keyNormalizer = Substitute.For<ILookupNormalizer>();
        var errors = Substitute.For<IdentityErrorDescriber>();
        var services = Substitute.For<IServiceProvider>();
        var logger = Substitute.For<Microsoft.Extensions.Logging.ILogger<UserManager<ApplicationUser>>>();

        return Substitute.For<UserManager<ApplicationUser>>(
            store, null, passwordHasher, userValidators, passwordValidators,
            keyNormalizer, errors, services, logger);
    }

    [Fact]
    public async Task Handle_ValidCommand_ReturnsSuccessWithUserId()
    {
        // Arrange
        var userManager = CreateMockUserManager();

        userManager.FindByEmailAsync(Arg.Any<string>()).Returns((ApplicationUser?)null);
        userManager.CreateAsync(Arg.Any<ApplicationUser>(), Arg.Any<string>())
            .Returns(callInfo =>
            {
                var user = callInfo.ArgAt<ApplicationUser>(0);
                user.Id = Guid.NewGuid().ToString();
                return Task.FromResult(IdentityResult.Success);
            });
        userManager.GenerateEmailConfirmationTokenAsync(Arg.Any<ApplicationUser>())
            .Returns("test-token");

        var emailSender = Substitute.For<IEmailSender>();
        var handler = new RegisterCommandHandler(userManager, emailSender);
        var command = new RegisterCommand(
            Email: "test@example.com",
            Password: "Password123",
            DisplayName: "Test User");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value.UserId);
        Assert.Equal("test@example.com", result.Value.Email);
        Assert.False(result.Value.EmailConfirmed);

        await userManager.Received(1).FindByEmailAsync("test@example.com");
        await userManager.Received(1).CreateAsync(
            Arg.Is<ApplicationUser>(u => u.Email == "test@example.com"),
            "Password123");
        await userManager.Received(1).UpdateAsync(
            Arg.Is<ApplicationUser>(u => u.UserName == "Test User"));
    }

    [Fact]
    public async Task Handle_ValidCommandWithoutDisplayName_ReturnsSuccessWithEmailAsUsername()
    {
        // Arrange
        var userManager = CreateMockUserManager();

        userManager.FindByEmailAsync(Arg.Any<string>()).Returns((ApplicationUser?)null);
        userManager.CreateAsync(Arg.Any<ApplicationUser>(), Arg.Any<string>())
            .Returns(callInfo =>
            {
                var user = callInfo.ArgAt<ApplicationUser>(0);
                user.Id = Guid.NewGuid().ToString();
                return Task.FromResult(IdentityResult.Success);
            });
        userManager.GenerateEmailConfirmationTokenAsync(Arg.Any<ApplicationUser>())
            .Returns("test-token");

        var emailSender = Substitute.For<IEmailSender>();
        var handler = new RegisterCommandHandler(userManager, emailSender);
        var command = new RegisterCommand(
            Email: "test@example.com",
            Password: "Password123",
            DisplayName: null);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        await userManager.DidNotReceive().UpdateAsync(Arg.Any<ApplicationUser>());
    }

    [Fact]
    public async Task Handle_InvalidEmail_ReturnsValidationError()
    {
        // Arrange
        var userManager = CreateMockUserManager();
        var emailSender = Substitute.For<IEmailSender>();
        var handler = new RegisterCommandHandler(userManager, emailSender);

        var command = new RegisterCommand(
            Email: "invalid-email",
            Password: "Password123",
            DisplayName: null);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == "User.EmailInvalidFormat");

        await userManager.DidNotReceive().FindByEmailAsync(Arg.Any<string>());
        await userManager.DidNotReceive().CreateAsync(Arg.Any<ApplicationUser>(), Arg.Any<string>());
    }

    [Fact]
    public async Task Handle_WeakPassword_ReturnsValidationError()
    {
        // Arrange
        var userManager = CreateMockUserManager();
        var emailSender = Substitute.For<IEmailSender>();
        var handler = new RegisterCommandHandler(userManager, emailSender);

        var command = new RegisterCommand(
            Email: "test@example.com",
            Password: "weak",
            DisplayName: null);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == "User.PasswordTooShort");

        await userManager.DidNotReceive().FindByEmailAsync(Arg.Any<string>());
        await userManager.DidNotReceive().CreateAsync(Arg.Any<ApplicationUser>(), Arg.Any<string>());
    }

    [Fact]
    public async Task Handle_EmailAlreadyExists_ReturnsConflictError()
    {
        // Arrange
        var userManager = CreateMockUserManager();

        var existingUser = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            Email = "test@example.com",
            UserName = "test@example.com",
            TimeZoneId = "UTC",
            CreatedAtUtc = DateTime.UtcNow
        };

        userManager.FindByEmailAsync("test@example.com").Returns(existingUser);

        var emailSender = Substitute.For<IEmailSender>();
        var handler = new RegisterCommandHandler(userManager, emailSender);
        var command = new RegisterCommand(
            Email: "test@example.com",
            Password: "Password123",
            DisplayName: null);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("User.EmailAlreadyExists", result.Error.Code);

        await userManager.Received(1).FindByEmailAsync("test@example.com");
        await userManager.DidNotReceive().CreateAsync(Arg.Any<ApplicationUser>(), Arg.Any<string>());
    }

    [Fact]
    public async Task Handle_UserManagerCreateFails_ReturnsValidationErrors()
    {
        // Arrange
        var userManager = CreateMockUserManager();

        userManager.FindByEmailAsync(Arg.Any<string>()).Returns((ApplicationUser?)null);

        var identityErrors = new[]
        {
            new IdentityError { Code = "PasswordTooShort", Description = "Password is too short" },
            new IdentityError { Code = "PasswordRequiresDigit", Description = "Password must contain a digit" }
        };

        userManager.CreateAsync(Arg.Any<ApplicationUser>(), Arg.Any<string>())
            .Returns(IdentityResult.Failed(identityErrors));

        var emailSender = Substitute.For<IEmailSender>();
        var handler = new RegisterCommandHandler(userManager, emailSender);
        var command = new RegisterCommand(
            Email: "test@example.com",
            Password: "Password123", // Valid per our validator, but UserManager can still reject it
            DisplayName: null);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(2, result.Errors.Count);
        Assert.Contains(result.Errors, e => e.Code == "User.PasswordTooShort");
        Assert.Contains(result.Errors, e => e.Code == "User.PasswordRequiresDigit");
    }

    [Fact]
    public async Task Handle_CreatesUserWithDefaultTimezone()
    {
        // Arrange
        var userManager = CreateMockUserManager();

        ApplicationUser? capturedUser = null;
        userManager.FindByEmailAsync(Arg.Any<string>()).Returns((ApplicationUser?)null);
        userManager.CreateAsync(Arg.Any<ApplicationUser>(), Arg.Any<string>())
            .Returns(callInfo =>
            {
                capturedUser = callInfo.ArgAt<ApplicationUser>(0);
                capturedUser.Id = Guid.NewGuid().ToString();
                return Task.FromResult(IdentityResult.Success);
            });

        var emailSender = Substitute.For<IEmailSender>();
        var handler = new RegisterCommandHandler(userManager, emailSender);
        var command = new RegisterCommand(
            Email: "test@example.com",
            Password: "Password123",
            DisplayName: null);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(capturedUser);
        Assert.Equal("UTC", capturedUser.TimeZoneId);
        Assert.Equal("test@example.com", capturedUser.Email);
        Assert.Equal("test@example.com", capturedUser.UserName);
    }
}
