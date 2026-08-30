using NextDrop.Modules.Payments.Application.Abstractions;
using NextDrop.Modules.Payments.Domain.Aggregates;
using NextDrop.SharedKernel.Common;

namespace NextDrop.Modules.Payments.Infrastructure.Services;

public class FakePaymentProvider : IPaymentProvider
{
    public Task<Result<PaymentProviderResultDto>> CreatePaymentAsync(Payment payment, CancellationToken cancellationToken = default)
    {
        var providerPaymentId = $"fake_pay_{payment.Id.Value:N}";
        var providerTxId = $"fake_tx_{Guid.NewGuid():N}";
        var result = new PaymentProviderResultDto(providerPaymentId, providerTxId, true, null);
        return Task.FromResult(Result.Success(result));
    }

    public Task<Result<PaymentProviderResultDto>> ConfirmPaymentAsync(Payment payment, CancellationToken cancellationToken = default)
    {
        var providerPaymentId = payment.ProviderPaymentId ?? $"fake_pay_{payment.Id.Value:N}";
        var providerTxId = $"fake_tx_{Guid.NewGuid():N}";
        var result = new PaymentProviderResultDto(providerPaymentId, providerTxId, true, null);
        return Task.FromResult(Result.Success(result));
    }

    public Task<Result<PaymentProviderResultDto>> RefundPaymentAsync(Payment payment, Refund refund, CancellationToken cancellationToken = default)
    {
        var providerPaymentId = payment.ProviderPaymentId ?? $"fake_pay_{payment.Id.Value:N}";
        var providerRefundId = $"fake_ref_{refund.Id.Value:N}";
        var result = new PaymentProviderResultDto(providerPaymentId, providerRefundId, true, null);
        return Task.FromResult(Result.Success(result));
    }

    public Task<bool> VerifyWebhookSignatureAsync(string provider, string signature, string payload, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(signature) || signature.Equals("invalid", StringComparison.OrdinalIgnoreCase))
            return Task.FromResult(false);

        return Task.FromResult(true);
    }
}
