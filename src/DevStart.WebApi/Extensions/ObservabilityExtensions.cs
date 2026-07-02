using Npgsql;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace DevStart.WebApi.Extensions
{
    internal static class ObservabilityExtensions
    {
        // Wires OpenTelemetry metrics (scraped by Prometheus at /metrics) and distributed tracing
        // (exported via OTLP to Tempo). Logs stay on the existing Serilog → Seq pipeline.
        internal static IServiceCollection AddObservability(
            this IServiceCollection services,
            IConfiguration configuration,
            IHostEnvironment environment)
        {
            string? otlpEndpoint = configuration["OpenTelemetry:Otlp:Endpoint"];

            // TraceIdRatioBasedSampler throws outside [0, 1]; clamp so a misconfigured ratio
            // degrades sampling instead of crashing startup.
            double samplingRatio = Math.Clamp(
                configuration.GetValue("OpenTelemetry:Tracing:SamplingRatio", 1.0), 0.0, 1.0);

            // The OTLP collector is optional infrastructure — a malformed endpoint must not take the
            // API down. Parse up front; when invalid, run without an exporter and log a warning once
            // the host starts (no logger exists yet at registration time).
            Uri? otlpUri = null;
            bool otlpEndpointInvalid =
                !string.IsNullOrWhiteSpace(otlpEndpoint)
                && !Uri.TryCreate(otlpEndpoint, UriKind.Absolute, out otlpUri);

            string serviceVersion =
                typeof(ObservabilityExtensions).Assembly.GetName().Version?.ToString() ?? "unknown";

            services.AddOpenTelemetry()
                .ConfigureResource(resource => resource
                    .AddService(serviceName: "devstart-api", serviceVersion: serviceVersion)
                    .AddAttributes(
                    [
                        new KeyValuePair<string, object>("deployment.environment", environment.EnvironmentName)
                    ]))
                .WithMetrics(metrics => metrics
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation()
                    .AddPrometheusExporter())
                .WithTracing(tracing =>
                {
                    tracing
                        .SetSampler(new ParentBasedSampler(new TraceIdRatioBasedSampler(samplingRatio)))
                        .AddAspNetCoreInstrumentation()
                        .AddHttpClientInstrumentation()
                        .AddNpgsql()
                        .AddRedisInstrumentation();

                    // Only export when a valid OTLP collector endpoint is configured; otherwise tracing
                    // runs in-process without a sink (the app must not depend on a reachable collector).
                    if (otlpUri is not null)
                    {
                        tracing.AddOtlpExporter(otlp => otlp.Endpoint = otlpUri);
                    }
                });

            if (otlpEndpointInvalid)
            {
                services.AddSingleton<IHostedService>(sp => new InvalidOtlpEndpointWarning(
                    sp.GetRequiredService<ILogger<InvalidOtlpEndpointWarning>>(), otlpEndpoint!));
            }

            return services;
        }

        private sealed class InvalidOtlpEndpointWarning(
            ILogger<InvalidOtlpEndpointWarning> logger,
            string configuredValue) : IHostedService
        {
            public Task StartAsync(CancellationToken cancellationToken)
            {
                logger.LogWarning(
                    "Configuration 'OpenTelemetry:Otlp:Endpoint' is not a valid absolute URI: {OtlpEndpoint}. OTLP trace export is disabled.",
                    configuredValue);

                return Task.CompletedTask;
            }

            public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        }
    }
}
