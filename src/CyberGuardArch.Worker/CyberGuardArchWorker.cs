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

        fileMonitorService.OnFileChanged += async (tipo, ruta) =>
        {
            // Loguear el cambio detectado en plantilla de log sin interpolación para mejor rendimiento
            logger.LogWarning("Cambio detectado Tipo: {Tipo} ,Ruta: {Ruta}, Usuario: {User}, Sistema: {Host}",
                        tipo,
                        ruta,
                        Environment.UserName,
                        Environment.MachineName);
            
            //mensaje de notificación con formato limpio y claro para Telegram
            string mensajenotificacion = $"Alerta de seguridad:\n" +
                                     $"\tAccion: {tipo}\n" +
                                     $"\tRuta del archivo: {ruta}\n" +
                                     $"\tSistema: {Environment.MachineName}\n" +
                                     $"\tHora: {DateTime.Now}";

            // Enviar la notificación a Telegram
            await notificationService.SendNotificationAsync(mensajenotificacion, stoppingToken);
        };

        // prueba monitoreo a carpeta y subcarpetas
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
    }
}
