using Domain.Enums;

namespace API.DTOs
{
    public class CreateMarketOrderDto
    {
        public string InstrumentId { get; set; }
        public OrderSide Side { get; set; }  // "Buy" or "Sell"
        public decimal Quantity { get; set; }
    }
}
