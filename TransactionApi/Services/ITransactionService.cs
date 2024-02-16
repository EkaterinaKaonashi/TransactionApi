using TransactionApi.API.Common.Models;

namespace TransactionApi.API.Services
{
    public interface ITransactionService
    {
        public Task CreateTransactionAsync(Transaction transaction);
    }
}