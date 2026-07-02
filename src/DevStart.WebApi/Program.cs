using DevStart.Application;
using DevStart.WebApi;
using DevStart.WebApi.Extensions;
using DevStart.WebApi.Infrastructure;
using Serilog;
using DevStart.Infrastructure;
using Hangfire;
using System.Reflection;
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

builder.Services.AddObservability(builder.Configuration, builder.Environment);

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
}

app.UseRequestContextLogging();
app.UseSerilogRequestLogging();

app.UseExceptionHandler();

app.UseAuthentication();
app.UseAuthorization();

// Exposed in all environments but gated: dev-open, else trusted-proxy header or authenticated admin.
// Placed after auth so the bearer-token path has a populated principal.
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    // Authorization defaults to LocalRequestsOnlyAuthorizationFilter, which would deny all proxied
    // traffic — empty it explicitly; the async filter below is the sole gate.
    Authorization = [],
    AsyncAuthorization = [new HangfireDashboardAuthorizationFilter(app.Environment, app.Configuration)]
});

// Mapped after auth so /health/details can enforce the admin authorization policy.
app.MapHealthCheckEndpoints();

// Prometheus scrape endpoint (/metrics) — kept internal to the network via nginx in prod.
app.MapPrometheusScrapingEndpoint();

app.UseRateLimiter();

app.MapControllers();

await app.RunAsync();

namespace DevStart.WebApi
{
    public partial class Program;
}
