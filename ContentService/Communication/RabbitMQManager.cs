using Microsoft.AspNetCore.Connections;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Collections.Concurrent;
using System.Text;
using System.Threading.Channels;

namespace ContentService.Communication
{
    public class RabbitMQManager : IRabbitMQManager
    {
        private IConnection _connection;
        private const string HOSTNAME = "host.docker.internal",
                             EXCHANGE = "",
                             KEY = "",
                             QUEUE = "content";
        private const int PORT = 5682;
        private const int RETRIES = 5;
        private string _replyQueueName;
        private IModel _channel;
        private readonly ConcurrentDictionary<string, TaskCompletionSource<string>> _callbackMapper = new();

        public RabbitMQManager()
        {
            Connect();
            StartListening();
        }
        private void Connect(int Try = 0)
        {
            try
            {
                var _factory = new ConnectionFactory { HostName = HOSTNAME, Port = PORT };
                _connection = _factory.CreateConnection();
            } catch (Exception ex)
            {
                if (Try < RETRIES)
                {
                    Console.WriteLine("Failed to connect to RabbitMQ broker; Retrying..");
                    Thread.Sleep(3000);
                    Connect(Try + 1);
                } else
                {
                    Console.WriteLine("Failed to connect to RabbitMQ broker;\n" + ex.Message);
                    throw ex;
                }
            }
        }
        private void StartListening()
        {
            _channel = _connection.CreateModel();

            _replyQueueName = _channel.QueueDeclare().QueueName;
            var consumer = new EventingBasicConsumer(_channel);
            consumer.Received += (model, ea) =>
            {
                Console.WriteLine($"RabbitMQ: A response is received");
                if (!_callbackMapper.TryRemove(ea.BasicProperties.CorrelationId, out var tcs))
                    return;
                var body = ea.Body.ToArray();
                var response = Encoding.UTF8.GetString(body);
                tcs.TrySetResult(response);
            };

            _channel.BasicConsume(consumer: consumer,
                                 queue: _replyQueueName,
                                 autoAck: true);
        }
        public virtual Task<string> RequestUpdate(CancellationToken cancellationToken = default)
        {
            IBasicProperties props = _channel.CreateBasicProperties();
            var correlationId = Guid.NewGuid().ToString();
            props.CorrelationId = correlationId;
            props.ReplyTo = _replyQueueName;
            var messageBytes = Encoding.UTF8.GetBytes("1");
            var tcs = new TaskCompletionSource<string>();
            _callbackMapper.TryAdd(correlationId, tcs);
            Console.WriteLine($"RabbitMQ: Sending {messageBytes}");
            _channel.BasicPublish(exchange: string.Empty,
                                 routingKey: QUEUE,
                                 basicProperties: props,
                                 body: messageBytes);

            cancellationToken.Register(() => _callbackMapper.TryRemove(correlationId, out _));
            return tcs.Task;
        }
        public void Dispose()
        {
            _connection.Close();
            _connection.Dispose();
        }
    }
}
