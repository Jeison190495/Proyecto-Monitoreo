using Moq;
using CyberGuardArch.Core.Interfaces;
using CyberGuardArch.Core.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using CyberGuardArch.Worker;
using Microsoft.Extensions.Configuration;

namespace CyberGuardArch.Tests;

public class WorkerTests
{
    [Fact]
    public async Task Worker_ShouldMonitorMultiplePaths_FromConfiguration()
    {
        // 1. Arrange
        var mockLogger = new Mock<ILogger<CyberGuardArchWorker>>(); // Mock del logger para evitar problemas de logging en el test
        var mockNotification = new Mock<INotificationService>();    // Mock del servicio de notificaciones para verificar que se envíe la notificación correctamente
        var mockMonitor = new Mock<IFileMonitorService>();          // Mock del servicio de monitoreo de archivos para simular la detección de un cambio
        var mockNetworkMonitor = new Mock<INetworkMonitorService>();// Mock del servicio de monitoreo de red, aunque no lo usaremos en este test, es necesario para la construcción del Worker
        var mockCommandHistory = new Mock<ICommandHistoryService>();  // Mock del servicio de historial de comandos, aunque no lo usaremos en este test, es necesario para la construcción del Worker

        // Usamos una lista simple de strings para asegurar el binding
        var rutasData = new Dictionary<string, string?>
    {
        {"Monitoreo:Rutas:0", "/ruta1"},
        {"Monitoreo:Rutas:1", "/ruta2"}
    };

        var myConfiguration = new ConfigurationBuilder()
            .AddInMemoryCollection(rutasData)
            .Build();

        var options = Options.Create(new TelegramOptions { Token = "t", ChatId = "c", NameBot = "b" });

        var worker = new CyberGuardArchWorker(
            mockLogger.Object,          // Mock del logger para evitar problemas de logging en el test
            options,                    // Opciones de Telegram con valores dummy, ya que no se probará la parte de notificaciones en este test
            mockNotification.Object,    // Mock del servicio de notificaciones para verificar que se envíe la notificación correctamente
            mockMonitor.Object,         // Mock del servicio de monitoreo de archivos para simular la detección de un cambio
            mockNetworkMonitor.Object,  // Mock del servicio de monitoreo de red, aunque no lo usaremos en este test, es necesario para la construcción del Worker
            mockCommandHistory.Object,  // Mock del servicio de historial de comandos, aunque no lo usaremos en este test, es necesario para la construcción del Worker
            myConfiguration);           // Configuración personalizada para el test, con las rutas que queremos verificar

        // 2. Act
        await worker.StartAsync(CancellationToken.None);

        // IMPORTANTE: Damos un tiempo pequeño para que el hilo de fondo procese el foreach
        await Task.Delay(100);

        // 3. Assert
        mockMonitor.Verify(m => m.StartMonitoring("/ruta1"), Times.Once, "Prueba fallida para /ruta1");
        mockMonitor.Verify(m => m.StartMonitoring("/ruta2"), Times.Once, "Prueba fallida para /ruta2");

        await worker.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task Worker_ShouldSendNotification_WhenFileMonitorDetectsChange()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<CyberGuardArchWorker>>(); // Mock del logger para evitar problemas de logging en el test
        var mockNotification = new Mock<INotificationService>();    // Mock del servicio de notificaciones para verificar que se envíe la notificación correctamente
        var mockMonitor = new Mock<IFileMonitorService>();          // Mock del servicio de monitoreo de archivos para simular la detección de un cambio    
        var mockNetworkMonitor = new Mock<INetworkMonitorService>();// Mock del servicio de monitoreo de red, aunque no lo usaremos en este test, es necesario para la construcción del Worker
        var mockCommandHistory = new Mock<ICommandHistoryService>();  // Mock del servicio de historial de comandos, aunque no lo usaremos en este test, es necesario para la construcción del Worker

        // Configuración vacía para este test
        var myConfiguration = new ConfigurationBuilder().Build();

        var options = Options.Create(new TelegramOptions
        {
            Token = "valid_token",
            ChatId = "123",
            NameBot = "SentinelBot"
        });

        var worker = new CyberGuardArchWorker(
            mockLogger.Object,
            options,
            mockNotification.Object,
            mockMonitor.Object,
            mockNetworkMonitor.Object,
            mockCommandHistory.Object, // Mock del servicio de historial de comandos, aunque no lo usaremos en este test, es necesario para la construcción del Worker
            myConfiguration);

        // Act
        await worker.StartAsync(CancellationToken.None);

        // Simulamos el evento
        mockMonitor.Raise(m => m.OnFileChanged += null, "CREADO", "/home/jeison/test.txt");

        await Task.Delay(100);
        await worker.StopAsync(CancellationToken.None);

        // Assert
        mockNotification.Verify(n => n.SendNotificationAsync(
            It.Is<string>(s => s.Contains("CREADO")),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }
}