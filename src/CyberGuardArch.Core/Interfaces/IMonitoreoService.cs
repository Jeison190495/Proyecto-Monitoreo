namespace CyberGuardArch.Core.Interfaces;

public interface IMonitoreoService
{
    /// <summary>
    /// Nombre del monitor (ej: "Network", "FileSystem").
    /// </summary>
    string MonitorName { get; }

    /// <summary>
    /// Inicia el monitoreo de forma asíncrona.
    /// </summary>
    Task StartMonitoringAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Detiene el monitoreo.
    /// </summary>
    Task StopMonitoringAsync();
}
