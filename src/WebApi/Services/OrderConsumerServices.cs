using Confluent.Kafka;
using Domain.Services;     
using Domain.Enums;        
using Domain.Factories;      
using Microsoft.Extensions.Hosting;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace WebApi.Services
{

    public class OrderConsumerService : BackgroundService
    {
        private readonly MatchingEngine _engine;
        private readonly IConsumer<Null, string> _consumer;

        public OrderConsumerService(MatchingEngine engine, string bootstrapServers, string groupId)
        {
            _engine = engine;

            var config = new ConsumerConfig
            {
                BootstrapServers = "host.docker.internal:9092"
,
                GroupId = groupId,
                AutoOffsetReset = AutoOffsetReset.Earliest,
                SecurityProtocol = SecurityProtocol.Plaintext
            };

            _consumer = new ConsumerBuilder<Null, string>(config).Build();
            _consumer.Subscribe("orders"); // The name of your Kafka topic
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            return Task.Run(() =>
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        var cr = _consumer.Consume(stoppingToken);
                        var json = cr.Message.Value;

                        var orderMessage = JsonSerializer.Deserialize<OrderMessage>(json);
                        if (orderMessage != null)
                        {
                            // Convert message -> domain Order
                            var side = orderMessage.Side.Equals("buy", System.StringComparison.OrdinalIgnoreCase)
                                ? OrderSide.Buy
                                : OrderSide.Sell;

                            // If you want to create a limit order by default:
                            var order = OrderFactory.CreateLimitOrder(
                                orderMessage.InstrumentId,
                                side,
                                orderMessage.Price,
                                orderMessage.Quantity
                            );

                            _engine.Process(order);
                            
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        // shutting down
                        break;
                    }
                    catch (System.Exception ex)
                    {
                        // log error, continue
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

    // The data shape we expect from Kafka
    public class OrderMessage
    {
        public System.Guid OrderId { get; set; }
        public string InstrumentId { get; set; }
        public string Side { get; set; } // "Buy" or "Sell"
        public decimal Price { get; set; }
        public decimal Quantity { get; set; }
    }
}
