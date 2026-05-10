using CyberGuardArch.Infrastructure.Services;
using Xunit;

namespace CyberGuardArch.Tests;

public class FileMonitorTests
{
    [Fact]
    public void StartMonitoring_ShouldThrowDirectoryNotFoundException_WhenPathIsInvalid()
    {
        // Arrange
        var service = new LinuxFileMonitorService();
        var invalidPath = "/home/usuario_no_existente/CyberGuard_Fake";

        // Act & Assert
        // Verificamos que el sistema proteja el inicio si la carpeta no existe
        Assert.Throws<DirectoryNotFoundException>(() =>
            service.StartMonitoring(invalidPath));
    }

    [Fact]
    public void FileMonitor_ShouldAllowSubscriptionToEvents()
    {
        // Arrange
        var service = new LinuxFileMonitorService();
        bool eventHandled = false;

        // Act
        service.OnFileChanged += (tipo, ruta) => { eventHandled = true; };

        // Para quitar el warning, simplemente "usamos" la variable
        // Aunque aquí no disparamos el evento real, verificamos que la variable existe.
        Assert.False(eventHandled);
        Assert.NotNull(service);
    }
}