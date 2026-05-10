using CyberGuardArch.Core.Configuration;
using CyberGuardArch.Core.Interfaces;
using CyberGuardArch.Infrastructure.Services;
using CyberGuardArch.Worker;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<TelegramOptions>(builder.Configuration.GetSection(TelegramOptions.SectionName));
builder.Services.AddSingleton<INotificationService, TelegramNotificationService>();

builder.Services.AddHostedService<CyberGuardArchWorker>();
builder.Services.AddSingleton<IFileMonitorService, LinuxFileMonitorService>();

var host = builder.Build();
host.Run();
