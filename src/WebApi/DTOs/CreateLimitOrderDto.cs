using Domain.Enums;

namespace API.DTOs
{
    public class CreateLimitOrderDto
    {
        public string InstrumentId { get; set; }
        public OrderSide Side { get; set; }  // "Buy" or "Sell"
        public decimal Price { get; set; }
        public decimal Quantity { get; set; }
    }
}
