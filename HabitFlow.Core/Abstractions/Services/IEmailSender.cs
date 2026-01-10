namespace HabitFlow.Core.Abstractions.Services;

/// <summary>
/// Interface for sending emails.
/// </summary>
public interface IEmailSender
{
    /// <summary>
    /// Sends an email confirmation link to the specified email address.
    /// </summary>
    /// <param name="email">Recipient email address</param>
    /// <param name="confirmationLink">Full URL for email confirmation</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task SendEmailConfirmationAsync(string email, string confirmationLink, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a password reset link to the specified email address.
    /// </summary>
    /// <param name="email">Recipient email address</param>
    /// <param name="resetLink">Full URL for password reset</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task SendPasswordResetAsync(string email, string resetLink, CancellationToken cancellationToken = default);
}
