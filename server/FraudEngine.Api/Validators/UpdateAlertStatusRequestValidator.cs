using FluentValidation;
using FraudEngine.Api.Dtos;

namespace FraudEngine.Api.Validators
{
    /// <summary>
    /// Validates <see cref="UpdateAlertStatusRequest"/> beyond what data annotations
    /// alone can express. <c>[Required]</c> on a non-nullable enum is a no-op (there's
    /// no "missing" value to reject), so without an explicit <c>IsInEnum</c> check an
    /// out-of-range integer like <c>{"status": 99}</c> would pass model validation and
    /// get persisted as-is.
    /// </summary>
    public class UpdateAlertStatusRequestValidator : AbstractValidator<UpdateAlertStatusRequest>
    {
        public UpdateAlertStatusRequestValidator()
        {
            RuleFor(x => x.Status)
                .IsInEnum();
        }
    }
}
