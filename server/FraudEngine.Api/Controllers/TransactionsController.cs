using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
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
        private readonly IRepository _repo;
        private readonly RulesEngine _engine;

        public TransactionsController(IRepository repo, RulesEngine engine)
        {
            _repo = repo;
            _engine = engine;
        }

        // POST api/transactions
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] TransactionEvent input)
        {
            if (input == null)
                return BadRequest("Invalid payload");

            // Ensure an Id and timestamp
            if (input.Id == Guid.Empty) input.Id = Guid.NewGuid();
            if (input.Timestamp == default) input.Timestamp = DateTimeOffset.UtcNow;

            await _repo.AddTransactionAsync(input);

            // Evaluate rules
            var alerts = await _engine.EvaluateAsync(input);
            var saved = new List<FraudAlert>();
            foreach (var a in alerts)
            {
                await _repo.AddAlertAsync(a);
                saved.Add(a);
            }

            return CreatedAtAction(nameof(GetById), new { id = input.Id }, new { transaction = input, alerts = saved });
        }

        // GET api/transactions/{id}
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var tx = await _repo.GetTransactionAsync(id);
            if (tx == null) return NotFound();
            return Ok(tx);
        }

        // GET api/transactions
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            // Not implemented listing all transactions for brevity
            return BadRequest("Listing all transactions is not supported in this demo");
        }

        // GET api/transactions/alerts
        [HttpGet("alerts")]
        public async Task<IActionResult> GetAlerts()
        {
            var alerts = await _repo.GetAlertsAsync();
            return Ok(alerts);
        }
    }
}
