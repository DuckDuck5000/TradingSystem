using Microsoft.AspNetCore.Mvc;
using Confluent.Kafka;
using System.Text.Json;
using Domain.Services; 
using API.DTOs;
using Domain.Enums;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrdersController : ControllerBase
    {
        private readonly IProducer<Null, string> _producer;
        private readonly MatchingEngine _matchingEngine; // if you're still using it for GET endpoints

        // This constructor requires a Kafka producer and optionally the matching engine
        public OrdersController(
            IProducer<Null, string> producer,
            MatchingEngine matchingEngine // optional if you want to handle GET/cancel here
        )
        {
            _producer = producer;
            _matchingEngine = matchingEngine;
        }

        // ---------------------------
        // 1) Limit Order (produces to Kafka)
        // ---------------------------
        [HttpPost("limit")]
        public async Task<IActionResult> CreateLimitOrder([FromBody] CreateLimitOrderDto dto)
        {
            // Convert DTO -> message object
            var orderMessage = new OrderMessage
            {
                OrderId = Guid.NewGuid(),
                InstrumentId = dto.InstrumentId,
                Side = dto.Side,      // "Buy" or "Sell"
                Price = dto.Price,
                Quantity = dto.Quantity
            };

            // Serialize to JSON
            var json = JsonSerializer.Serialize(orderMessage);

            // Produce the message to "orders" topic in Kafka
            await _producer.ProduceAsync("orders", new Message<Null, string> { Value = json });

            // Return a simple acknowledgment
            return Ok(new {
                message = "Limit order submitted to Kafka",
                orderId = orderMessage.OrderId
            });
        }

        // ---------------------------
        // 2) Market Order (produces to Kafka)
        // ---------------------------
        [HttpPost("market")]
        public async Task<IActionResult> CreateMarketOrder([FromBody] CreateMarketOrderDto dto)
        {
            // Convert DTO -> message object
            var orderMessage = new OrderMessage
            {
                OrderId = Guid.NewGuid(),
                InstrumentId = dto.InstrumentId,
                Side = dto.Side,      // "Buy" or "Sell"
                Price = 0, 
                Quantity = dto.Quantity
            };

            var json = JsonSerializer.Serialize(orderMessage);

            // Produce the message to "orders" topic
            await _producer.ProduceAsync("orders", new Message<Null, string> { Value = json });


            return Ok(new {
                message = "Market order submitted to Kafka",
                orderId = orderMessage.OrderId
            });
        }

        // ---------------------------
        // 3) GET an order by ID
        //     - If you're still using the in-memory matching engine
        // ---------------------------
        [HttpGet("{orderId}")]
        public IActionResult GetOrder(Guid orderId)
        {
            // This only works if your matching engine is in the same process
            // and you're storing the orders in memory.
            var order = _matchingEngine.GetOrder(orderId);
            if (order == null) return NotFound();

            return Ok(new {
                order.OrderId,
                order.InstrumentId,
                order.Status,
                order.Price,
                order.Quantity,
                order.FilledQuantity
            });
        }

        // ---------------------------
        // 4) Cancel an Order
        //     - This still calls matchingEngine directly
        //     - In a fully decoupled approach, you'd produce a "Cancel" event to Kafka
        // ---------------------------
        [HttpPost("{orderId}/cancel")]
        public IActionResult CancelOrder(Guid orderId)
        {
            var order = _matchingEngine.GetOrder(orderId);
            if (order == null) return NotFound();

            if (order.Status == OrderStatus.Filled || order.Status == OrderStatus.Canceled)
            {
                return BadRequest(new { message = "Order is already filled or canceled." });
            }

            // Should be on kafka too.....
            _matchingEngine.CancelOrder(orderId);

            return Ok(new { message = "Order canceled.", orderId });
        }

        // ---------------------------
        // 5) Get Order Book
        // ---------------------------
        [HttpGet("orderbook/{instrumentId}")]
        public IActionResult GetOrderBook(string instrumentId)
        {
            var snapshot = _matchingEngine.GetOrderBookSnapshot(instrumentId);
            if (snapshot == null) return NotFound("Instrument not found");
            return Ok(snapshot);
        }

        // ---------------------------
        // 6) Get Trades
        // ---------------------------
        [HttpGet("trades")]
        public IActionResult GetTrades(string instrumentId)
        {
            var trades = _matchingEngine.GetAllTrades(instrumentId);
            return Ok(trades.Select(t => new {
                t.TradeId,
                t.BuyOrderId,
                t.SellOrderId,
                t.InstrumentId,
                t.Price,
                t.Quantity,
                t.ExecutedAt
            }));
        }
    }

    public class CreateLimitOrderDto
    {
        public string InstrumentId { get; set; }
        public string Side { get; set; }  // "Buy" or "Sell"
        public decimal Price { get; set; }
        public decimal Quantity { get; set; }
    }

    public class CreateMarketOrderDto
    {
        public string InstrumentId { get; set; }
        public string Side { get; set; }  // "Buy" or "Sell"
        public decimal Quantity { get; set; }
    }

    // The message shape for Kafka
    public class OrderMessage
    {
        public Guid OrderId { get; set; }
        public string InstrumentId { get; set; }
        public string Side { get; set; }
        public decimal Price { get; set; }
        public decimal Quantity { get; set; }
    }
}
