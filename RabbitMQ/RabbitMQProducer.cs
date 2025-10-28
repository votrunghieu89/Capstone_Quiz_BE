using Capstone.Model;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using System.Threading.Tasks;

namespace Capstone.RabbitMQ
{
    public class RabbitMQProducer : IRabbitMQProducer
    {
        private readonly IConnection _connection;
        private readonly IChannel _channel;

        public RabbitMQProducer(IOptions<RabbitMQModel> options)
        {
            var settings = options.Value;

            var Connection_Factory = new ConnectionFactory
            {
                HostName = settings.HostName,
                Port = settings.Port,
                UserName = settings.UserName,
                Password = settings.Password
            };

            
            _connection = Connection_Factory.CreateConnectionAsync().GetAwaiter().GetResult();
            _channel = _connection.CreateChannelAsync().GetAwaiter().GetResult();

            // Khai báo queue (async)
            _channel.QueueDeclareAsync(
                queue: "AuditLog",
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null
            ).GetAwaiter().GetResult();
        }

        public async Task SendMessageAsync(string message)
        {
            var body = System.Text.Encoding.UTF8.GetBytes(message);

            // Gửi message (bản async)
            await _channel.BasicPublishAsync(
                exchange: "",
                routingKey: "AuditLog",
                mandatory: false,
                body: body
            );
        }
    }
}
