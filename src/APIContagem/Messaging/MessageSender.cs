using RabbitMQ.Client;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using OpenTelemetry.Context.Propagation;
using OpenTelemetry.Trace;
using APIContagem.Tracing;
using OpenTelemetry;

namespace APIContagem.Messaging;

public class MessageSender
{
    private readonly ILogger<MessageSender> _logger;
    private readonly IConfiguration _configuration;
    private readonly ActivitySource _activitySource;

    public MessageSender(ILogger<MessageSender> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
        _activitySource = new ActivitySource("APIContagem.Messaging");
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

            var basicProperties = new BasicProperties
            {
                Persistent = true
            };

            // Semantic convention - OpenTelemetry messaging specification:
            // https://opentelemetry.io/docs/specs/semconv/messaging/rabbitmq/
            var activityName = $"{exchangeName} send";
            using var activity = OpenTelemetryExtensions.ActivitySource
                .StartActivity(activityName, ActivityKind.Producer);
            ActivityContext contextToInject = OpenTelemetryExtensions.GetContextToInject(activity);
            var propagationContext = new PropagationContext(contextToInject, Baggage.Current);
            var _propagator = Propagators.DefaultTextMapPropagator;
            _propagator.Inject(propagationContext, basicProperties, InjectContextIntoProperties);
            activity?.SetTag("messaging.system", "rabbitmq");
            activity?.SetTag("messaging.operation", "send");
            activity?.SetTag("messaging.destination.name", exchangeName);
            activity?.SetTag("messaging.operation.type", "send");
            activity?.SetTag("message", bodyContent);

            await channel.BasicPublishAsync(
                exchange: exchangeName!,
                routingKey: String.Empty,
                body: Encoding.UTF8.GetBytes(bodyContent),
                mandatory: true,
                basicProperties: basicProperties
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

    // Método auxiliar para injetar contexto nos headers da mensagem
    private void InjectContextIntoProperties(BasicProperties properties, string key, string value)
    {
        properties.Headers ??= new Dictionary<string, object?>();
        properties.Headers[key] = Encoding.UTF8.GetBytes(value);
    }
}