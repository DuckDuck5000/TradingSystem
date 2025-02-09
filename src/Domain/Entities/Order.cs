using System;
using Domain.Enums;

namespace Domain.Entities
{
    public class Order
    {
        // Private constructor to force creation via a factory or a static create method
        private Order() { }

        public Guid OrderId { get; private set; }
        public string InstrumentId { get; private set; }
        public OrderSide Side { get; private set; }
        public OrderType OrderType { get; private set; }
        public decimal Price { get; private set; }       // 0 for a market order
        public decimal Quantity { get; private set; }
        public decimal FilledQuantity { get; private set; } // to track partial fills
        public OrderStatus Status { get; private set; }
        public DateTime CreatedAt { get; private set; }

        // Internal methods to manage the order lifecycle
        internal void Fill(decimal fillQuantity)
        {
            FilledQuantity += fillQuantity;
            if (FilledQuantity >= Quantity)
            {
                Status = OrderStatus.Filled;
            }
            else
            {
                Status = OrderStatus.PartiallyFilled;
            }
        }

        internal void Cancel()
        {
            Status = OrderStatus.Canceled;
        }

        // Friend or factory method to allow creation
        internal static Order Create(
            Guid orderId, 
            string instrumentId, 
            OrderSide side, 
            OrderType orderType, 
            decimal price, 
            decimal quantity,
            DateTime createdAt)
        {
            return new Order
            {
                OrderId = orderId,
                InstrumentId = instrumentId,
                Side = side,
                OrderType = orderType,
                Price = price,
                Quantity = quantity,
                FilledQuantity = 0,
                Status = OrderStatus.New,
                CreatedAt = createdAt
            };
        }
    }
}
