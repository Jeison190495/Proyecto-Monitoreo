using CyberGuardArch.Core.Configuration;
using CyberGuardArch.Core.Interfaces;
using CyberGuardArch.Infrastructure.Service;
using CyberGuardArch.Infrastructure.Services;
using CyberGuardArch.Worker;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;

try
{
    Log.Information("Iniciando Monitoreo...");

    var builder = Host.CreateApplicationBuilder(args);
    builder.Services.AddSingleton<ILogIntegrityService, HmacLogIntegrityService>();
    builder.Services.AddSingleton<ForenseLogEnricher>();
    builder.Services.AddSerilog((services, loggerConfiguration) =>
        {
            var enricher = services.GetRequiredService<ForenseLogEnricher>();
            loggerConfiguration
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .Enrich.With(enricher)
            .WriteTo.Console(outputTemplate: "{Timestamp: dd/MM/yyyy HH:mm:ss} [{Level}] {Message} {NewLine}{Exception}")
            .WriteTo.File(
                    new RenderedCompactJsonFormatter(),
                    "Logs/CiberGuard_Audit.json",
                    rollingInterval: RollingInterval.Month,
                    flushToDiskInterval: TimeSpan.FromSeconds(1),
                    buffered: false
                );
        }
    );
    Log.Information("Iniciando Monitoreo...");

    builder.Services.Configure<TelegramOptions>(builder.Configuration.GetSection(TelegramOptions.SectionName));
    builder.Services.AddSingleton<INotificationService, TelegramNotificationService>();
    builder.Services.AddSingleton<IFileMonitorService, LinuxFileMonitorService>();
    builder.Services.AddHostedService<CyberGuardArchWorker>();


    var host = builder.Build();
    host.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Cerrando con error inesperado");
}
finally
{
    Log.CloseAndFlush();
}