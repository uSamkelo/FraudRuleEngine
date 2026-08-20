using System;
using FraudEngine.Core.Models;

namespace FraudEngine.Api.Dtos
{
    /// <summary>
    /// What GET endpoints return for a transaction. Mirrors <see cref="TransactionEvent"/>
    /// without exposing EF Core annotations as part of the public API contract.
    /// </summary>
    public class TransactionResponse
    {
        public Guid Id { get; set; }

        public DateTimeOffset Timestamp { get; set; }

        public TransactionCategory Category { get; set; }

        public decimal Amount { get; set; }

        public required string AccountId { get; set; }

        public string? Metadata { get; set; }

        public DateTimeOffset? ProcessedAt { get; set; }

        public string Currency { get; set; } = "ZAR";

        public string? MerchantId { get; set; }

        public string? MerchantName { get; set; }

        public string? MerchantCategoryCode { get; set; }

        public Channel Channel { get; set; }

        public string CountryCode { get; set; } = "ZA";

        public string? DeviceId { get; set; }

        public string? IpAddress { get; set; }

        public string? CardLast4 { get; set; }
    }
}
