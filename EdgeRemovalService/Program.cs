
using EdgeRemovalService;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddWindowsService(options =>
{
    options.ServiceName = "EdgeRemovalService";
});

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
