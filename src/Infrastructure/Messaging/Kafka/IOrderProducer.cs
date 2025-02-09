public interface IOrderProducer
{
    Task ProduceOrderAsync(OrderMessage msg);
}