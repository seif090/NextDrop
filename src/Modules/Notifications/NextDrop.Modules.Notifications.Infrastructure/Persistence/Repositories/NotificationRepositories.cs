using Microsoft.EntityFrameworkCore;
using NextDrop.Infrastructure.Persistence;
using NextDrop.Modules.Notifications.Application.Abstractions;
using NextDrop.Modules.Notifications.Domain.Aggregates;
using NextDrop.Modules.Notifications.Domain.Enums;
using NextDrop.Modules.Notifications.Domain.ValueObjects;

namespace NextDrop.Modules.Notifications.Infrastructure.Persistence.Repositories;

public class NotificationRepository : INotificationRepository
{
    private readonly NextDropDbContext _dbContext;

    public NotificationRepository(NextDropDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Notification?> GetByIdAsync(NotificationId id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Notifications
            .Include(n => n.Deliveries)
            .FirstOrDefaultAsync(n => n.Id == id, cancellationToken);
    }

    public async Task<(List<Notification> Items, int TotalCount)> GetPagedByUserIdAsync(Guid userId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Notifications
            .Include(n => n.Deliveries)
            .Where(n => n.UserId == userId && n.Status != NotificationStatus.Archived)
            .OrderByDescending(n => n.CreatedAtUtc);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<List<Notification>> GetUnreadByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Notifications
            .Include(n => n.Deliveries)
            .Where(n => n.UserId == userId && n.Status == NotificationStatus.Unread)
            .OrderByDescending(n => n.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetUnreadCountByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Notifications
            .Where(n => n.UserId == userId && n.Status == NotificationStatus.Unread)
            .CountAsync(cancellationToken);
    }

    public async Task AddAsync(Notification notification, CancellationToken cancellationToken = default)
    {
        await _dbContext.Notifications.AddAsync(notification, cancellationToken);
    }

    public void Remove(Notification notification)
    {
        _dbContext.Notifications.Remove(notification);
    }
}

public class NotificationTemplateRepository : INotificationTemplateRepository
{
    private readonly NextDropDbContext _dbContext;

    public NotificationTemplateRepository(NextDropDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<NotificationTemplate?> GetActiveTemplateAsync(NotificationType type, NotificationChannel channel, string language, CancellationToken cancellationToken = default)
    {
        var lang = string.IsNullOrWhiteSpace(language) ? "en" : language.Trim().ToLowerInvariant();
        return await _dbContext.NotificationTemplates
            .Where(t => t.Type == type && t.Channel == channel && t.Language == lang && t.IsActive)
            .OrderByDescending(t => t.Version)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task AddAsync(NotificationTemplate template, CancellationToken cancellationToken = default)
    {
        await _dbContext.NotificationTemplates.AddAsync(template, cancellationToken);
    }
}

public class UserNotificationPreferenceRepository : IUserNotificationPreferenceRepository
{
    private readonly NextDropDbContext _dbContext;

    public UserNotificationPreferenceRepository(NextDropDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<UserNotificationPreference?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.UserNotificationPreferences
            .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);
    }

    public async Task AddAsync(UserNotificationPreference preference, CancellationToken cancellationToken = default)
    {
        await _dbContext.UserNotificationPreferences.AddAsync(preference, cancellationToken);
    }
}

public class ProcessedIntegrationEventRepository : IProcessedIntegrationEventRepository
{
    private readonly NextDropDbContext _dbContext;

    public ProcessedIntegrationEventRepository(NextDropDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<bool> IsEventProcessedAsync(string consumerName, string eventId, CancellationToken cancellationToken = default)
    {
        var consumer = consumerName.Trim();
        var evtId = eventId.Trim();
        return await _dbContext.ProcessedIntegrationEvents
            .AnyAsync(e => e.ConsumerName == consumer && e.EventId == evtId, cancellationToken);
    }

    public async Task AddAsync(ProcessedIntegrationEvent processedEvent, CancellationToken cancellationToken = default)
    {
        await _dbContext.ProcessedIntegrationEvents.AddAsync(processedEvent, cancellationToken);
    }
}
