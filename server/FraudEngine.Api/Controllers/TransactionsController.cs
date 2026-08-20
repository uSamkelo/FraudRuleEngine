using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using FraudEngine.Api.Dtos;
using FraudEngine.Core.Models;
using FraudEngine.Core.Repositories;
using FraudEngine.Core.Rules;
using System.Linq;
using System.Collections.Generic;

namespace FraudEngine.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TransactionsController : ControllerBase
    {
        // Sane bounds for the page/pageSize query params used by GetAll below -
        // clamped rather than validated, so an out-of-range value (e.g.
        // ?pageSize=-1 or an overflow-prone ?page=99999999999) just gets coerced
        // into range instead of surfacing as a 500 from the database (negative
        // LIMIT, OFFSET overflow, etc.). MaxPage is set far higher than any real
        // result set could page through, purely so (page - 1) * pageSize can
        // never overflow int math downstream.
        private const int MinPage = 1;
        private const int MaxPage = 1_000_000;
        private const int MinPageSize = 1;
        private const int MaxPageSize = 100;

        private readonly IRepository _repo;
        private readonly RulesEngine _engine;

        public TransactionsController(IRepository repo, RulesEngine engine)
        {
            _repo = repo;
            _engine = engine;
        }

        // POST api/transactions
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] TransactionRequest request)
        {
            var transaction = new TransactionEvent
            {
                Id = Guid.NewGuid(),
                Timestamp = DateTimeOffset.UtcNow,
                AccountId = request.AccountId,
                Amount = request.Amount,
                Category = request.Category,
                Currency = request.Currency,
                Channel = request.Channel,
                CountryCode = request.CountryCode,
                MerchantId = request.MerchantId,
                MerchantName = request.MerchantName,
                MerchantCategoryCode = request.MerchantCategoryCode,
                DeviceId = request.DeviceId,
                IpAddress = request.IpAddress,
                CardLast4 = request.CardLast4,
                Metadata = request.Metadata
            };

            await _repo.AddTransactionAsync(transaction);

            // Evaluate rules
            var alerts = await _engine.EvaluateAsync(transaction);
            var saved = new List<FraudAlert>();
            foreach (var a in alerts)
            {
                await _repo.AddAlertAsync(a);
                saved.Add(a);
            }

            var body = new
            {
                transaction = transaction.ToResponse(),
                alerts = saved.Select(a => a.ToResponse())
            };

            return CreatedAtAction(nameof(GetById), new { id = transaction.Id }, body);
        }

        // GET api/transactions/{id}
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var tx = await _repo.GetTransactionAsync(id);
            if (tx == null) return NotFound();
            return Ok(tx.ToResponse());
        }

        // GET api/transactions?accountId=&category=&from=&to=&page=1&pageSize=20
        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] string? accountId,
            [FromQuery] TransactionCategory? category,
            [FromQuery] DateTimeOffset? from,
            [FromQuery] DateTimeOffset? to,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            page = Math.Clamp(page, MinPage, MaxPage);
            pageSize = Math.Clamp(pageSize, MinPageSize, MaxPageSize);

            var (items, totalCount) = await _repo.GetTransactionsAsync(accountId, category, from, to, page, pageSize);

            var result = new PagedResult<TransactionResponse>
            {
                Items = items.Select(t => t.ToResponse()),
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount
            };

            return Ok(result);
        }

        // GET api/transactions/alerts?status=Open
        [HttpGet("alerts")]
        public async Task<IActionResult> GetAlerts([FromQuery] AlertStatus? status)
        {
            var alerts = await _repo.GetAlertsAsync(status);
            return Ok(alerts.Select(a => a.ToResponse()));
        }
    }
}
