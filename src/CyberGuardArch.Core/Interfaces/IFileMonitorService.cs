namespace CyberGuardArch.Core.Interfaces;
public interface IFileMonitorService
{
    /// <summary>
    /// Inicia el monitoreo en la ruta especificada.
    /// </summary>
    void StartMonitoring(string path);

    /// <summary>
    /// Evento que se dispara cuando se detecta un cambio.
    /// Devuelve: (Tipo de Cambio, Ruta del Archivo)
    /// </summary>
    event Action<string, string> OnFileChanged;
}