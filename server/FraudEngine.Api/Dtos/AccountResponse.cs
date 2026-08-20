using System;
using FraudEngine.Core.Models;

namespace FraudEngine.Api.Dtos
{
    /// <summary>
    /// What GET/POST endpoints return for an account. Mirrors <see cref="Account"/>
    /// without exposing EF Core annotations as part of the public API contract.
    /// </summary>
    public class AccountResponse
    {
        public required string AccountId { get; set; }

        public AccountType AccountType { get; set; }

        public RiskTier RiskTier { get; set; }

        public DateTimeOffset CreatedAt { get; set; }

        public required string OwnerId { get; set; }

        public required string DefaultCountryCode { get; set; }
    }
}
