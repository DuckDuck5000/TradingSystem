using System;

namespace Domain.Entities
{
    public class Trade
    {
        // A trade often represents a matched transaction between two orders
        public Guid TradeId { get; private set; }
        public Guid BuyOrderId { get; private set; }
        public Guid SellOrderId { get; private set; }
        public string InstrumentId { get; private set; }
        public decimal Price { get; private set; }
        public decimal Quantity { get; private set; }
        public DateTime ExecutedAt { get; private set; }

        // Private or internal constructor
        private Trade() { }

        internal static Trade Create(
            Guid buyOrderId,
            Guid sellOrderId,
            string instrumentId,
            decimal price,
            decimal quantity,
            DateTime executedAt)
        {
            return new Trade
            {
                TradeId = Guid.NewGuid(),
                BuyOrderId = buyOrderId,
                SellOrderId = sellOrderId,
                InstrumentId = instrumentId,
                Price = price,
                Quantity = quantity,
                ExecutedAt = executedAt
            };
        }
    }
}
