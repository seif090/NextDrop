using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NextDrop.Infrastructure.Persistence;
using NextDrop.SharedKernel.Abstractions;

namespace NextDrop.Infrastructure.Outbox;

public class OutboxProcessorJob
{
    private readonly NextDropDbContext _context;
    private readonly IMessagePublisher _publisher;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILogger<OutboxProcessorJob> _logger;

    public OutboxProcessorJob(
        NextDropDbContext context,
        IMessagePublisher publisher,
        IDateTimeProvider dateTimeProvider,
        ILogger<OutboxProcessorJob> logger)
    {
        _context = context;
        _publisher = publisher;
        _dateTimeProvider = dateTimeProvider;
        _logger = logger;
    }

    public async Task ProcessOutboxMessagesAsync(CancellationToken cancellationToken = default)
    {
        var now = _dateTimeProvider.UtcNow;
        var pendingMessages = await _context.OutboxMessages
            .Where(m => m.ProcessedOnUtc == null && m.RetryCount < 5)
            .OrderBy(m => m.OccurredOnUtc)
            .Take(20)
            .ToListAsync(cancellationToken);

        if (!pendingMessages.Any())
        {
            return;
        }

        foreach (var message in pendingMessages)
        {
            try
            {
                var eventObject = JsonSerializer.Deserialize<object>(message.Content);
                if (eventObject != null)
                {
                    await _publisher.PublishAsync(eventObject, "nextdrop-events", message.Type, cancellationToken);
                }

                message.ProcessedOnUtc = now;
                message.Error = null;
            }
            catch (Exception ex)
            {
                message.RetryCount++;
                message.Error = ex.Message;
                _logger.LogError(ex, "Failed to process Outbox message {MessageId}. Retry count: {RetryCount}", message.Id, message.RetryCount);
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}
