using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using FraudEngine.Api.Dtos;
using FraudEngine.Core.Models;
using FraudEngine.Core.Repositories;

namespace FraudEngine.Api.Controllers
{
    /// <summary>
    /// Minimal write path for <see cref="Account"/> records: create + lookup-by-id
    /// only. Without this, accounts only ever exist via the Development-only
    /// DbSeeder, which leaves UnusualCountryRule, NightTimeWithdrawalRule, and
    /// AccountAgeRule permanently inert in a real deployment (they all early-return
    /// when GetAccountAsync finds nothing). Update/delete/list-all are deliberately
    /// out of scope - not needed to close that gap.
    /// </summary>
    [ApiController]
    [Route("api/accounts")]
    public class AccountsController : ControllerBase
    {
        private readonly IRepository _repo;

        public AccountsController(IRepository repo)
        {
            _repo = repo;
        }

        // POST api/accounts
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] AccountRequest request)
        {
            // Check for a duplicate AccountId up front so a unique-constraint
            // violation never surfaces as an unhandled 500 from the database -
            // it's a normal, expected 409 instead.
            var existing = await _repo.GetAccountAsync(request.AccountId);
            if (existing != null)
            {
                return Conflict($"An account with AccountId '{request.AccountId}' already exists.");
            }

            var account = new Account
            {
                AccountId = request.AccountId,
                AccountType = request.AccountType,
                RiskTier = request.RiskTier,
                CreatedAt = DateTimeOffset.UtcNow,
                OwnerId = request.OwnerId,
                DefaultCountryCode = request.DefaultCountryCode
            };

            await _repo.AddAccountAsync(account);

            return CreatedAtAction(nameof(GetById), new { id = account.AccountId }, account.ToResponse());
        }

        // GET api/accounts/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            // GetAccountAsync's signature is non-nullable, but the EF implementation
            // can genuinely return null at runtime (same intentional pattern as
            // GetTransactionAsync) - so null-check it anyway despite the declared type.
            var account = await _repo.GetAccountAsync(id);
            if (account == null) return NotFound();

            return Ok(account.ToResponse());
        }
    }
}
