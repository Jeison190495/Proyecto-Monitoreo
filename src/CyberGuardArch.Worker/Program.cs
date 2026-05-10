using CyberGuardArch.Core.Configuration;
using CyberGuardArch.Core.Interfaces;
using CyberGuardArch.Infrastructure.Services;
using CyberGuardArch.Worker;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning) // Filtramos ruido de .NET
    /*
    //mostrar el log en archivo log
    .WriteTo.Console()
    .WriteTo.File(
        "Logs/CiberGuard_Auditory.log",
        rollingInterval: RollingInterval.Day,
        buffered: false, //dejar en falso para que imprima en tiempo real
        flushToDiskInterval: TimeSpan.FromSeconds(1), //vacia el buffer cada segundo
        outputTemplate: "{Timestamp: dd--MM--yyyy HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}"
    */
    .WriteTo.Console(outputTemplate: "{Timestamp: dd/MM/yyyy HH:mm:ss} [{Level}] {Message}{NewLine}{Exception}")  //log en consola 
    .WriteTo.File(
        new RenderedCompactJsonFormatter(), // formato actual limpo y estructurado en json para serilog
        "Logs/CiberGuard_Audit.json", //nombre del archivo de log y su ruta
        rollingInterval: RollingInterval.Month, //creacion de un nuevo archivo cada mes puede cambiarse por dia 
        flushToDiskInterval: TimeSpan.FromSeconds(1), //vacia el buffer cada segundo
        buffered: false //permite que no se pierda el log en caso de un fallo del sistema, se escribe directamente en el archivo sin usar un buffer.
    )
    .CreateLogger();
try
{
    Log.Information("Iniciando Monitoreo...");

    var builder = Host.CreateApplicationBuilder(args);

    builder.Services.AddSerilog();
    builder.Services.Configure<TelegramOptions>(builder.Configuration.GetSection(TelegramOptions.SectionName));
    builder.Services.AddSingleton<INotificationService, TelegramNotificationService>();

    builder.Services.AddHostedService<CyberGuardArchWorker>();
    builder.Services.AddSingleton<IFileMonitorService, LinuxFileMonitorService>();

    var host = builder.Build();
    host.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "CyberGuardArch Worker terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

