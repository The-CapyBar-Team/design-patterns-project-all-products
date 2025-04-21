using Microsoft.EntityFrameworkCore.Metadata;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AllProductsService.Services
{
    public class RabbitMQService : IDisposable
    {
        private readonly ConnectionFactory _factory;
        private IConnection _connection;
        private IChannel _channel;

        public RabbitMQService(string hostName, string port, string userName, string password)
        {
            _factory = new ConnectionFactory
            {
                HostName = hostName,
                Port = int.Parse(port),
                UserName = userName,
                Password = password
            };

            _connection = _factory.CreateConnectionAsync().Result;
            _channel = _connection.CreateChannelAsync().Result;
        }

        public async Task DeclareQueue(string queueName)
        {
            await _channel.QueueDeclareAsync(queue: queueName,
                                 durable: true,
                                 exclusive: false,
                                 autoDelete: false,
                                 arguments: null);
        }

        public async Task SendMessage(string queueName, byte[] message)
        {
            //var body = Encoding.UTF8.GetBytes(message);

            await _channel.BasicPublishAsync(exchange: "",
                                 routingKey: queueName,
                                 body: message);

            //Console.WriteLine($"[x] Sent {message} to {queueName}");
        }

        public void SubscribeToQueue(string queueName, Action<byte[]> messageHandler)
        {

            var consumer = new AsyncEventingBasicConsumer(_channel);

            consumer.ReceivedAsync += (model, ea) =>
            {
                var body = ea.Body.ToArray();
                //var message = Encoding.UTF8.GetString(body);

                //Console.WriteLine($"[x] Received message from {queueName}");

                messageHandler(body);

                //_channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
                return Task.CompletedTask;
            };

            _channel.BasicConsumeAsync(queue: queueName,
                                 autoAck: true,
                                 consumer: consumer);
        }

        public void Dispose()
        {
            _channel?.Dispose();
            _connection?.Dispose();
        }
    }
}
