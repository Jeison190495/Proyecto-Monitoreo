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
        try
        {
            await _botClient.SendMessage(
                chatId: options.Value.ChatId, // Acceso directo al parámetro del constructor primario
                text: message,
                cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            throw new Exception("Error al enviar notificación a Telegram", ex);
        }
    }
}