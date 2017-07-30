using System.Diagnostics;
using Microsoft.AspNetCore.Server.HttpSys;

var builder = WebApplication.CreateBuilder(args);

Debug.Assert(OperatingSystem.IsWindows());
builder.WebHost.UseHttpSys(options =>
{
    options.RequestQueueName = "SampleHttpSysServerQueue";
    options.RequestQueueMode = RequestQueueMode.Create;
});

var app = builder.Build();

app.Run(async context =>
{
    await context.Response.WriteAsync($"Hello World! (PID: {Environment.ProcessId})");
});
app.Run();
