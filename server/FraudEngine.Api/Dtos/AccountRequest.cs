using System.ComponentModel.DataAnnotations;
using FraudEngine.Core.Models;

namespace FraudEngine.Api.Dtos
{
    /// <summary>
    /// The shape callers POST to create an account. Deliberately separate from
    /// <see cref="Account"/> so the EF entity (and its annotations, keys, etc.) never
    /// leaks into the public API contract.
    ///
    /// Deliberately has no <c>CreatedAt</c> field - it's always set server-side to
    /// <c>DateTimeOffset.UtcNow</c> at creation time. Letting a caller supply/backdate
    /// it would let them bypass <c>AccountAgeRule</c> by construction, which defeats
    /// the rule's purpose.
    /// </summary>
    public class AccountRequest
    {
        [Required]
        public required string AccountId { get; set; }

        public AccountType AccountType { get; set; }

        public RiskTier RiskTier { get; set; }

        [Required]
        public required string OwnerId { get; set; }

        public string DefaultCountryCode { get; set; } = "ZA";
    }
}
