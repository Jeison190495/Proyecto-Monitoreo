using CyberGuardArch.Core.Configuration;
using CyberGuardArch.Core.Interfaces;
using Microsoft.Extensions.Options;

namespace CyberGuardArch.Worker;

public class Worker(ILogger<Worker> logger,
IOptions<TelegramOptions> telegramOptions,
INotificationService notificationService) : BackgroundService
{
    private readonly TelegramOptions _options = telegramOptions.Value;
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_options is { Token: null or "" } or { ChatId: null or "" } or { NameBot: null or "" })
        {
            switch (_options)
            {
                case { Token: "" or null }:
                    logger.LogError("Telegram Token no configurado");
                    break;
                case { ChatId: "" or null }:
                    logger.LogError("Telegram ChatId no configurado");
                    break;
                case { NameBot: "" or null }:
                    logger.LogError("Telegram NameBot no configurado");
                    break;
            }

            return;
        }

        logger.LogInformation($"Worker iniciado con Telegram Token: {_options.Token}, ChatId: {_options.ChatId}, Nombre: {_options.NameBot}");

        try
        {
            await notificationService.SendNotificationAsync(
                $"🚀 {_options.NameBot} en línea! Sistema: {Environment.MachineName}",
                stoppingToken);
            logger.LogInformation("Mensaje de bienvenida enviado a Telegram.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error al enviar el mensaje inicial.");
        }
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(10000, stoppingToken);
        }
    }
}
