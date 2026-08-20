using FluentValidation;
using FraudEngine.Api.Dtos;

namespace FraudEngine.Api.Validators
{
    /// <summary>
    /// Validates <see cref="TransactionRequest"/> beyond what data annotations alone
    /// can express (format checks, enum membership).
    /// </summary>
    public class TransactionRequestValidator : AbstractValidator<TransactionRequest>
    {
        public TransactionRequestValidator()
        {
            RuleFor(x => x.AccountId)
                .NotEmpty();

            RuleFor(x => x.Amount)
                .GreaterThan(0);

            RuleFor(x => x.Currency)
                .Matches("^[A-Z]{3}$")
                .WithMessage("Currency must be a 3-letter ISO 4217 code (e.g. 'ZAR').");

            RuleFor(x => x.CountryCode)
                .Matches("^[A-Z]{2}$")
                .WithMessage("CountryCode must be a 2-letter ISO 3166-1 alpha-2 code (e.g. 'ZA').");

            RuleFor(x => x.Category)
                .IsInEnum();
        }
    }
}
