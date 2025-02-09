using Microsoft.Extensions.Hosting;
using System.Threading;
using System.Threading.Tasks;
using Domain.Services;
using Domain.Factories;
using Confluent.Kafka;
using System.Text.Json;

namespace Consumer
{
    public class OrderConsumerWorker : BackgroundService
    {
        private readonly MatchingEngine _engine;
        private readonly IConsumer<Null, string> _consumer;
        private readonly ILogger<OrderConsumerWorker> _logger;

        public OrderConsumerWorker(ILogger<OrderConsumerWorker> logger, MatchingEngine engine)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _engine = engine ?? throw new ArgumentNullException(nameof(engine));

            var config = new ConsumerConfig
            {
                BootstrapServers = "host.docker.internal:9092"
,
                GroupId = "engine-group",
                AutoOffsetReset = AutoOffsetReset.Earliest,
                SecurityProtocol = SecurityProtocol.Plaintext
            };

            _consumer = new ConsumerBuilder<Null, string>(config).Build();
            _consumer.Subscribe("orders");
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {

            _logger.LogInformation("OrderConsumerWorker starting Chris!");
            return Task.Run(() =>
            {
                while (!stoppingToken.IsCancellationRequested)
                {   
                    
                    try
                    {
                        var cr = _consumer.Consume(stoppingToken);
                        var json = cr.Message.Value;
                        var orderMsg = JsonSerializer.Deserialize<OrderMessage>(json);
                        if (orderMsg != null)
                        {
                            Console.WriteLine($"Received order: {orderMsg.OrderId}, {orderMsg.InstrumentId}, {orderMsg.Side}");
                        }
                        var message = JsonSerializer.Deserialize<OrderMessage>(json);

                        // Convert to domain model & process
                        if (message != null)
                        {
                            var order = OrderFactory.CreateLimitOrder(
                                message.InstrumentId,
                                message.Side.Equals("Buy", System.StringComparison.OrdinalIgnoreCase)
                                    ? Domain.Enums.OrderSide.Buy
                                    : Domain.Enums.OrderSide.Sell,
                                message.Price,
                                message.Quantity
                            );
                            _engine.Process(order);
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        // Exiting
                    }
                    catch (Exception ex)
                    {
                        // Log error
                    }
                }
            }, stoppingToken);
        }

        public override void Dispose()
        {
            _consumer.Close();
            _consumer.Dispose();
            base.Dispose();
        }
    }

    // This is just a simple DTO for the incoming message
    public class OrderMessage
    {
        public Guid OrderId { get; set; }
        public string InstrumentId { get; set; }
        public string Side { get; set; }
        public decimal Price { get; set; }
        public decimal Quantity { get; set; }
    }
}
