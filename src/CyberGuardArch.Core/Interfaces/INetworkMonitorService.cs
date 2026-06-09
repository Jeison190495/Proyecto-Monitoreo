using CyberGuardArch.Core.Models;
namespace CyberGuardArch.Core.Interfaces;
public interface INetworkMonitorService : IMonitoreoService
{
    /// <summary>
    /// Evento que se dispara cuando se detecta una nueva conexión de red.
    /// Devuelve: NetworkConnectionInfo con detalles de la conexión.
    /// </summary>
    event Action<NetworkConnectionInfo>? OnConnectionDetected;
    /// <summary>
    /// 👇Se dispara cuando una conexión sospechosa previamente detectada se cierra.
    /// </summary>
    event Action<NetworkConnectionInfo>? OnConnectionDisconnected;
    Task StartMonitoringAsync(CancellationToken cancellationToken);
    
    // 🟢 AÑADE ESTA LÍNEA A LA INTERFAZ:
    List<NetworkConnectionInfo> GetActiveConnections();

    string MonitorName { get; }
}