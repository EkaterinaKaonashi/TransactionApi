using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TransactionApi.API.Common.Models;
using TransactionApi.API.Services;

namespace TransactionApi.API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class TransactionController : ControllerBase
    {
        private readonly IValidator<Transaction> _validator;
        private readonly ITransactionService _transactionService;

        public TransactionController(IValidator<Transaction> validator,
            ITransactionService transactionService)
        {
            _validator = validator;
            _transactionService = transactionService;
        }
        [HttpPost()]
        public async Task<IActionResult> CreateTransaction([FromBody] Transaction model)
        {
            var validationResult = _validator.Validate(model);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors.Select(st => st.ErrorMessage).ToList();
                return StatusCode(StatusCodes.Status400BadRequest, errors);
            }

            await _transactionService.CreateTransactionAsync(model);

            return Ok();
        }
    }
}
