using System;
using Domain.Entities;
using Domain.Enums;

namespace Domain.Factories
{
    public static class OrderFactory
    {
        public static Order CreateLimitOrder(
            string instrumentId,
            OrderSide side,
            decimal price,
            decimal quantity)
        {
            // Basic validation
            if (string.IsNullOrWhiteSpace(instrumentId))
                throw new ArgumentException("Instrument ID cannot be empty.", nameof(instrumentId));
            if (price <= 0)
                throw new ArgumentException("Price must be > 0 for a limit order.", nameof(price));
            if (quantity <= 0)
                throw new ArgumentException("Quantity must be > 0.", nameof(quantity));

            // Construct via the internal static Create method
            return Order.Create(
                Guid.NewGuid(),
                instrumentId,
                side,
                OrderType.Limit,
                price,
                quantity,
                DateTime.UtcNow
            );
        }

        public static Order CreateMarketOrder(
            string instrumentId,
            OrderSide side,
            decimal quantity)
        {
            if (string.IsNullOrWhiteSpace(instrumentId))
                throw new ArgumentException("Instrument ID cannot be empty.", nameof(instrumentId));
            if (quantity <= 0)
                throw new ArgumentException("Quantity must be > 0.", nameof(quantity));

            // Price is 0 (or null) for market orders
            return Order.Create(
                Guid.NewGuid(),
                instrumentId,
                side,
                OrderType.Market,
                0,
                quantity,
                DateTime.UtcNow
            );
        }
        
    }
}
