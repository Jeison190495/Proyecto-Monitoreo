using Moq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using CyberGuardArch.Infrastructure.Services;
using Xunit;

namespace CyberGuardArch.Tests;

public class LinuxNetworkMonitorServiceTests
{
    private readonly Mock<ILogger<LinuxNetworkMonitorService>> _loggerMock;
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly Mock<IConfigurationSection> _configSeccionMock;
    private readonly LinuxNetworkMonitorService _service;

    public LinuxNetworkMonitorServiceTests()
    {
        _loggerMock = new Mock<ILogger<LinuxNetworkMonitorService>>();
        _configurationMock = new Mock<IConfiguration>();
        _configSeccionMock = new Mock<IConfigurationSection>();

        // Configurar los valores simulados que lee el "EscudoRed"
        _configSeccionMock.Setup(s => s.GetSection("VentanaTiempoSegundos").Value).Returns("60");
        _configSeccionMock.Setup(s => s.GetSection("MaxIntentosPermitidos").Value).Returns("3");
        _configSeccionMock.Setup(s => s.GetSection("MinutosEnfriamientoAlerta").Value).Returns("5");

        // Vincular la sección "Monitoreo:EscudoRed" al Mock principal
        _configurationMock
            .Setup(c => c.GetSection("Monitoreo:EscudoRed"))
            .Returns(_configSeccionMock.Object);

        // 🌟 Pasamos el mock de configuración al constructor corregido
        _service = new LinuxNetworkMonitorService(_loggerMock.Object, _configurationMock.Object);
    }

    [Fact]
    public void Test_ParseAndEvaluateConnections_DeberiaPromoverIPaVIP()
    {
        // Tu lógica de prueba actual aquí...
        string rawOutputSample = "tcp   ESTAB 0   0               192.168.0.9:22         191.95.55.125:17565";
        
        // Act
        _service.ParseAndEvaluateConnections(rawOutputSample);

        // Assert
        // Las verificaciones que ya tenías programadas...
    }
}