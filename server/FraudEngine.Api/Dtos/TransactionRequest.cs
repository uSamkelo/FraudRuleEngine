using System.ComponentModel.DataAnnotations;
using FraudEngine.Core.Models;

namespace FraudEngine.Api.Dtos
{
    /// <summary>
    /// The shape callers POST to create a transaction. Deliberately separate from
    /// <see cref="TransactionEvent"/> so the EF entity (and its annotations, keys,
    /// etc.) never leaks into the public API contract.
    /// </summary>
    public class TransactionRequest
    {
        [Required]
        public required string AccountId { get; set; }

        [Range(0.01, double.MaxValue)]
        public decimal Amount { get; set; }

        public TransactionCategory Category { get; set; }

        public string Currency { get; set; } = "ZAR";

        public Channel Channel { get; set; } = Channel.Unknown;

        public string CountryCode { get; set; } = "ZA";

        public string? MerchantId { get; set; }

        public string? MerchantName { get; set; }

        public string? MerchantCategoryCode { get; set; }

        public string? DeviceId { get; set; }

        public string? IpAddress { get; set; }

        public string? CardLast4 { get; set; }

        public string? Metadata { get; set; }
    }
}
