using System.Diagnostics;
using System.Text.RegularExpressions;
using CyberGuardArch.Core.Interfaces;
using CyberGuardArch.Core.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CyberGuardArch.Infrastructure.Services;

public partial class LinuxNetworkMonitorService(
    ILogger<LinuxNetworkMonitorService> logger,
    IConfiguration configuration) : INetworkMonitorService
{
    public string MonitorName => "Network";

    public event Action<NetworkConnectionInfo>? OnConnectionDetected;
    public event Action<NetworkConnectionInfo>? OnConnectionDisconnected;

    private readonly HashSet<string> _knownConnections = [];
    private readonly Dictionary<string, NetworkConnectionInfo> _activeConnectionsCache = [];

    private readonly Dictionary<string, List<DateTime>> _historialIntentosPublicos = [];
    private readonly Dictionary<string, DateTime> _ipsSilenciadasCooldown = [];
    private readonly HashSet<string> _ipsConAccesoExitosoVIP = [];

    private CancellationTokenSource? _cts;

    [GeneratedRegex(@"users:\(\(""(?<pname>[^""]+)""")]
    private static partial Regex ProcessNameRegex();

    public Task StartMonitoringAsync(CancellationToken cancellationToken)
    {
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        logger.LogInformation("Iniciando escaneo heurístico tolerante a fallos en sensores...");
        _ = MonitorNetworkLoopAsync(_cts.Token);
        return Task.CompletedTask;
    }

    public Task StopMonitoringAsync()
    {
        _cts?.Cancel();
        logger.LogInformation("Sensor de red detenido.");
        return Task.CompletedTask;
    }

    private async Task MonitorNetworkLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                string rawOutput = await ExecuteSsCommandAsync(cancellationToken);
                ParseAndEvaluateConnections(rawOutput);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Error en el ciclo del monitor de red.");
            }
            await Task.Delay(3000, cancellationToken);
        }
    }

    private static async Task<string> ExecuteSsCommandAsync(CancellationToken cancellationToken)
    {
        using var process = new Process();
        process.StartInfo = new ProcessStartInfo
        {
            FileName = "ss",
            Arguments = "-tupn", // Mantenemos tupn para capturar estados y mapeos cruzados
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        process.Start();
        return await process.StandardOutput.ReadToEndAsync(cancellationToken);
    }

    public void ParseAndEvaluateConnections(string rawOutput)
    {
        var configSeccion = configuration.GetSection("Monitoreo:EscudoRed");
        int ventanaSegundos = configSeccion.GetValue("VentanaTiempoSegundos", 60);
        int maxIntentos = configSeccion.GetValue("MaxIntentosPermitidos", 3);
        int minutesCooldown = configSeccion.GetValue("MinutosEnfriamientoAlerta", 5);

        if (string.IsNullOrWhiteSpace(rawOutput)) return;

        var lines = rawOutput.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var currentCycleConnections = new HashSet<string>();

        // FASE 1: Identificación Heurística Dinámica de Sesiones Estables (Gana Pase VIP)
        foreach (var line in lines)
        {
            if (line.StartsWith("State") || line.StartsWith("Netid") || string.IsNullOrWhiteSpace(line)) continue;

            var parts = line.Split((char[])[' ', '\t'], StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 4) continue;

            // Si la línea empieza con protocolo (tcp/udp), las posiciones se corren 1 índice a la derecha
            bool tieneProtocolo = parts[0] == "tcp" || parts[0] == "udp";
            string state = tieneProtocolo ? parts[1] : parts[0];
            string rawRemote = tieneProtocolo ? parts[5] : parts[4];

            // Caso de respaldo si ss omite columnas de colas Recv-Q/Send-Q
            if (rawRemote.Contains("users:") || !rawRemote.Contains(':'))
            {
                rawRemote = tieneProtocolo ? parts[3] : parts[2];
            }

            int lastRemoteColon = rawRemote.LastIndexOf(':');
            if (lastRemoteColon == -1) continue;
            string remoteIp = rawRemote[..lastRemoteColon].Replace("[", "").Replace("]", "").Replace("::ffff:", "");

            /// <summary>
            /// Heurística de Pase VIP Dinámico: Si detecta una conexión estable (ESTAB) desde una IP externa (no localhost), la añadimos a una lista VIP temporal. Esto permite que dispositivos confiables como tu celular o laptop se conecten sin bloqueos, incluso si no están en la red local, mientras seguimos protegiendo contra conexiones desconocidas. La IP VIP se mantiene mientras siga teniendo conexiones estables y se elimina automáticamente al desconectarse, evitando acumulación de IPs VIP obsoletas.
            /// </summary>
            if ((state.Contains("ESTAB") || state == "ESTAB") && remoteIp != "127.0.0.1" && remoteIp != "::1")
            {
                if (!_ipsConAccesoExitosoVIP.Contains(remoteIp))
                {
                    logger.LogInformation("✅ [Filtro de Red] IP identificada como segura (Conexión Establecida): {RemoteIp}", remoteIp);
                    _ipsConAccesoExitosoVIP.Add(remoteIp);
                }
            }
        }

        // FASE 2: Procesamiento y Despacho de Eventos Certificados
        foreach (var line in lines)
        {
            if (line.StartsWith("State") || line.StartsWith("Netid") || string.IsNullOrWhiteSpace(line)) continue;

            var parts = line.Split((char[])[' ', '\t'], StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 4) continue;

            string state = "ESTAB";
            string rawLocal = "";
            string rawRemote = "";
            string processName = "sshd (Servicio SSH)";

            bool tieneProtocolo = parts[0] == "tcp" || parts[0] == "udp";

            if (tieneProtocolo)
            {
                // Mapeo exacto detectado en tu log: [tcp, ESTAB, 0, 0, Local, Remote]
                state = parts[1];
                rawLocal = parts[4];
                rawRemote = parts[5];

                // Buscar si se adjuntaron metadatos del proceso al final
                string filaCompleta = string.Join(" ", parts);
                if (filaCompleta.Contains("users:"))
                {
                    var match = ProcessNameRegex().Match(filaCompleta);
                    if (match.Success) processName = match.Groups["pname"].Value;
                }
            }
            else
            {
                // Formato alternativo sin cabecera de protocolo
                if (char.IsDigit(parts[0][0]))
                {
                    rawLocal = parts[2];
                    rawRemote = parts[3];
                }
                else
                {
                    state = parts[0];
                    rawLocal = parts[3];
                    rawRemote = parts[4];
                }
            }

            int lastLocalColon = rawLocal.LastIndexOf(':');
            int lastRemoteColon = rawRemote.LastIndexOf(':');
            if (lastLocalColon == -1 || lastRemoteColon == -1) continue;

            string ipA = rawLocal[..lastLocalColon].Replace("[", "").Replace("]", "").Replace("::ffff:", "");
            string ipB = rawRemote[..lastRemoteColon].Replace("[", "").Replace("]", "").Replace("::ffff:", "");
            _ = int.TryParse(rawLocal[(lastLocalColon + 1)..], out int portA);
            _ = int.TryParse(rawRemote[(lastRemoteColon + 1)..], out int portB);

            // Ajuste simétrico para servidores (puerto local 22)
            string localIp = ipA; int localPort = portA; string remoteIp = ipB; int remotePort = portB;
            if (portB == 22 || (portA > 32768 && portB <= 1024 && portB != 443 && portB != 80))
            {
                localIp = ipB; localPort = portB; remoteIp = ipA; remotePort = portA;
            }

            if (remoteIp == "127.0.0.1" || remoteIp == "::1" || remotePort == 443 || remotePort == 80) continue;

            string connectionKey = $"tcp-{remoteIp}:{remotePort}->{localIp}:{localPort}";
            currentCycleConnections.Add(connectionKey);

            var connectionInfo = new NetworkConnectionInfo("tcp", localIp, localPort, remoteIp, remotePort, state, processName);

            if (!_knownConnections.Contains(connectionKey))
            {
                _knownConnections.Add(connectionKey);
                _activeConnectionsCache[connectionKey] = connectionInfo;

                bool esVIP = remoteIp.StartsWith("192.168.") || _ipsConAccesoExitosoVIP.Contains(remoteIp);

                if (esVIP)
                {
                    logger.LogWarning("🔓 [NOTIFICACIÓN] Conexión entrante autorizada: {RemoteIp} en puerto {LocalPort}", remoteIp, localPort);
                    OnConnectionDetected?.Invoke(connectionInfo);
                }
                else
                {
                    // Escudo protector de ráfagas para IPs externas desconocidas
                    DateTime ahora = DateTime.Now;

                    if (_ipsSilenciadasCooldown.TryGetValue(remoteIp, out var finCooldown))
                    {
                        if (ahora < finCooldown) continue;
                        _ipsSilenciadasCooldown.Remove(remoteIp);
                    }

                    if (!_historialIntentosPublicos.TryGetValue(remoteIp, out var marcas))
                    {
                        marcas = [];
                        _historialIntentosPublicos[remoteIp] = marcas;
                    }

                    marcas.Add(ahora);
                    marcas.RemoveAll(t => (ahora - t).TotalSeconds > ventanaSegundos);

                    if (marcas.Count > maxIntentos)
                    {
                        _ipsSilenciadasCooldown[remoteIp] = ahora.AddMinutes(minutesCooldown);
                        logger.LogWarning("⚠️ [Filtro Anti-Spam] IP {RemoteIp} bloqueada temporalmente por exceso de intentos fallidos, intento de ataque.", remoteIp);
                        //puede verificar si el ataque  https://www.abuseipdb.com/check/RemoteIp para ver si es una IP maliciosa conocida y agregarla a una lista negra permanente
                    }
                    else
                    {
                        OnConnectionDetected?.Invoke(connectionInfo);
                    }
                }
            }
        }

        // FASE 3: Desconexiones limpias
        var disconnectedKeys = _knownConnections.Except(currentCycleConnections).ToList();
        foreach (var key in disconnectedKeys)
        {
            if (_activeConnectionsCache.TryGetValue(key, out var oldInfo))
            {
                bool esVIP = oldInfo.RemoteAddress.StartsWith("192.168.") || _ipsConAccesoExitosoVIP.Contains(oldInfo.RemoteAddress);

                if (esVIP)
                {
                    OnConnectionDisconnected?.Invoke(oldInfo);
                    _ipsConAccesoExitosoVIP.Remove(oldInfo.RemoteAddress);
                }
                else
                {
                    if (_ipsSilenciadasCooldown.ContainsKey(oldInfo.RemoteAddress))
                    {
                        logger.LogInformation("Escudo Red: Tráfico abusivo de {RemoteIp} finalizado.", oldInfo.RemoteAddress);
                    }
                    else
                    {
                        OnConnectionDisconnected?.Invoke(oldInfo);
                    }
                }
                _activeConnectionsCache.Remove(key);
            }
            _knownConnections.Remove(key);
        }
    }
// 🟢 AÑADE ESTE MÉTODO AL FINAL DE TU CLASE LinuxNetworkMonitorService.cs
public List<NetworkConnectionInfo> GetActiveConnections()
{
    lock (_activeConnectionsCache)
    {
        return [.. _activeConnectionsCache.Values];
    }
}
}