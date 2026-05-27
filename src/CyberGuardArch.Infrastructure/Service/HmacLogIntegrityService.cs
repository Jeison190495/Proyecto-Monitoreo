using System.Security.Cryptography;
using System.Text;
using CyberGuardArch.Core.Interfaces;
using Microsoft.Extensions.Configuration;

namespace CyberGuardArch.Infrastructure.Service;

public sealed class HmacLogIntegrityService(IConfiguration configuration) : ILogIntegrityService
{
    private readonly byte[] _key = Encoding.UTF8.GetBytes(
        configuration["Security:LogKey"]
        ?? throw new InvalidOperationException("Error de configuracion: 'Security:LogKey' no está configurado.")
    );

    public string SignContent(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return string.Empty;

        byte[] inputBytes = Encoding.UTF8.GetBytes(content);
        //HMACSHA256 tipo de encriptacion para generar el hash del contenido del log
        byte[] hashBytes = HMACSHA256.HashData(_key, inputBytes);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }
}