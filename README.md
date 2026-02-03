# aspnetcore10-producer-rabbitmq-exchange-fanout-otel-grafana
Exemplo de API criada com .NET 10 + ASP.NET Core que simula um Producer do RabbitMQ, com envio de mensagens a uma exchange do tipo Fan-out e monitoramento via OpenTelemetry + Grafana + Alloy + Tempo. Inclui script do Docker Compose para subida do ambiente de testes.

Aplicação consumidora utilizada nos testes: **https://github.com/renatogroffe/dotnet10-worker-consumer-rabbitmq-exchange-fanout-otel-grafana**

Exemplo de telemetria gerada na comunicação entre essas 2 aplicações:

![Comunicação entre Producer e Consumer via RabbitMQ](img/grafana-otel-rabbitmq.png)