using CyberGuardArch.Core.Configuration;
using CyberGuardArch.Core.Interfaces;
using Microsoft.Extensions.Options;
using Telegram.Bot;

namespace CyberGuardArch.Infrastructure.Services;

public class TelegramNotificationService(IOptions<TelegramOptions> options) : INotificationService
{
    private readonly TelegramBotClient _botClient = new(options.Value.Token);

    public async Task SendNotificationAsync(string message, CancellationToken cancellationToken = default)
    {
        int maxIntentos = 10;
        int delayBase = 3000; // 3 segundo
        for (int i = 0; i < maxIntentos; i++)
        {
            try
            {
                await _botClient.SendMessage(
                    chatId: options.Value.ChatId, // Acceso directo al parámetro del constructor primario
                    text: message,
                    cancellationToken: cancellationToken);
                return;
            }
            catch (Exception ex) when (i < maxIntentos - 1)
            {
                await Task.Delay(delayBase, cancellationToken);
                delayBase *= 2; // Incrementa el tiempo de espera para el próximo intento
            }
            catch (Exception ex)
            {
                throw new Exception("Error al enviar notificación a Telegram después de varios intentos", ex);
            }
        }
    }
}