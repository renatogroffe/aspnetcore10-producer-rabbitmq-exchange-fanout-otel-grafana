using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace APIContagem.Messaging;

public class MessageSender
{
    private readonly ILogger<MessageSender> _logger;
    private readonly IConfiguration _configuration;

    public MessageSender(ILogger<MessageSender> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    public async Task SendMessageAsync<T>(T message)
    {
        var exchangeName = _configuration["RabbitMQ:Exchange"];
        var bodyContent = JsonSerializer.Serialize(message);

        try
        {
            var factory = new ConnectionFactory()
            {
                Uri = new Uri(_configuration.GetConnectionString("RabbitMQ")!)
            };
            using var connection = await factory.CreateConnectionAsync();
            using var channel = await connection.CreateChannelAsync();
                
            await channel.BasicPublishAsync(
                exchange: exchangeName!,
                routingKey: String.Empty,
                body: Encoding.UTF8.GetBytes(bodyContent),
                mandatory: true,
                basicProperties: new BasicProperties
                {
                    Persistent = true
                }
            );

            _logger.LogInformation(
                $"RabbitMQ - Envio para a exchange {exchangeName} concluído | " +
                $"{bodyContent}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha na publicacao da mensagem.");
            throw;
        }
    }
}