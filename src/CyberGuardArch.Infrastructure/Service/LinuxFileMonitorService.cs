using CyberGuardArch.Core.Interfaces;
namespace CyberGuardArch.Infrastructure.Services;

public class LinuxFileMonitorService : IFileMonitorService, IDisposable
{
    private FileSystemWatcher? _watcher;
    public event Action<string, string>? OnFileChanged;

    public void StartMonitoring(string path)
    {
        if (!Directory.Exists(path))
        {
            throw new DirectoryNotFoundException($"La ruta no existe: {path}");
        }

        _watcher = new FileSystemWatcher(path)
        {
            NotifyFilter = NotifyFilters.FileName           // Monitorea cambios en el nombre del archivo
                         | NotifyFilters.DirectoryName      // Monitorea cambios en el nombre del directorio
                         | NotifyFilters.LastWrite          // Monitorea cambios en la fecha de última escritura
                         | NotifyFilters.Security           // Monitorea cambios en los permisos de seguridad
                         | NotifyFilters.Attributes,        // Monitorea cambios en los atributos del archivo
            IncludeSubdirectories = true,
            EnableRaisingEvents = true
        };

        // Suscribimos a los eventos de creación, modificación y eliminación de archivos
        _watcher.Created += (s, e) => OnFileChanged?.Invoke("CREADO", e.FullPath);
        _watcher.Changed += (s, e) => OnFileChanged?.Invoke("MODIFICADO", e.FullPath);
        _watcher.Deleted += (s, e) => OnFileChanged?.Invoke("ELIMINADO", e.FullPath);
        _watcher.Renamed += (s, e) => OnFileChanged?.Invoke("RENOMBRADO", $"{e.OldFullPath} -> {e.FullPath}");
    }

    public void Dispose()
    {
        _watcher?.Dispose();
    }
}
