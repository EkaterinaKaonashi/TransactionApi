namespace TransactionApi.API.Common.Models
{
    public class ExchangeRate
    {
        public Guid Id { get; set; }

        public string Name { get; set; }

        public decimal? Rate { get; set; }

        public string Code { get; set; }

        public DateTime? CreatedOn { get; set; }
    }
}
