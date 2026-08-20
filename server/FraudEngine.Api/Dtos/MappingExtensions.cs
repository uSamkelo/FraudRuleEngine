using FraudEngine.Core.Models;

namespace FraudEngine.Api.Dtos
{
    /// <summary>
    /// Maps EF entities to their public-facing DTO shapes so controllers never
    /// return <see cref="TransactionEvent"/>/<see cref="FraudAlert"/> directly.
    /// </summary>
    public static class MappingExtensions
    {
        public static TransactionResponse ToResponse(this TransactionEvent tx) => new()
        {
            Id = tx.Id,
            Timestamp = tx.Timestamp,
            Category = tx.Category,
            Amount = tx.Amount,
            AccountId = tx.AccountId,
            Metadata = tx.Metadata,
            ProcessedAt = tx.ProcessedAt,
            Currency = tx.Currency,
            MerchantId = tx.MerchantId,
            MerchantName = tx.MerchantName,
            MerchantCategoryCode = tx.MerchantCategoryCode,
            Channel = tx.Channel,
            CountryCode = tx.CountryCode,
            DeviceId = tx.DeviceId,
            IpAddress = tx.IpAddress,
            CardLast4 = tx.CardLast4
        };

        public static AlertResponse ToResponse(this FraudAlert alert) => new()
        {
            Id = alert.Id,
            TransactionId = alert.TransactionId,
            RuleName = alert.RuleName,
            Reason = alert.Reason,
            Severity = alert.Severity,
            CreatedAt = alert.CreatedAt,
            Status = alert.Status,
            ReviewedAt = alert.ReviewedAt,
            ReviewedBy = alert.ReviewedBy
        };
    }
}
