using Microsoft.Extensions.Logging;
using NextDrop.Modules.Identity.Application.Abstractions;

namespace NextDrop.Modules.Identity.Infrastructure.Services;

public class DevEmailService : IEmailService
{
    private readonly ILogger<DevEmailService> _logger;

    public DevEmailService(ILogger<DevEmailService> logger)
    {
        _logger = logger;
    }

    public Task SendVerificationEmailAsync(string email, string rawVerificationToken, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[DEV EMAIL] Verification email dispatched to {Email}. Token: {Token}", email, rawVerificationToken);
        return Task.CompletedTask;
    }

    public Task SendPasswordResetEmailAsync(string email, string rawResetToken, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[DEV EMAIL] Password reset email dispatched to {Email}. Token: {Token}", email, rawResetToken);
        return Task.CompletedTask;
    }
}
