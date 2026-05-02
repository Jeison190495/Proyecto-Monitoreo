namespace CyberGuardArch.Core.Interfaces;

public interface INotificationService
{
    /// <summary>
    /// Envía una notificación de forma asíncrona.
    /// </summary>
    /// <param name="message">El contenido del mensaje.</param>
    /// <param name="cancellationToken">Token para cancelar la operación si el servicio se detiene.</param>
    Task SendNotificationAsync(string message, CancellationToken cancellationToken);
}