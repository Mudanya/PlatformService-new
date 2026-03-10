using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Metadata;
using PlatformService.Dtos;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace PlatformService.AsyncDataServices;

public class MessageBusClient : IMessageBusClient
{
    private readonly IConfiguration _configuration;
    private IConnection? _connection;
    private IChannel? _channel;

    public MessageBusClient(IConfiguration configuration)
    {
        _configuration = configuration;
        var factory = new ConnectionFactory() { HostName = _configuration["RabbitMQHost"]!, Port = int.Parse(_configuration["RabbitMQPort"]!) };
        _ = InitializeAsync();

    }
    public async Task InitializeAsync()
    {
        var factory = new ConnectionFactory()
        {
            HostName = _configuration["RabbitMQHost"]!,
            Port = int.Parse(_configuration["RabbitMQPort"]!)
        };
        try
        {
            _connection = await factory.CreateConnectionAsync();
            _channel = await _connection.CreateChannelAsync();
            await _channel.ExchangeDeclareAsync(exchange: "trigger", type: ExchangeType.Fanout);
            _connection.ConnectionShutdownAsync += RabbitMQ_ConnectionShutdown;
            Console.WriteLine("--> Connected to Message Bus");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"--> Could not connect to Message Bus: {ex.Message}");
        }
    }

    private async Task RabbitMQ_ConnectionShutdown(object sender, ShutdownEventArgs e)
    {
        Console.WriteLine("--> RabbitMQ Connection Shutdown");
    }

    public async Task PublishNewPlatform(PlatformPublishedDto platformPublishedDto)
    {
        await InitializeAsync();
        var message = JsonSerializer.Serialize(platformPublishedDto);
        if (_connection != null && _connection.IsOpen)
        {
            Console.WriteLine("--> RabbitMQ Connection Open, sending message...");
            await SendMessage(message);

        }
        else
        {
            Console.WriteLine("--> RabbitMQ Connection is closed, not sending");
        }

    }

    private async Task SendMessage(string message)
    {
        var body = Encoding.UTF8.GetBytes(message);
        var props = new BasicProperties();
        await _channel!.BasicPublishAsync(exchange: "trigger", routingKey: "", mandatory: true, basicProperties: props, body: body);
        Console.WriteLine($"--> We have sent {message}");
    }
    public async Task Dispose()
    {
        await InitializeAsync();
        Console.WriteLine("MessageBus Disposed");
        if (_channel != null && _channel.IsOpen)
        {
            await _channel.CloseAsync();
            await _connection!.CloseAsync();
        }
    }
}