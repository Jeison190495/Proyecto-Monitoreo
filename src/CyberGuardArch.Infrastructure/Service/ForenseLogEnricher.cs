using CyberGuardArch.Core.Interfaces;
using Serilog.Core;
using Serilog.Events;

namespace CyberGuardArch.Infrastructure.Service;

public class ForenseLogEnricher(ILogIntegrityService integrityService) : ILogEventEnricher
{
    public void Enrich (LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        if (logEvent.Level < LogEventLevel.Warning)
            return;
        
        string tipo = logEvent.Properties.TryGetValue("Tipo", out var t) ? t.ToString().Trim('"') : "GENERAL";
        string ruta = logEvent.Properties.TryGetValue("Ruta", out var r) ? r.ToString().Trim('"') : "N/A";
        string timespamp = logEvent.Timestamp.ToString("o");

        string semillaForense = $"{timespamp}|{logEvent.Level}|{tipo}|{ruta}";

        string firmaDigital = integrityService.SignContent(semillaForense);

        var property = propertyFactory.CreateProperty("ForenseHash", firmaDigital);
        logEvent.AddPropertyIfAbsent(property);

    }
}