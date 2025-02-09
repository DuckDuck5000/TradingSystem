using Microsoft.AspNetCore.Mvc;
using Domain.Factories;
using Domain.Services;
using API.DTOs;
using Domain.Enums;
using System.Text.Json.Serialization; // Ensure this is added


namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrdersController : ControllerBase
    {
        private readonly MatchingEngine _matchingEngine;

        public OrdersController(MatchingEngine matchingEngine)
        {
            _matchingEngine = matchingEngine;
        }

        [HttpPost("limit")]
        public IActionResult CreateLimitOrder([FromBody] CreateLimitOrderDto dto)
        {

            // 1. Create the domain order via the factory
            var order = OrderFactory.CreateLimitOrder(
                dto.InstrumentId, 
                dto.Side, 
                dto.Price, 
                dto.Quantity
            );

            // 2. Process it in the matching engine
            var trades = _matchingEngine.Process(order);

            // 3. Return a response
            //    Typically you’d return the created order info + any resulting trades
            return Ok(new
            {
                orderId = order.OrderId,
                instrumentId = order.InstrumentId,
                status = order.Status.ToString(),
                trades = trades.Select(t => new 
                {
                    t.TradeId,
                    t.Price,
                    t.Quantity,
                    t.ExecutedAt
                })
            });
        }



        [HttpPost("market")]
        public IActionResult CreateMarketOrder([FromBody] CreateMarketOrderDto dto)
        {

            var order = OrderFactory.CreateMarketOrder(
                dto.InstrumentId, 
                dto.Side, 
                dto.Quantity
            );
            
            var trades = _matchingEngine.Process(order);

            return Ok(new
            {
                orderId = order.OrderId,
                instrumentId = order.InstrumentId,
                status = order.Status.ToString(),
                trades = trades.Select(t => new 
                {
                    t.TradeId,
                    t.Price,
                    t.Quantity,
                    t.ExecutedAt
                })
            });
        }
        

        // Optionally GET an order by ID, though we might not have persistence set up yet
   [HttpGet("{orderId}")]
public IActionResult GetOrder(Guid orderId)
{
    // If you have in-memory orders, you'd query the matching engine’s order book.
    // Or, if you have a repository (IOrderRepository), you'd do something like:
    // var order = _orderRepository.GetById(orderId);
    
    // For a pure in-memory approach, you might have a method in your matching engine:
    var order = _matchingEngine.GetOrder(orderId);

    if (order == null)
        return NotFound();

    return Ok(new {
        order.OrderId,
        order.InstrumentId,
        order.Status,
        order.Price,
        order.Quantity,
        order.FilledQuantity
        // ...
    });
}

   [HttpPost("{orderId}/cancel")]
public IActionResult CancelOrder(Guid orderId)
{
    // You might look up the order in the matching engine or repository:
    var order = _matchingEngine.GetOrder(orderId);

    if (order == null) 
        return NotFound();

    if (order.Status == OrderStatus.Filled || order.Status == OrderStatus.Canceled)
    {
        return BadRequest(new { message = "Order is already filled or canceled." });
    }

    _matchingEngine.CancelOrder(orderId);

    return Ok(new { message = "Order canceled.", orderId = orderId });
}

[HttpGet("orderbook/{instrumentId}")]
public IActionResult GetOrderBook(string instrumentId)
{
    var snapshot = _matchingEngine.GetOrderBookSnapshot(instrumentId);
    if (snapshot == null) 
        return NotFound("Instrument not found");

    return Ok(snapshot);
}

[HttpGet("trades")]
public IActionResult GetTrades(string instrumentId)
{
    // If you store trades in memory or a DB, fetch them here.
    // If instrumentId is provided, filter on that. Otherwise return all.
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
}
