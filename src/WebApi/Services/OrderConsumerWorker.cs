using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Confluent.Kafka;
using Domain.Services;
using Domain.Enums;
using Domain.Factories;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System;

namespace WebApi.Services  // Or your chosen namespace
{
    public class OrderConsumerWorker : BackgroundService
    {
        private readonly ILogger<OrderConsumerWorker> _logger;
        private readonly MatchingEngine _engine;
        private readonly IConsumer<Null, string> _consumer;

        public OrderConsumerWorker(ILogger<OrderConsumerWorker> logger, MatchingEngine engine)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _engine = engine ?? throw new ArgumentNullException(nameof(engine));

            var config = new ConsumerConfig
            {
                BootstrapServers = "host.docker.internal:9092",
                GroupId = "engine-group",
                AutoOffsetReset = AutoOffsetReset.Earliest,
                SecurityProtocol = SecurityProtocol.Plaintext
            };

            _consumer = new ConsumerBuilder<Null, string>(config).Build();
            _consumer.Subscribe("orders");
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("OrderConsumerWorker started.");
            while (!stoppingToken.IsCancellationRequested)
            {

                await Task.Yield(); 
                try
                {
                    var result = _consumer.Consume(stoppingToken);
                    var json = result.Message.Value;
                    _logger.LogInformation("Received Kafka message: {Message}", json);
                    var orderMsg = JsonSerializer.Deserialize<OrderMessage>(json);
                    if (orderMsg != null)
                    {
                        var side = orderMsg.Side.Equals("buy", StringComparison.OrdinalIgnoreCase)
                            ? OrderSide.Buy
                            : OrderSide.Sell;
                        var order = OrderFactory.CreateLimitOrder(
                            orderMsg.InstrumentId,
                            side,
                            orderMsg.Price,
                            orderMsg.Quantity
                        );
                        _engine.Process(order);
                        _logger.LogInformation("Processed order: {OrderId}", order.OrderId);
                    }
                }
                catch (ConsumeException ex)
                {
                    _logger.LogError(ex, "Error consuming message from Kafka.");
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unexpected error in OrderConsumerWorker.");
                }
            }
            _logger.LogInformation("OrderConsumerWorker stopping.");
        }

        public override void Dispose()
        {
            _consumer.Close();
            _consumer.Dispose();
            base.Dispose();
        }
    }
}
