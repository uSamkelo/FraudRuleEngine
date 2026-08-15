using System.Threading.Tasks;
using FraudEngine.Core.Models;

namespace FraudEngine.Core.Rules
{
    public interface IFraudRule
    {
        string Name { get; }
        Task<FraudAlert[]> EvaluateAsync(TransactionEvent tx);
    }
}
