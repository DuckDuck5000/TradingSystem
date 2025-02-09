using Confluent.Kafka;
using System.Text.Json;

public class KafkaOrderProducer : IOrderProducer
{
    private readonly IProducer<Null, string> _producer;
    private const string TopicName = "orders";

    public KafkaOrderProducer(string bootstrapServers)
    {
        var config = new ProducerConfig
        {
            BootstrapServers = "host.docker.internal:9092",//bootstrapServers,
            SecurityProtocol = SecurityProtocol.Plaintext
        };
        _producer = new ProducerBuilder<Null, string>(config).Build();
    }

    public async Task ProduceOrderAsync(OrderMessage msg)
    {
        var json = JsonSerializer.Serialize(msg);
        await _producer.ProduceAsync(TopicName, new Message<Null, string> { Value = json });
    }
}


public class OrderMessage
{
    public Guid OrderId { get; set; }
    public string InstrumentId { get; set; }
    public string Side { get; set; }
    public decimal Price { get; set; }
    public decimal Quantity { get; set; }
    // TODO: add more
}
