using FluentValidation;
using TransactionApi.API.Common.Models;

namespace TransactionApi.API.Common.Validators
{
    public class TransactionValidator : AbstractValidator<Transaction>
    {
        private readonly List<string> allowedCurrencies = new List<string> { "USD", "RUB", "EUR", "CNY" };

        public TransactionValidator()
        {
            RuleFor(t => t.Id).NotEmpty().WithMessage("Id is required.");

            RuleFor(t => t.ClientId).NotEmpty().WithMessage("ClientId is required.")
            .Length(10, 10).WithMessage("ClientId length must be 10.")
            .Matches("^[a-zA-Z0-9#]*$").WithMessage("ClientId should only contain letters, numbers, and #.")
            .Must(x => x.Distinct().Count() == x.Length).WithMessage("ClientId should contain unique characters.")
            .Matches("[0-9]+").Matches("[A-Z].*[A-Z]")
            .Matches("[0-9].*?[0-9].*?[0-9].*?[0-9].*?[0-9]").WithMessage("ClientId must contain 5 numbers and at least 2 upper case letters.");

            RuleFor(t => t.Amount).GreaterThan(0).WithMessage("Amount should be greater than 0.");
            RuleFor(t => t.Currency).Must(cur => allowedCurrencies.Contains(cur.ToUpper()))
                .WithMessage($"Currency must be one of the following values: {string.Join(", ", allowedCurrencies)}");
            RuleFor(t => t.CreatedOn).NotEmpty().WithMessage("CreatedOn is required.");

        }
    }
}
