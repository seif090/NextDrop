namespace NextDrop.Modules.Identity.Application.Abstractions;

public interface IEmailService
{
    Task SendVerificationEmailAsync(string email, string rawVerificationToken, CancellationToken cancellationToken = default);
    Task SendPasswordResetEmailAsync(string email, string rawResetToken, CancellationToken cancellationToken = default);
}
