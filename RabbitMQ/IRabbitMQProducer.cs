namespace Capstone.RabbitMQ
{
    public interface IRabbitMQProducer
    {
        public Task SendMessageAsync(string message);
    }
}
