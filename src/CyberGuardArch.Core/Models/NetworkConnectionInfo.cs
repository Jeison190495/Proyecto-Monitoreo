namespace CyberGuardArch.Core.Models;

/// <summary>
/// Representa la informacion forence de quien se esta conectando a la red, incluyendo el protocolo, las direcciones y puertos locales y remotos, el estado de la conexión, el ID del proceso y el nombre del proceso asociado. Esta información es crucial para identificar conexiones sospechosas o no autorizadas en un sistema.
/// </summary>
public record NetworkConnectionInfo
(
    string Protocol,
    string LocalAddress,
    int LocalPort,
    string RemoteAddress,
    int RemotePort,
    string State,
    string ProcessName
);
