using DevStart.Application;
using DevStart.WebApi;
using DevStart.WebApi.Extensions;
using Serilog;
using DevStart.Infrastructure;
using Hangfire;
using System.Reflection;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using HealthChecks.UI.Client;
using Microsoft.Extensions.Configuration;

var builder = WebApplication.CreateBuilder(args);


builder.Host.UseSerilog((context, loggerConfig) => loggerConfig.ReadFrom.Configuration(context.Configuration));

builder.Services.AddSwaggerGenWithAuth();

builder.Services
    .AddApplication()
    .AddPresentation()
    .AddInfrastructure(builder.Configuration);

builder.Services.AddEndpoints(Assembly.GetExecutingAssembly());

builder.Services.AddRateLimiting();

builder.Services.AddForwardedHeaders(builder.Configuration);

var app = builder.Build();

app.UseForwardedHeaders();

app.MapEndpoints();

if (app.Configuration.GetValue("Database:RunMigrationsOnStartup", true))
{
    app.ApplyMigrations();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwaggerWithUi();
    app.UseHangfireDashboard("/hangfire");

}

app.MapHealthChecks("health", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

app.UseRequestContextLogging();
app.UseSerilogRequestLogging();

app.UseExceptionHandler();

app.UseAuthentication();
app.UseAuthorization();

app.UseRateLimiter();

app.MapControllers();

await app.RunAsync();

namespace DevStart.WebApi
{
    public partial class Program;
}
