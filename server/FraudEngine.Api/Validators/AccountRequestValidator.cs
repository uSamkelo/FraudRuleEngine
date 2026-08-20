using FluentValidation;
using FraudEngine.Api.Dtos;

namespace FraudEngine.Api.Validators
{
    /// <summary>
    /// Validates <see cref="AccountRequest"/> beyond what data annotations alone
    /// can express (format checks, enum membership).
    /// </summary>
    public class AccountRequestValidator : AbstractValidator<AccountRequest>
    {
        public AccountRequestValidator()
        {
            RuleFor(x => x.AccountId)
                .NotEmpty();

            RuleFor(x => x.OwnerId)
                .NotEmpty();

            // NotEmpty must come before Matches: Matches alone treats null/empty as
            // valid (a prior bug in TransactionRequestValidator showed this lets a bad
            // value reach the database as an unhandled 500 instead of a 400).
            RuleFor(x => x.DefaultCountryCode)
                .NotEmpty()
                .Matches("^[A-Z]{2}$")
                .WithMessage("DefaultCountryCode must be a 2-letter ISO 3166-1 alpha-2 code (e.g. 'ZA').");

            RuleFor(x => x.AccountType)
                .IsInEnum();

            RuleFor(x => x.RiskTier)
                .IsInEnum();
        }
    }
}
