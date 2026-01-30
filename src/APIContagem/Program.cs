using APIContagem;
using APIContagem.Messaging;
using APIContagem.Models;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

builder.Services.AddSingleton<Contador>();
builder.Services.AddScoped<MessageSender>();

builder.Services.AddCors();

var app = builder.Build();

app.MapOpenApi();
app.MapScalarApiReference(options =>
{
    options.Title = "API de Contagem de Acessos - Producer RabbitMQ";
    options.Theme = ScalarTheme.BluePlanet;
    options.DarkMode = true;
});

app.UseCors(policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());

app.MapGet("/contador", async (Contador contador, MessageSender messageSender) =>
{
    int valorAtualContador;
    lock (contador)
    {
        contador.Incrementar();
        valorAtualContador = contador.ValorAtual;
    }
    app.Logger.LogInformation($"Contador - Valor atual: {valorAtualContador}"); 

    var resultado = new ResultadoContador()
    {
        ValorAtual = contador.ValorAtual,
        Local = contador.Local,
        Kernel = contador.Kernel,
        Framework = contador.Framework,
        Mensagem = app.Configuration["Saudacao"]
    };
    app.Logger.LogInformation("Iniciando envio da mensagem...");
    await messageSender.SendMessageAsync<ResultadoContador>(resultado);

    return Results.Ok(resultado);
})
.Produces<ResultadoContador>();

app.Run();