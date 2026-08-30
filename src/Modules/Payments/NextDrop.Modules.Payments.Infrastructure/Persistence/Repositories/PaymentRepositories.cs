using Microsoft.EntityFrameworkCore;
using NextDrop.Infrastructure.Persistence;
using NextDrop.Modules.Orders.Domain.ValueObjects;
using NextDrop.Modules.Payments.Application.Abstractions;
using NextDrop.Modules.Payments.Domain.Aggregates;
using NextDrop.Modules.Payments.Domain.Enums;
using NextDrop.Modules.Payments.Domain.ValueObjects;

namespace NextDrop.Modules.Payments.Infrastructure.Persistence.Repositories;

public class PaymentRepository : IPaymentRepository
{
    private readonly NextDropDbContext _dbContext;

    public PaymentRepository(NextDropDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Payment?> GetByIdAsync(PaymentId id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Payments
            .Include(p => p.Transactions)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<Payment?> GetByOrderIdAsync(OrderId orderId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Payments
            .Include(p => p.Transactions)
            .FirstOrDefaultAsync(p => p.OrderId == orderId, cancellationToken);
    }

    public async Task AddAsync(Payment payment, CancellationToken cancellationToken = default)
    {
        await _dbContext.Payments.AddAsync(payment, cancellationToken);
    }
}

public class RefundRepository : IRefundRepository
{
    private readonly NextDropDbContext _dbContext;

    public RefundRepository(NextDropDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Refund?> GetByIdAsync(RefundId id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Refunds
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    public async Task<List<Refund>> GetByPaymentIdAsync(PaymentId paymentId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Refunds
            .Where(r => r.PaymentId == paymentId)
            .ToListAsync(cancellationToken);
    }

    public async Task<decimal> GetTotalSuccessfulRefundsForPaymentAsync(PaymentId paymentId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Refunds
            .Where(r => r.PaymentId == paymentId && r.Status == RefundStatus.Completed)
            .SumAsync(r => r.Amount, cancellationToken);
    }

    public async Task AddAsync(Refund refund, CancellationToken cancellationToken = default)
    {
        await _dbContext.Refunds.AddAsync(refund, cancellationToken);
    }
}

public class WebhookEventRepository : IWebhookEventRepository
{
    private readonly NextDropDbContext _dbContext;

    public WebhookEventRepository(NextDropDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<WebhookEvent?> GetByProviderEventIdAsync(string provider, string providerEventId, CancellationToken cancellationToken = default)
    {
        var prov = provider.Trim().ToLowerInvariant();
        var evtId = providerEventId.Trim();
        return await _dbContext.WebhookEvents
            .FirstOrDefaultAsync(w => w.Provider == prov && w.ProviderEventId == evtId, cancellationToken);
    }

    public async Task AddAsync(WebhookEvent webhookEvent, CancellationToken cancellationToken = default)
    {
        await _dbContext.WebhookEvents.AddAsync(webhookEvent, cancellationToken);
    }
}
