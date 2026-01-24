using System.Net;
using System.Net.Http.Json;
using HabitFlow.Api.Contracts.Auth;
using HabitFlow.Data.Entities;
using HabitFlow.Tests.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace HabitFlow.Tests.IntegrationTests;

public class AuthEndpointsTests(IntegrationTestFixture fixture) : IClassFixture<IntegrationTestFixture>
{
    [Fact]
    public async Task Logout_WithoutAuthentication_ReturnsUnauthorized()
    {
        using var client = fixture.CreateClient();

        var response = await client.PostAsync("/api/v1/auth/logout", new StringContent(string.Empty));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task LoginAndLogout_Succeed()
    {
        // Arrange: Create and confirm a user
        using var scope = fixture.Factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var testEmail = $"logout-test-{Guid.NewGuid()}@test.com";
        var testPassword = "Test123!";

        var user = new ApplicationUser
        {
            UserName = testEmail,
            Email = testEmail,
            EmailConfirmed = true,
            TimeZoneId = "Europe/Warsaw",
            CreatedAtUtc = DateTime.UtcNow
        };

        var createResult = await userManager.CreateAsync(user, testPassword);
        Assert.True(createResult.Succeeded);

        // Act: Login and then logout
        using var client = fixture.CreateClient();

        var loginRequest = new LoginRequest(testEmail, testPassword);
        var loginResponse = await client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        // Extract cookies from login response
        var cookies = loginResponse.Headers.GetValues("Set-Cookie");
        foreach (var cookie in cookies)
        {
            client.DefaultRequestHeaders.Add("Cookie", cookie.Split(';')[0]);
        }

        var logoutResponse = await client.PostAsync("/api/v1/auth/logout", new StringContent(string.Empty));

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, logoutResponse.StatusCode);
    }

    [Fact]
    public async Task ForgotPassword_WithValidEmail_ReturnsNoContent()
    {
        // Arrange: Create and confirm a user
        using var scope = fixture.Factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var testEmail = $"forgot-password-test-{Guid.NewGuid()}@test.com";
        var testPassword = "Test123!";

        var user = new ApplicationUser
        {
            UserName = testEmail,
            Email = testEmail,
            EmailConfirmed = true,
            TimeZoneId = "Europe/Warsaw",
            CreatedAtUtc = DateTime.UtcNow
        };

        var createResult = await userManager.CreateAsync(user, testPassword);
        Assert.True(createResult.Succeeded);

        // Act: Request password reset
        using var client = fixture.CreateClient();
        var forgotPasswordRequest = new ForgotPasswordRequest(testEmail);
        var response = await client.PostAsJsonAsync("/api/v1/auth/forgot-password", forgotPasswordRequest);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task ForgotPassword_WithNonExistentEmail_ReturnsNoContent()
    {
        // Arrange
        using var client = fixture.CreateClient();
        var nonExistentEmail = $"nonexistent-{Guid.NewGuid()}@test.com";
        var forgotPasswordRequest = new ForgotPasswordRequest(nonExistentEmail);

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/auth/forgot-password", forgotPasswordRequest);

        // Assert: Should return NoContent to prevent user enumeration
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task ForgotPassword_WithInvalidEmail_ReturnsBadRequest()
    {
        // Arrange
        using var client = fixture.CreateClient();
        var invalidEmailRequest = new ForgotPasswordRequest("not-an-email");

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/auth/forgot-password", invalidEmailRequest);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ResetPassword_WithValidToken_ReturnsNoContent()
    {
        // Arrange: Create a user and generate reset token
        using var scope = fixture.Factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var testEmail = $"reset-password-test-{Guid.NewGuid()}@test.com";
        var testPassword = "Test123!";
        var newPassword = "NewTest456!";

        var user = new ApplicationUser
        {
            UserName = testEmail,
            Email = testEmail,
            EmailConfirmed = true,
            TimeZoneId = "Europe/Warsaw",
            CreatedAtUtc = DateTime.UtcNow
        };

        var createResult = await userManager.CreateAsync(user, testPassword);
        Assert.True(createResult.Succeeded);

        // Generate reset token
        var resetToken = await userManager.GeneratePasswordResetTokenAsync(user);

        // Act: Reset password
        using var client = fixture.CreateClient();
        var resetPasswordRequest = new ResetPasswordRequest(testEmail, resetToken, newPassword);
        var response = await client.PostAsJsonAsync("/api/v1/auth/reset-password", resetPasswordRequest);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        // Verify: Can login with new password
        var loginRequest = new LoginRequest(testEmail, newPassword);
        var loginResponse = await client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
    }

    [Fact]
    public async Task ResetPassword_WithInvalidToken_ReturnsBadRequest()
    {
        // Arrange: Create a user
        using var scope = fixture.Factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var testEmail = $"reset-invalid-token-{Guid.NewGuid()}@test.com";
        var testPassword = "Test123!";

        var user = new ApplicationUser
        {
            UserName = testEmail,
            Email = testEmail,
            EmailConfirmed = true,
            TimeZoneId = "Europe/Warsaw",
            CreatedAtUtc = DateTime.UtcNow
        };

        var createResult = await userManager.CreateAsync(user, testPassword);
        Assert.True(createResult.Succeeded);

        // Act: Try to reset password with invalid token
        using var client = fixture.CreateClient();
        var resetPasswordRequest = new ResetPasswordRequest(testEmail, "invalid-token", "NewTest456!");
        var response = await client.PostAsJsonAsync("/api/v1/auth/reset-password", resetPasswordRequest);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ResetPassword_WithWeakPassword_ReturnsBadRequest()
    {
        // Arrange: Create a user and generate reset token
        using var scope = fixture.Factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var testEmail = $"reset-weak-password-{Guid.NewGuid()}@test.com";
        var testPassword = "Test123!";

        var user = new ApplicationUser
        {
            UserName = testEmail,
            Email = testEmail,
            EmailConfirmed = true,
            TimeZoneId = "Europe/Warsaw",
            CreatedAtUtc = DateTime.UtcNow
        };

        var createResult = await userManager.CreateAsync(user, testPassword);
        Assert.True(createResult.Succeeded);

        var resetToken = await userManager.GeneratePasswordResetTokenAsync(user);

        // Act: Try to reset password with weak password
        using var client = fixture.CreateClient();
        var resetPasswordRequest = new ResetPasswordRequest(testEmail, resetToken, "weak");
        var response = await client.PostAsJsonAsync("/api/v1/auth/reset-password", resetPasswordRequest);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task DeleteAccount_WithoutAuthentication_ReturnsUnauthorized()
    {
        using var client = fixture.CreateClient();

        var request = new DeleteAccountRequest("DELETE");
        var response = await client.PostAsJsonAsync("/api/v1/auth/delete-account", request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task DeleteAccount_WithValidConfirmation_DeletesAccountAndData()
    {
        // Arrange: Create and login a user
        using var scope = fixture.Factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var testEmail = $"delete-account-test-{Guid.NewGuid()}@test.com";
        var testPassword = "Test123!";

        var user = new ApplicationUser
        {
            UserName = testEmail,
            Email = testEmail,
            EmailConfirmed = true,
            TimeZoneId = "Europe/Warsaw",
            CreatedAtUtc = DateTime.UtcNow
        };

        var createResult = await userManager.CreateAsync(user, testPassword);
        Assert.True(createResult.Succeeded);

        // Login
        using var client = fixture.CreateClient();
        var loginRequest = new LoginRequest(testEmail, testPassword);
        var loginResponse = await client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        // Extract cookies from login response
        var cookies = loginResponse.Headers.GetValues("Set-Cookie");
        foreach (var cookie in cookies)
        {
            client.DefaultRequestHeaders.Add("Cookie", cookie.Split(';')[0]);
        }

        // Act: Delete account
        var deleteRequest = new DeleteAccountRequest("DELETE");
        var deleteResponse = await client.PostAsJsonAsync("/api/v1/auth/delete-account", deleteRequest);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        // Verify: User no longer exists
        using var verifyScope = fixture.Factory.Services.CreateScope();
        var verifyUserManager = verifyScope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var deletedUser = await verifyUserManager.FindByEmailAsync(testEmail);
        Assert.Null(deletedUser);
    }

    [Fact]
    public async Task DeleteAccount_WithInvalidConfirmation_ReturnsBadRequest()
    {
        // Arrange: Create and login a user
        using var scope = fixture.Factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var testEmail = $"delete-invalid-confirm-{Guid.NewGuid()}@test.com";
        var testPassword = "Test123!";

        var user = new ApplicationUser
        {
            UserName = testEmail,
            Email = testEmail,
            EmailConfirmed = true,
            TimeZoneId = "Europe/Warsaw",
            CreatedAtUtc = DateTime.UtcNow
        };

        var createResult = await userManager.CreateAsync(user, testPassword);
        Assert.True(createResult.Succeeded);

        // Login
        using var client = fixture.CreateClient();
        var loginRequest = new LoginRequest(testEmail, testPassword);
        var loginResponse = await client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        // Extract cookies from login response
        var cookies = loginResponse.Headers.GetValues("Set-Cookie");
        foreach (var cookie in cookies)
        {
            client.DefaultRequestHeaders.Add("Cookie", cookie.Split(';')[0]);
        }

        // Act: Try to delete account with wrong confirmation
        var deleteRequest = new DeleteAccountRequest("WRONG");
        var deleteResponse = await client.PostAsJsonAsync("/api/v1/auth/delete-account", deleteRequest);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, deleteResponse.StatusCode);

        // Verify: User still exists
        using var verifyScope = fixture.Factory.Services.CreateScope();
        var verifyUserManager = verifyScope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var stillExistingUser = await verifyUserManager.FindByEmailAsync(testEmail);
        Assert.NotNull(stillExistingUser);
    }

    [Fact]
    public async Task DeleteAccount_WithEmptyConfirmation_ReturnsBadRequest()
    {
        // Arrange: Create and login a user
        using var scope = fixture.Factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var testEmail = $"delete-empty-confirm-{Guid.NewGuid()}@test.com";
        var testPassword = "Test123!";

        var user = new ApplicationUser
        {
            UserName = testEmail,
            Email = testEmail,
            EmailConfirmed = true,
            TimeZoneId = "Europe/Warsaw",
            CreatedAtUtc = DateTime.UtcNow
        };

        var createResult = await userManager.CreateAsync(user, testPassword);
        Assert.True(createResult.Succeeded);

        // Login
        using var client = fixture.CreateClient();
        var loginRequest = new LoginRequest(testEmail, testPassword);
        var loginResponse = await client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        // Extract cookies from login response
        var cookies = loginResponse.Headers.GetValues("Set-Cookie");
        foreach (var cookie in cookies)
        {
            client.DefaultRequestHeaders.Add("Cookie", cookie.Split(';')[0]);
        }

        // Act: Try to delete account with empty confirmation
        var deleteRequest = new DeleteAccountRequest("");
        var deleteResponse = await client.PostAsJsonAsync("/api/v1/auth/delete-account", deleteRequest);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, deleteResponse.StatusCode);
    }

    [Fact]
    public async Task ResendConfirmation_WithUnconfirmedEmail_ReturnsNoContent()
    {
        // Arrange: Create a user with unconfirmed email
        using var scope = fixture.Factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var testEmail = $"resend-confirmation-test-{Guid.NewGuid()}@test.com";
        var testPassword = "Test123!";

        var user = new ApplicationUser
        {
            UserName = testEmail,
            Email = testEmail,
            EmailConfirmed = false, // Email NOT confirmed
            TimeZoneId = "Europe/Warsaw",
            CreatedAtUtc = DateTime.UtcNow
        };

        var createResult = await userManager.CreateAsync(user, testPassword);
        Assert.True(createResult.Succeeded);

        // Manually confirm email to allow login (in real scenario, user would need to confirm first)
        // We'll temporarily set it to true for login, then set back to false
        user.EmailConfirmed = true;
        await userManager.UpdateAsync(user);

        // Login to get authentication cookie
        using var client = fixture.CreateClient();
        var loginRequest = new LoginRequest(testEmail, testPassword);
        var loginResponse = await client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        // Extract cookies from login response
        var cookies = loginResponse.Headers.GetValues("Set-Cookie");
        foreach (var cookie in cookies)
        {
            client.DefaultRequestHeaders.Add("Cookie", cookie.Split(';')[0]);
        }

        // Set email back to unconfirmed for the actual test
        user.EmailConfirmed = false;
        await userManager.UpdateAsync(user);

        // Act: Resend confirmation email
        var resendResponse = await client.PostAsync("/api/v1/auth/resend-confirmation", new StringContent(string.Empty));

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, resendResponse.StatusCode);
    }

    [Fact]
    public async Task ResendConfirmation_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Arrange
        using var client = fixture.CreateClient();

        // Act: Try to resend confirmation without being authenticated
        var response = await client.PostAsync("/api/v1/auth/resend-confirmation", new StringContent(string.Empty));

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ResendConfirmation_WithAlreadyConfirmedEmail_ReturnsConflict()
    {
        // Arrange: Create a user with confirmed email
        using var scope = fixture.Factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var testEmail = $"resend-already-confirmed-{Guid.NewGuid()}@test.com";
        var testPassword = "Test123!";

        var user = new ApplicationUser
        {
            UserName = testEmail,
            Email = testEmail,
            EmailConfirmed = true, // Email already confirmed
            TimeZoneId = "Europe/Warsaw",
            CreatedAtUtc = DateTime.UtcNow
        };

        var createResult = await userManager.CreateAsync(user, testPassword);
        Assert.True(createResult.Succeeded);

        // Login
        using var client = fixture.CreateClient();
        var loginRequest = new LoginRequest(testEmail, testPassword);
        var loginResponse = await client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        // Extract cookies from login response
        var cookies = loginResponse.Headers.GetValues("Set-Cookie");
        foreach (var cookie in cookies)
        {
            client.DefaultRequestHeaders.Add("Cookie", cookie.Split(';')[0]);
        }

        // Act: Try to resend confirmation for already confirmed email
        var resendResponse = await client.PostAsync("/api/v1/auth/resend-confirmation", new StringContent(string.Empty));

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, resendResponse.StatusCode);
    }
}
