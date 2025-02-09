using System;
using System.Collections.Generic;
using System.Linq;
using Domain.Entities;
using Domain.Enums;

namespace Domain.Services
{
    public class MatchingEngine
    {
        // We'll keep separate order books per instrument (InstrumentId -> OrderBook).
        private readonly Dictionary<string, OrderBook> _orderBooks 
            = new(StringComparer.OrdinalIgnoreCase);

        // Store executed trades so we can query them later.
        private readonly List<Trade> _executedTrades = new();

        public MatchingEngine()
        {
        }

        /// <summary>
        /// Process an incoming order by matching it against the existing order book.
        /// Returns a list of executed trades.
        /// </summary>
        public List<Trade> Process(Order incomingOrder)
        {
            // Ensure we have an order book for this instrument.
            if (!_orderBooks.ContainsKey(incomingOrder.InstrumentId))
            {
                _orderBooks[incomingOrder.InstrumentId] = new OrderBook(incomingOrder.InstrumentId);
            }

            var book = _orderBooks[incomingOrder.InstrumentId];
            var trades = new List<Trade>();

            if (incomingOrder.Side == OrderSide.Buy)
            {
                trades.AddRange(MatchBuyOrder(incomingOrder, book));
            }
            else
            {
                trades.AddRange(MatchSellOrder(incomingOrder, book));
            }

            // Record executed trades in our global list
            _executedTrades.AddRange(trades);

            // If the order is still not fully filled and not canceled/filled, add to the book
            if (incomingOrder.Quantity > incomingOrder.FilledQuantity &&
                incomingOrder.Status != OrderStatus.Canceled &&
                incomingOrder.Status != OrderStatus.Filled)
            {
                book.AddOrder(incomingOrder);
            }

            return trades;
        }

        // --------------- NEW METHODS for Controller Endpoints ---------------

        /// <summary>
        /// Retrieve an order by ID from any of the order books.
        /// </summary>
        public Order GetOrder(Guid orderId)
        {
            // Search all order books
            foreach (var kvp in _orderBooks)
            {
                var book = kvp.Value;

                // Check among bids
                foreach (var priceLevel in book.Bids.Values)
                {
                    var found = priceLevel.FirstOrDefault(o => o.OrderId == orderId);
                    if (found != null) return found;
                }

                // Check among asks
                foreach (var priceLevel in book.Asks.Values)
                {
                    var found = priceLevel.FirstOrDefault(o => o.OrderId == orderId);
                    if (found != null) return found;
                }
            }
            return null;
        }

        /// <summary>
        /// Cancel an existing order if it is not already filled or canceled.
        /// </summary>
        public void CancelOrder(Guid orderId)
        {
            var order = GetOrder(orderId);
            if (order == null) return;

            if (order.Status == OrderStatus.Filled || order.Status == OrderStatus.Canceled)
                return; // Already final state

            // Mark as canceled
            order.Cancel();

            // Remove from the order book data structure
            RemoveOrderFromBook(order);
        }

        /// <summary>
        /// Provide an aggregated snapshot of the order book (bids and asks) for a given instrument.
        /// </summary>
        public object GetOrderBookSnapshot(string instrumentId)
        {
            if (!_orderBooks.TryGetValue(instrumentId, out var book))
            {
                return null; // Instrument not found
            }

            // Bids: sorted descending by price
            var bids = book.Bids.Select(pair => new 
            {
                Price = pair.Key,
                // Sum the *unfilled* quantities
                Quantity = pair.Value.Sum(o => o.Quantity - o.FilledQuantity)
            });

            // Asks: sorted ascending by price
            var asks = book.Asks.Select(pair => new 
            {
                Price = pair.Key,
                Quantity = pair.Value.Sum(o => o.Quantity - o.FilledQuantity)
            });

            return new
            {
                InstrumentId = instrumentId,
                Bids = bids,
                Asks = asks
            };
        }

        /// <summary>
        /// Return a list of all executed trades. If instrumentId is specified, filter by that instrument.
        /// </summary>
        public List<Trade> GetAllTrades(string instrumentId = null)
        {
            if (string.IsNullOrWhiteSpace(instrumentId))
            {
                // Return all trades
                return _executedTrades.ToList();
            }
            else
            {
                // Filter by instrument
                return _executedTrades
                    .Where(t => t.InstrumentId.Equals(instrumentId, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }
        }

        // --------------- PRIVATE HELPER Methods ---------------

        private IEnumerable<Trade> MatchBuyOrder(Order buyOrder, OrderBook book)
        {
            var executedTrades = new List<Trade>();

            // For a BUY, we try matching with the best ASK
            while (buyOrder.Quantity > buyOrder.FilledQuantity && book.HasAsks())
            {
                var bestAskPrice = book.GetBestAskPrice();
                if (buyOrder.OrderType == OrderType.Limit && buyOrder.Price < bestAskPrice)
                {
                    // Buy price too low to match the lowest ask
                    break;
                }

                var askList = book.Asks[bestAskPrice];
                var askOrder = askList[0];  // FIFO

                // Match price = ask price (simple approach)
                var matchPrice = bestAskPrice;

                // Determine how much we can fill
                var buyRemaining = buyOrder.Quantity - buyOrder.FilledQuantity;
                var askRemaining = askOrder.Quantity - askOrder.FilledQuantity;
                var matchedQty = Math.Min(buyRemaining, askRemaining);

                // Create and record the trade
                var trade = Trade.Create(
                    buyOrderId: buyOrder.OrderId,
                    sellOrderId: askOrder.OrderId,
                    instrumentId: buyOrder.InstrumentId,
                    price: matchPrice,
                    quantity: matchedQty,
                    executedAt: DateTime.UtcNow
                );
                executedTrades.Add(trade);

                // Update fill
                buyOrder.Fill(matchedQty);
                askOrder.Fill(matchedQty);

                // If the ask is fully filled, remove it
                if (askOrder.Status == OrderStatus.Filled)
                {
                    askList.RemoveAt(0);
                    if (askList.Count == 0)
                    {
                        book.Asks.Remove(bestAskPrice);
                    }
                }
            }

            return executedTrades;
        }

        private IEnumerable<Trade> MatchSellOrder(Order sellOrder, OrderBook book)
        {
            var executedTrades = new List<Trade>();

            // For a SELL, we try matching with the best BID
            while (sellOrder.Quantity > sellOrder.FilledQuantity && book.HasBids())
            {
                var bestBidPrice = book.GetBestBidPrice();
                if (sellOrder.OrderType == OrderType.Limit && sellOrder.Price > bestBidPrice)
                {
                    // Sell price too high to match highest bid
                    break;
                }

                var bidList = book.Bids[bestBidPrice];
                var bidOrder = bidList[0];  // FIFO

                // Match price = bid price (simple approach)
                var matchPrice = bestBidPrice;

                var sellRemaining = sellOrder.Quantity - sellOrder.FilledQuantity;
                var bidRemaining = bidOrder.Quantity - bidOrder.FilledQuantity;
                var matchedQty = Math.Min(sellRemaining, bidRemaining);

                // Create the trade
                var trade = Trade.Create(
                    buyOrderId: bidOrder.OrderId,
                    sellOrderId: sellOrder.OrderId,
                    instrumentId: sellOrder.InstrumentId,
                    price: matchPrice,
                    quantity: matchedQty,
                    executedAt: DateTime.UtcNow
                );
                executedTrades.Add(trade);

                // Update fill
                sellOrder.Fill(matchedQty);
                bidOrder.Fill(matchedQty);

                // If the bid is fully filled, remove it
                if (bidOrder.Status == OrderStatus.Filled)
                {
                    bidList.RemoveAt(0);
                    if (bidList.Count == 0)
                    {
                        book.Bids.Remove(bestBidPrice);
                    }
                }
            }

            return executedTrades;
        }

        private void RemoveOrderFromBook(Order order)
        {
            if (!_orderBooks.TryGetValue(order.InstrumentId, out var book)) 
                return;

            var dict = order.Side == OrderSide.Buy ? book.Bids : book.Asks;
            if (!dict.TryGetValue(order.Price, out var ordersAtPrice))
                return;

            // Remove the order from the list
            ordersAtPrice.Remove(order);

            // If no more orders at that price, remove the price level
            if (ordersAtPrice.Count == 0)
            {
                dict.Remove(order.Price);
            }
        }
    }

    // Represents the order book for a single instrument
    internal class OrderBook
    {
        public string InstrumentId { get; }
        
        // Bids: sorted descending by price
        public SortedDictionary<decimal, List<Order>> Bids { get; }
        
        // Asks: sorted ascending by price
        public SortedDictionary<decimal, List<Order>> Asks { get; }

        public OrderBook(string instrumentId)
        {
            InstrumentId = instrumentId;

            Bids = new SortedDictionary<decimal, List<Order>>(Comparer<decimal>.Create((x, y) => y.CompareTo(x))); 
            // The above comparer flips the sort so that highest price is first for Bids

            Asks = new SortedDictionary<decimal, List<Order>>();
            // Default ascending order for Asks
        }

        public bool HasBids() => Bids.Count > 0;
        public bool HasAsks() => Asks.Count > 0;

        public decimal GetBestBidPrice() => Bids.First().Key;  // highest
        public decimal GetBestAskPrice() => Asks.First().Key;  // lowest

        public void AddOrder(Order order)
        {
            if (order.Side == OrderSide.Buy)
            {
                if (!Bids.ContainsKey(order.Price))
                {
                    Bids[order.Price] = new List<Order>();
                }
                Bids[order.Price].Add(order);
            }
            else
            {
                if (!Asks.ContainsKey(order.Price))
                {
                    Asks[order.Price] = new List<Order>();
                }
                Asks[order.Price].Add(order);
            }
        }
    }
}
