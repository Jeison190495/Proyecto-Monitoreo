using CyberGuardArch.Core.Configuration;
using CyberGuardArch.Core.Interfaces;
using Microsoft.Extensions.Options;

namespace CyberGuardArch.Worker;

public class CyberGuardArchWorker(ILogger<CyberGuardArchWorker> logger,
    IOptions<TelegramOptions> telegramOptions,
    INotificationService notificationService,
    IFileMonitorService fileMonitorService,
    INetworkMonitorService networkMonitorService,
    ICommandHistoryService commandHistoryService,
    IConfiguration configuration) : BackgroundService
{
    private readonly TelegramOptions _options = telegramOptions.Value;
    // Guarda la IP + Proceso y la hora de la última notificación enviada para no repetir
    private readonly Dictionary<string, DateTime> _alertasRedEnviadasCache = new();

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

        // --- MONITOR DE ALTERACIONES DE ARCHIVOS E HISTORIAL DE COMANDOS ---
        fileMonitorService.OnFileChanged += async (tipo, ruta) =>
        {
            // 1. Filtrar exclusiones de appsettings
            List<string> rutasExcluidas = configuration.GetSection("Monitoreo:Exclusiones").Get<List<string>>() ?? [];
            if (rutasExcluidas.Any(exc => ruta.Contains(exc))) return;

            // Silenciar archivos temporales generados por el flush de ZSH (.new o .tmp)
            if (ruta.Contains(".zsh_history.")) return;

            // 2. Separación de Lógica Forense por bloques condicionales
            if (ruta.EndsWith(".zsh_history") || ruta.EndsWith(".bash_history"))
            {
                // Un delay un poco más largo (400ms) da tiempo a que el sistema operativo asiente el archivo en disco correctamente
                await Task.Delay(400, stoppingToken);
                var comandosRecientes = commandHistoryService.GetLastCommands(ruta, count: 1);

                if (comandosRecientes.Any())
                {
                    string ultimoComando = comandosRecientes.First();
                    string usuario = Path.GetFileName(Path.GetDirectoryName(ruta)) ?? "Desconocido";
                    if (ruta.Contains("/root/")) usuario = "root";

                    string origenTerminal = "Consola Física / Local TTY";

                    try
                    {
                        // Consultamos el sensor de red para ver si hay sockets SSH reales vivos en este instante
                        var conexionesActivas = networkMonitorService.GetActiveConnections();
                        var conexionSsh = conexionesActivas.FirstOrDefault(c =>
                            c.LocalPort == 22 ||
                            c.ProcessName.Contains("sshd", StringComparison.OrdinalIgnoreCase));

                        // 🔍 REGLA DE ORO FORENSE: 
                        // Si el comando ejecutado es "exit", y hay una sesión SSH abierta, asumimos con prioridad alta que proviene de la sesión remota que desea cerrarse.
                        // Si el comando es un comando común (como pwd, ls) y se disparó en simultáneo, validamos el estado real usando 'who'.
                        if (conexionSsh != null)
                        {
                            if (ultimoComando == "exit" || ultimoComando.StartsWith("logout"))
                            {
                                origenTerminal = $"Remoto (IP: {conexionSsh.RemoteAddress}:{conexionSsh.RemotePort} vía SSH)";
                            }
                            else
                            {
                                var psi = new System.Diagnostics.ProcessStartInfo
                                {
                                    FileName = "who",
                                    Arguments = "-u",
                                    RedirectStandardOutput = true,
                                    UseShellExecute = false,
                                    CreateNoWindow = true
                                };

                                using var proc = System.Diagnostics.Process.Start(psi);
                                if (proc != null)
                                {
                                    string output = await proc.StandardOutput.ReadToEndAsync(stoppingToken);
                                    string[] lineasWho = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);

                                    // 🔍 ANÁLISIS FORENSE DE INACTIVIDAD (Idle Time)
                                    // Buscamos la línea de 'who' asignada a la IP remota
                                    string? lineaSshActiva = lineasWho.FirstOrDefault(l =>
                                        l.Contains(usuario) &&
                                        l.Contains(conexionSsh.RemoteAddress) &&
                                        l.Contains("pts/"));

                                    if (lineaSshActiva != null)
                                    {
                                        // En 'who -u', si la terminal SSH se usó en el último minuto, muestra un '.' o está vacía la sección de idle.
                                        // Si lleva inactiva más de un minuto o muestra un tiempo intermedio (ej: 00:01), 
                                        // fragmentamos la línea para verificar si está verdaderamente activa en este segundo.
                                        string[] columnas = lineaSshActiva.Split(' ', StringSplitOptions.RemoveEmptyEntries);

                                        // Heurística de desempate segura:
                                        // Si la terminal SSH está abierta pero tú escribes en la PC local, la terminal SSH registrará inactividad (idle).
                                        // Buscamos si la línea contiene el indicador de actividad en caliente '.' 
                                        // Si no lo contiene o vemos que otra línea (la de la consola física tty) no tiene idle, discriminamos.
                                        bool ttySshTieneActividadEnCaliente = lineaSshActiva.Contains(" . ") || !lineaSshActiva.Any(c => char.IsDigit(c) && lineaSshActiva.Contains(':'));

                                        // Para asegurar efectividad total: si el comando se genera en la PC local,
                                        // 'who' marcará la terminal pts con tiempo de espera.
                                        if (ttySshTieneActividadEnCaliente)
                                        {
                                            origenTerminal = $"Remoto (IP: {conexionSsh.RemoteAddress}:{conexionSsh.RemotePort} vía SSH)";
                                        }
                                        else
                                        {
                                            origenTerminal = "Consola Física / Local TTY";
                                        }
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogError("Error al correlacionar origen con heurística avanzada: {Message}", ex.Message);
                    }

                    // Evitar que comandos fantasmas vacíos ensucien el log
                    if (string.IsNullOrWhiteSpace(ultimoComando)) return;

                    // 📝 Imprimimos en consola enriqueciendo el log con el origen real detectado
                    logger.LogWarning("💻 [Auditoría] El usuario '{Usuario}' ejecutó: \"{Comando}\" desde \"{Origen}\"",
                        usuario, ultimoComando, origenTerminal);

                    // 📤 Notificación estructurada para Telegram
                    string mensajeComando = $"💻 *AUDITORÍA DE COMANDOS* 💻\n\n" +
                                            $"👤 *Usuario:* `{usuario}`\n" +
                                            $"⌨️ *Comando:* `{ultimoComando}`\n" +
                                            $"📍 *Origen Detectado:* `{origenTerminal}`\n" +
                                            $"💻 *Sistema Host:* {Environment.MachineName}\n" +
                                            $"⏰ *Hora:* {DateTime.Now:dd/MM/yyyy HH:mm:ss}";

                    await notificationService.SendNotificationAsync(mensajeComando, stoppingToken);
                }
            }
            else
            {
                // 📂 ALERTA PARA CUALQUIER OTRO ARCHIVO: Se ejecuta si NO es un archivo de historial (.zsh_history / .bash_history)
                logger.LogWarning("Cambio detectado Tipo: \"{Tipo}\" ,Ruta: \"{Ruta}\", Usuario: \"{Usuario}\", Sistema: \"{Sistema}\"",
                    tipo, ruta, Environment.UserName, Environment.MachineName);
            }
        };

        // Carga dinámica de rutas a vigilar de appsettings.json
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

        // --- MONITOR DE SENSOR DE RED (MÓDULO SS) ---
        networkMonitorService.OnConnectionDetected += async (info) =>
        {
            // 🛑 FILTRO ANTISPAM DE TELEGRAM: Evitar inundación por la misma IP y Proceso
            string llaveAlerta = $"{info.RemoteAddress}_{info.ProcessName}_{info.LocalPort}";

            if (_alertasRedEnviadasCache.TryGetValue(llaveAlerta, out DateTime ultimaAlertaTime))
            {
                // Si ya notificamos esta IP/Proceso hace menos de 5 minutos, ignoramos el envío a Telegram
                if (DateTime.Now - ultimaAlertaTime < TimeSpan.FromMinutes(5))
                {
                    // Lo dejamos en el log local de la consola por si acaso, pero NO satures Telegram
                    logger.LogInformation("🤫 [Antispam Telegram] Alerta omitida para {RemoteIp} ({Proceso}) por enfriamiento activo.",
                        info.RemoteAddress, info.ProcessName);
                    return;
                }
            }

            // Actualizar o insertar la hora del último envío de esta alerta
            _alertasRedEnviadasCache[llaveAlerta] = DateTime.Now;

            logger.LogWarning("¡CONEXIÓN ENTRANTE DETECTADA! Protocolo: {Protocolo}, Origen: {RemoteIp}:{RemotePort} -> Destino Local: {LocalPort}, Proceso: {Proceso}",
                        info.Protocol, info.RemoteAddress, info.RemotePort, info.LocalPort, info.ProcessName);

            string mensajeDeRed = $"🟢 *CONEXIÓN ENTRANTE DETECTADA* 🟢\n\n" +
                                  $"🌐 *Origen:* `{info.RemoteAddress}:{info.RemotePort}`\n" +
                                  $"🖥️ *Destino Local (Puerto):* `{info.LocalPort}`\n" +
                                  $"🔹 *Protocolo:* {info.Protocol}\n" +
                                  $"👤 *Proceso Asociado:* `{info.ProcessName}`\n" +
                                  $"💻 *Sistema:* {Environment.MachineName}\n" +
                                  $"⏰ *Hora:* {DateTime.Now:dd/MM/yyyy HH:mm:ss}";

            await notificationService.SendNotificationAsync(mensajeDeRed, stoppingToken);
        };

        networkMonitorService.OnConnectionDisconnected += async (info) =>
        {
            logger.LogInformation("Conexión finalizada. Origen: {RemoteIp}:{RemotePort} liberó el puerto local {LocalPort}",
                info.RemoteAddress, info.RemotePort, info.LocalPort);

            string mensajeDesconexion = $"🚪 *SESIÓN FINALIZADA* 🚪\n\n" +
                                        $"🔹 *Origen IP:* `{info.RemoteAddress}:{info.RemotePort}`\n" +
                                        $"🔹 *Puerto Liberado:* `{info.LocalPort}` ({info.ProcessName})\n" +
                                        $"💻 *Host:* {Environment.MachineName}\n" +
                                        $"⏰ *Hora Salida:* {DateTime.Now:dd/MM/yyyy HH:mm:ss}\\n\\n" +
                                        $"💡 *NOTA:* Revisa los mensajes anteriores para verificar la auditoría de comandos ejecutados en esta sesión.";

            await notificationService.SendNotificationAsync(mensajeDesconexion, stoppingToken);
        };

        try
        {
            await networkMonitorService.StartMonitoringAsync(stoppingToken);
            logger.LogInformation("Sensor de red iniciado correctamente.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Fallo al iniciar sensor de red.");
            return;
        }

        await notificationService.SendNotificationAsync($"🛡️ {_options.NameBot} activo y protegiendo el sistema...", stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken);
        }
    }
}