using CyberGuardArch.Core.Configuration;
using CyberGuardArch.Core.Interfaces;
using Microsoft.Extensions.Options;

namespace CyberGuardArch.Worker;

public class CyberGuardArchWorker(ILogger<CyberGuardArchWorker> logger,
IOptions<TelegramOptions> telegramOptions,
INotificationService notificationService,
IFileMonitorService fileMonitorService) : BackgroundService
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

        fileMonitorService.OnFileChanged += async(tipo, ruta) =>
        {
            string mensajenotificacion = $"Alerta de seguridad:\n" +
                                     $"Accion: {tipo}\n" +
                                     $"Ruta del archivo: {ruta}\n" +
                                     $"Sistema: {Environment.MachineName}\n" +
                                     $"Hora: {DateTime.Now}";

            logger.LogWarning($"Cambio detectado: {tipo} en {ruta}");

            await notificationService.SendNotificationAsync(mensajenotificacion, stoppingToken);
        };

        // 3. Iniciar el monitoreo en tu carpeta de laboratorio
        // Cambia "tu_usuario" por tu nombre de usuario real en Arch
        string rutaLaboratorio = $"/home/jeison/CyberGuard_Lab"; 
        
        try 
        {
            fileMonitorService.StartMonitoring(rutaLaboratorio);
            logger.LogInformation($"Vigilando la carpeta: {rutaLaboratorio}");
        }
        catch (Exception ex)
        {
            logger.LogError($"No se pudo iniciar el monitoreo: {ex.Message}");
            return;
        }

        // 4. Mantener el servicio vivo
        await notificationService.SendNotificationAsync($"🛡️ {_options.NameBot} monitoreando archivos...", stoppingToken);
        
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken);
        }

        /*logger.LogInformation($"Worker iniciado con Telegram Token: {_options.Token}, ChatId: {_options.ChatId}, Nombre: {_options.NameBot}");

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
        }*/
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(10000, stoppingToken);
        }
    }
}
