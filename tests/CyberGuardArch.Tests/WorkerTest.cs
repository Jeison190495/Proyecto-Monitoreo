using Moq;
using CyberGuardArch.Core.Interfaces;
using CyberGuardArch.Core.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using CyberGuardArch.Worker;

namespace CyberGuardArch.Tests;

public class WorkerTests
{
    [Fact]
    public async Task ExecuteAsync_ShouldNotSendNotification_WhenTokenIsEmpty()
    {
        // Arrange: Preparamos los mocks
        var mockLogger = new Mock<ILogger<CyberGuardArchWorker>>();
        var mockNotification = new Mock<INotificationService>();
        var mockMonitor = new Mock<IFileMonitorService>();

        // Simulamos configuración inválida (Token vacío)
        var options = Options.Create(new TelegramOptions
        {
            Token = "",
            ChatId = "123",
            NameBot = "TestBot"
        });

        var worker = new CyberGuardArchWorker(mockLogger.Object, options, mockNotification.Object, mockMonitor.Object);

        // Act: Ejecutamos el inicio del worker
        using var cts = new CancellationTokenSource();
        await worker.StartAsync(cts.Token);
        await Task.Delay(50); // Tiempo breve para que corra la validación inicial
        await worker.StopAsync(cts.Token);

        // Assert: Verificamos que NO se llamó al servicio de notificación
        mockNotification.Verify(
            x => x.SendNotificationAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);

        // Opcional: Verificar que se logueó el error específico
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Telegram Token no configurado")),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()),
            Times.Once);
    }

    [Fact]
    public async Task Worker_ShouldSendNotification_WhenFileMonitorDetectsChange()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<CyberGuardArchWorker>>();
        var mockNotification = new Mock<INotificationService>();
        var mockMonitor = new Mock<IFileMonitorService>();
        
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
            mockMonitor.Object);

        // Act
        await worker.StartAsync(CancellationToken.None);

        // Simulamos que el monitor lanza un evento de cambio
        mockMonitor.Raise(m => m.OnFileChanged += null, "CREADO", "/home/jeison/CyberGuard_Lab/evidencia.txt");

        await Task.Delay(100); 
        await worker.StopAsync(CancellationToken.None);

        // Assert: Verificamos que el Worker reaccionó al evento enviando a Telegram
        mockNotification.Verify(n => n.SendNotificationAsync(
            It.Is<string>(s => s.Contains("CREADO") && s.Contains("evidencia.txt")), 
            It.IsAny<CancellationToken>()), 
            Times.Once);
    }
}