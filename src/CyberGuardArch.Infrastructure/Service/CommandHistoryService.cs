using CyberGuardArch.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace CyberGuardArch.Infrastructure.Service;

public class CommandHistoryService(ILogger<CommandHistoryService> logger) : ICommandHistoryService
{
    public List<string> GetLastCommands(string filePath, int count = 3)
    {
        var commands = new List<string>();
        try
        {
            if (!File.Exists(filePath)) return commands;

            // Leer usando FileStream y FileAccess.Read para evitar bloqueos si el sistema operativo está escribiendo en él
            using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var reader = new StreamReader(fs);
            
            var allLines = new List<string>();
            while (reader.ReadLine() is { } line)
            {
                if (!string.IsNullOrWhiteSpace(line))
                {
                    allLines.Add(line);
                }
            }

            // Tomar las últimas líneas
            var lastLines = allLines.Skip(Math.Max(0, allLines.Count - count)).ToList();

            foreach (var line in lastLines)
            {
                // Limpieza para ZSH: Remueve el formato de timestamps si existe ": 171787890;0;comando"
                string cleanLine = line;
                if (line.StartsWith(':'))
                {
                    int lastSemicolon = line.IndexOf(';');
                    if (lastSemicolon != -1 && lastSemicolon + 1 < line.Length)
                    {
                        cleanLine = line[(lastSemicolon + 1)..];
                    }
                }
                commands.Add(cleanLine.Trim());
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error al intentar leer el historial de comandos en {Path}", filePath);
        }

        return commands;
    }
}