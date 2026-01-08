using HabitFlow.Core.Features.Auth;
using HabitFlow.Data.Entities;
using Microsoft.AspNetCore.Identity;
using NSubstitute;
using Xunit;

namespace HabitFlow.Tests.UnitTests;

public class LoginCommandHandlerTests
{
    private class FakeSignInManager : SignInManager<ApplicationUser>
    {
        public SignInResult FakeSignInResult { get; set; } = SignInResult.Success;

        public FakeSignInManager(
            UserManager<ApplicationUser> userManager)
            : base(
                userManager,
                Substitute.For<Microsoft.AspNetCore.Http.IHttpContextAccessor>(),
                Substitute.For<IUserClaimsPrincipalFactory<ApplicationUser>>(),
                Substitute.For<Microsoft.Extensions.Options.IOptions<IdentityOptions>>(),
                Substitute.For<Microsoft.Extensions.Logging.ILogger<SignInManager<ApplicationUser>>>(),
                Substitute.For<Microsoft.AspNetCore.Authentication.IAuthenticationSchemeProvider>(),
                Substitute.For<IUserConfirmation<ApplicationUser>>())
        {
        }

        public override Task<SignInResult> PasswordSignInAsync(ApplicationUser user, string password, bool isPersistent, bool lockoutOnFailure)
        {
            return Task.FromResult(FakeSignInResult);
        }
    }

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
    public async Task Handle_ValidCredentials_ReturnsSuccessWithUserInfo()
    {
        // Arrange
        var userManager = CreateMockUserManager();
        var signInManager = new FakeSignInManager(userManager)
        {
            FakeSignInResult = SignInResult.Success
        };

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            Email = "test@example.com",
            EmailConfirmed = true,
            UserName = "test@example.com"
        };

        userManager.FindByEmailAsync("test@example.com").Returns(user);

        var handler = new LoginCommandHandler(userManager, signInManager);
        var command = new LoginCommand("test@example.com", "Password123");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(user.Id, result.Value.UserId);
        Assert.Equal(user.Email, result.Value.Email);
        Assert.True(result.Value.EmailConfirmed);
    }

    [Fact]
    public async Task Handle_InvalidEmail_ReturnsUnauthorizedError()
    {
        // Arrange
        var userManager = CreateMockUserManager();
        var signInManager = new FakeSignInManager(userManager);

        userManager.FindByEmailAsync("invalid@example.com").Returns((ApplicationUser?)null);

        var handler = new LoginCommandHandler(userManager, signInManager);
        var command = new LoginCommand("invalid@example.com", "Password123");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Single(result.Errors);
        Assert.Equal("Auth.InvalidCredentials", result.Error.Code);
        Assert.Equal("Invalid email or password.", result.Error.Description);
    }

    [Fact]
    public async Task Handle_EmailNotConfirmed_ReturnsForbiddenError()
    {
        // Arrange
        var userManager = CreateMockUserManager();
        var signInManager = new FakeSignInManager(userManager);

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            Email = "test@example.com",
            EmailConfirmed = false,
            UserName = "test@example.com"
        };

        userManager.FindByEmailAsync("test@example.com").Returns(user);

        var handler = new LoginCommandHandler(userManager, signInManager);
        var command = new LoginCommand("test@example.com", "Password123");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Single(result.Errors);
        Assert.Equal("Auth.EmailNotConfirmed", result.Error.Code);
        Assert.Contains("not been confirmed", result.Error.Description);
    }

    [Fact]
    public async Task Handle_InvalidPassword_ReturnsUnauthorizedError()
    {
        // Arrange
        var userManager = CreateMockUserManager();
        var signInManager = new FakeSignInManager(userManager)
        {
            FakeSignInResult = SignInResult.Failed
        };

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            Email = "test@example.com",
            EmailConfirmed = true,
            UserName = "test@example.com"
        };

        userManager.FindByEmailAsync("test@example.com").Returns(user);

        var handler = new LoginCommandHandler(userManager, signInManager);
        var command = new LoginCommand("test@example.com", "WrongPassword");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Single(result.Errors);
        Assert.Equal("Auth.InvalidCredentials", result.Error.Code);
        Assert.Equal("Invalid email or password.", result.Error.Description);
    }

    [Fact]
    public async Task Handle_InvalidCommand_ReturnsValidationErrors()
    {
        // Arrange
        var userManager = CreateMockUserManager();
        var signInManager = new FakeSignInManager(userManager);

        var handler = new LoginCommandHandler(userManager, signInManager);
        var command = new LoginCommand("", ""); // Invalid command

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.True(result.Errors.Count >= 2); // Email and password errors
        Assert.Contains(result.Errors, e => e.Code.Contains("Email"));
        Assert.Contains(result.Errors, e => e.Code.Contains("Password"));
    }

    [Fact]
    public async Task Handle_UserNotFound_DoesNotRevealUserExistence()
    {
        // Arrange
        var userManager = CreateMockUserManager();
        var signInManager = new FakeSignInManager(userManager);

        userManager.FindByEmailAsync("nonexistent@example.com").Returns((ApplicationUser?)null);

        var handler = new LoginCommandHandler(userManager, signInManager);
        var command = new LoginCommand("nonexistent@example.com", "Password123");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert - Should return same message as wrong password
        Assert.False(result.IsSuccess);
        Assert.Equal("Auth.InvalidCredentials", result.Error.Code);
        Assert.Equal("Invalid email or password.", result.Error.Description);
    }

    [Fact]
    public async Task Handle_WrongPassword_DoesNotRevealUserExistence()
    {
        // Arrange
        var userManager = CreateMockUserManager();
        var signInManager = new FakeSignInManager(userManager)
        {
            FakeSignInResult = SignInResult.Failed
        };

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            Email = "test@example.com",
            EmailConfirmed = true,
            UserName = "test@example.com"
        };

        userManager.FindByEmailAsync("test@example.com").Returns(user);

        var handler = new LoginCommandHandler(userManager, signInManager);
        var command = new LoginCommand("test@example.com", "WrongPassword");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert - Should return same generic message
        Assert.False(result.IsSuccess);
        Assert.Equal("Auth.InvalidCredentials", result.Error.Code);
        Assert.Equal("Invalid email or password.", result.Error.Description);
    }
}
