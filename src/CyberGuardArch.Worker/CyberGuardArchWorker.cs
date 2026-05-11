using CyberGuardArch.Core.Configuration;
using CyberGuardArch.Core.Interfaces;
using Microsoft.Extensions.Options;

namespace CyberGuardArch.Worker;

public class CyberGuardArchWorker(ILogger<CyberGuardArchWorker> logger,
IOptions<TelegramOptions> telegramOptions,
INotificationService notificationService,
IFileMonitorService fileMonitorService,
IConfiguration configuration) : BackgroundService
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
            List<string> rutasExcluidas = configuration.GetSection("Monitoreo:Exclusiones").Get<List<string>>() ?? new List<string>();
            if (rutasExcluidas.Any(e => ruta.Contains(e, StringComparison.OrdinalIgnoreCase)))
            {
                return; 
            }
            
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
        string[] rutas = configuration.GetSection("Monitoreo:Rutas").Get<string[]>() ?? Array.Empty<string>();
        if (rutas.Length == 0)
        {
            logger.LogError("No se han configurado rutas para monitorear en appsettings.json");
            return;
        }

        foreach (var ruta in rutas)
        {
            try
            {
                fileMonitorService.StartMonitoring(ruta);
                logger.LogInformation("Vigilando la carpeta: {ruta}", ruta);
            }
            catch (UnauthorizedAccessException)
            {
                logger.LogError("Acceso denegado a la ruta: {ruta}. \nFalta permisos de administrador.", ruta);
                return;
            }
            catch (DirectoryNotFoundException)
            {
                logger.LogError("La ruta no existe: {ruta}", ruta);
                return;
            }
            catch (Exception ex)
            {
                logger.LogError("Error al iniciar monitoreo en la ruta: {ruta}: {Mensaje}", ruta, ex.Message);
                return;
            }
        }

        await notificationService.SendNotificationAsync($"🛡️ {_options.NameBot} monitoreando archivos...", stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken);
        }
    }
}
