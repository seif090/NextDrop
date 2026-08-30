using FluentValidation;
using MediatR;
using NextDrop.Modules.Notifications.Application.Abstractions;
using NextDrop.Modules.Notifications.Application.DTOs;
using NextDrop.Modules.Notifications.Domain.Aggregates;
using NextDrop.Modules.Notifications.Domain.Enums;
using NextDrop.Modules.Notifications.Domain.ValueObjects;
using NextDrop.SharedKernel.Abstractions;
using NextDrop.SharedKernel.Common;

namespace NextDrop.Modules.Notifications.Application.Commands;

// 1. CREATE NOTIFICATION
public record CreateNotificationCommand(
    Guid UserId,
    NotificationType Type,
    NotificationChannel Channel,
    string Title,
    string Body,
    string? DataJson,
    NotificationPriority Priority = NotificationPriority.Normal) : IRequest<Result<NotificationDto>>;

public class CreateNotificationCommandValidator : AbstractValidator<CreateNotificationCommand>
{
    public CreateNotificationCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Body).NotEmpty().MaximumLength(1000);
    }
}

public class CreateNotificationCommandHandler : IRequestHandler<CreateNotificationCommand, Result<NotificationDto>>
{
    private readonly INotificationRepository _notificationRepository;
    private readonly IUserNotificationPreferenceRepository _preferenceRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;

    public CreateNotificationCommandHandler(
        INotificationRepository notificationRepository,
        IUserNotificationPreferenceRepository preferenceRepository,
        IUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider)
    {
        _notificationRepository = notificationRepository;
        _preferenceRepository = preferenceRepository;
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<NotificationDto>> Handle(CreateNotificationCommand request, CancellationToken cancellationToken)
    {
        var pref = await _preferenceRepository.GetByUserIdAsync(request.UserId, cancellationToken);
        if (pref != null)
        {
            // Check preference rules
            if (request.Type == NotificationType.Marketing && !pref.AllowMarketingNotifications)
                return Result.Failure<NotificationDto>(Error.Conflict("Notification.DisabledByPreference", "User has disabled marketing notifications."));

            if (request.Channel == NotificationChannel.Email && !pref.EmailEnabled)
                return Result.Failure<NotificationDto>(Error.Conflict("Notification.ChannelDisabled", "User has disabled email channel."));
        }

        var notificationId = NotificationId.New();
        var result = Notification.Create(
            notificationId,
            request.UserId,
            request.Type,
            request.Title,
            request.Body,
            request.DataJson,
            request.Channel,
            request.Priority,
            _dateTimeProvider.UtcNow);

        if (result.IsFailure)
            return Result.Failure<NotificationDto>(result.Error);

        var notification = result.Value;
        await _notificationRepository.AddAsync(notification, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapToDto(notification);
    }

    internal static NotificationDto MapToDto(Notification notification)
    {
        return new NotificationDto(
            notification.Id.Value,
            notification.UserId,
            notification.Type.ToString(),
            notification.Title,
            notification.Body,
            notification.DataJson,
            notification.Channel.ToString(),
            notification.Priority.ToString(),
            notification.Status.ToString(),
            notification.CreatedAtUtc,
            notification.ReadAtUtc);
    }
}

// 2. MARK AS READ
public record MarkNotificationAsReadCommand(
    Guid RequesterUserId,
    Guid NotificationId) : IRequest<Result>;

public class MarkNotificationAsReadCommandHandler : IRequestHandler<MarkNotificationAsReadCommand, Result>
{
    private readonly INotificationRepository _notificationRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;

    public MarkNotificationAsReadCommandHandler(
        INotificationRepository notificationRepository,
        IUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider)
    {
        _notificationRepository = notificationRepository;
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(MarkNotificationAsReadCommand request, CancellationToken cancellationToken)
    {
        var notification = await _notificationRepository.GetByIdAsync(new NotificationId(request.NotificationId), cancellationToken);
        if (notification == null)
            return Result.Failure(Error.NotFound("Notification.NotFound", "Notification not found."));

        if (notification.UserId != request.RequesterUserId)
            return Result.Failure(Error.Forbidden("Notification.Forbidden", "Not authorized to modify this notification."));

        notification.MarkAsRead(_dateTimeProvider.UtcNow);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

// 3. MARK ALL AS READ
public record MarkAllNotificationsAsReadCommand(Guid RequesterUserId) : IRequest<Result>;

public class MarkAllNotificationsAsReadCommandHandler : IRequestHandler<MarkAllNotificationsAsReadCommand, Result>
{
    private readonly INotificationRepository _notificationRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;

    public MarkAllNotificationsAsReadCommandHandler(
        INotificationRepository notificationRepository,
        IUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider)
    {
        _notificationRepository = notificationRepository;
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(MarkAllNotificationsAsReadCommand request, CancellationToken cancellationToken)
    {
        var unreadNotifications = await _notificationRepository.GetUnreadByUserIdAsync(request.RequesterUserId, cancellationToken);
        foreach (var notification in unreadNotifications)
        {
            notification.MarkAsRead(_dateTimeProvider.UtcNow);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

// 4. DELETE NOTIFICATION
public record DeleteNotificationCommand(
    Guid RequesterUserId,
    Guid NotificationId) : IRequest<Result>;

public class DeleteNotificationCommandHandler : IRequestHandler<DeleteNotificationCommand, Result>
{
    private readonly INotificationRepository _notificationRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteNotificationCommandHandler(
        INotificationRepository notificationRepository,
        IUnitOfWork unitOfWork)
    {
        _notificationRepository = notificationRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(DeleteNotificationCommand request, CancellationToken cancellationToken)
    {
        var notification = await _notificationRepository.GetByIdAsync(new NotificationId(request.NotificationId), cancellationToken);
        if (notification == null)
            return Result.Failure(Error.NotFound("Notification.NotFound", "Notification not found."));

        if (notification.UserId != request.RequesterUserId)
            return Result.Failure(Error.Forbidden("Notification.Forbidden", "Not authorized to delete this notification."));

        _notificationRepository.Remove(notification);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

// 5. UPDATE NOTIFICATION PREFERENCES
public record UpdateNotificationPreferencesCommand(
    Guid RequesterUserId,
    bool AllowOrderNotifications,
    bool AllowMarketingNotifications,
    bool EmailEnabled,
    bool InAppEnabled) : IRequest<Result<UserNotificationPreferenceDto>>;

public class UpdateNotificationPreferencesCommandHandler : IRequestHandler<UpdateNotificationPreferencesCommand, Result<UserNotificationPreferenceDto>>
{
    private readonly IUserNotificationPreferenceRepository _preferenceRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;

    public UpdateNotificationPreferencesCommandHandler(
        IUserNotificationPreferenceRepository preferenceRepository,
        IUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider)
    {
        _preferenceRepository = preferenceRepository;
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<UserNotificationPreferenceDto>> Handle(UpdateNotificationPreferencesCommand request, CancellationToken cancellationToken)
    {
        var pref = await _preferenceRepository.GetByUserIdAsync(request.RequesterUserId, cancellationToken);
        if (pref == null)
        {
            pref = UserNotificationPreference.CreateDefault(request.RequesterUserId, _dateTimeProvider.UtcNow);
            await _preferenceRepository.AddAsync(pref, cancellationToken);
        }

        pref.Update(
            request.AllowOrderNotifications,
            request.AllowMarketingNotifications,
            request.EmailEnabled,
            request.InAppEnabled,
            _dateTimeProvider.UtcNow);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new UserNotificationPreferenceDto(
            pref.UserId,
            pref.AllowOrderNotifications,
            pref.AllowMarketingNotifications,
            pref.EmailEnabled,
            pref.InAppEnabled);
    }
}

// 6. PROCESS INTEGRATION EVENT NOTIFICATION (INBOX DEDUPLICATION)
public record ProcessIntegrationEventNotificationCommand(
    string ConsumerName,
    string EventId,
    Guid UserId,
    Guid OrderId,
    NotificationType Type,
    string Title,
    string Body,
    string? DataJson) : IRequest<Result>;

public class ProcessIntegrationEventNotificationCommandHandler : IRequestHandler<ProcessIntegrationEventNotificationCommand, Result>
{
    private readonly IProcessedIntegrationEventRepository _processedEventRepository;
    private readonly INotificationRepository _notificationRepository;
    private readonly IRealTimeNotificationPublisher _realTimePublisher;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;

    public ProcessIntegrationEventNotificationCommandHandler(
        IProcessedIntegrationEventRepository processedEventRepository,
        INotificationRepository notificationRepository,
        IRealTimeNotificationPublisher realTimePublisher,
        IUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider)
    {
        _processedEventRepository = processedEventRepository;
        _notificationRepository = notificationRepository;
        _realTimePublisher = realTimePublisher;
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(ProcessIntegrationEventNotificationCommand request, CancellationToken cancellationToken)
    {
        var isProcessed = await _processedEventRepository.IsEventProcessedAsync(request.ConsumerName, request.EventId, cancellationToken);
        if (isProcessed)
        {
            // Inbox deduplication: Duplicate integration event safely ignored!
            return Result.Success();
        }

        var notificationId = NotificationId.New();
        var notificationResult = Notification.Create(
            notificationId,
            request.UserId,
            request.Type,
            request.Title,
            request.Body,
            request.DataJson,
            NotificationChannel.InApp,
            NotificationPriority.High,
            _dateTimeProvider.UtcNow);

        if (notificationResult.IsFailure)
            return notificationResult;

        var notification = notificationResult.Value;
        await _notificationRepository.AddAsync(notification, cancellationToken);

        var processedEvent = new ProcessedIntegrationEvent(Guid.NewGuid(), request.ConsumerName, request.EventId, _dateTimeProvider.UtcNow);
        await _processedEventRepository.AddAsync(processedEvent, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Broadcast live real-time update via SignalR hub
        await _realTimePublisher.PublishOrderNotificationAsync(
            request.UserId,
            request.OrderId,
            request.Type.ToString(),
            new { NotificationId = notification.Id.Value, request.Title, request.Body, request.DataJson },
            cancellationToken);

        return Result.Success();
    }
}
