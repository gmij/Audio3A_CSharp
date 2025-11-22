using Microsoft.Extensions.DependencyInjection;
using Audio3A.Core.Processors;

namespace Audio3A.Core.Extensions;

/// <summary>
/// Extension methods for registering Audio 3A services with dependency injection
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Audio 3A services to the service collection
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configureOptions">Optional configuration action</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddAudio3A(
        this IServiceCollection services,
        Action<Audio3AConfig>? configureOptions = null)
    {
        // Register configuration
        var config = new Audio3AConfig();
        configureOptions?.Invoke(config);
        services.AddSingleton(config);

        // Conditionally register processors as scoped services based on configuration
        if (config.EnableAec)
        {
            services.AddScoped<AecProcessor>(sp =>
            {
                var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<AecProcessor>>();
                var cfg = sp.GetRequiredService<Audio3AConfig>();
                return new AecProcessor(
                    logger,
                    cfg.SampleRate,
                    cfg.AecFilterLength,
                    cfg.AecStepSize);
            });
        }

        if (config.EnableAgc)
        {
            services.AddScoped<AgcProcessor>(sp =>
            {
                var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<AgcProcessor>>();
                var cfg = sp.GetRequiredService<Audio3AConfig>();
                return new AgcProcessor(
                    logger,
                    cfg.SampleRate,
                    cfg.AgcTargetLevel,
                    cfg.AgcCompressionRatio);
            });
        }

        if (config.EnableAns)
        {
            services.AddScoped<AnsProcessor>(sp =>
            {
                var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<AnsProcessor>>();
                var cfg = sp.GetRequiredService<Audio3AConfig>();
                return new AnsProcessor(
                    logger,
                    cfg.SampleRate,
                    noiseReductionDb: cfg.AnsNoiseReductionDb);
            });
        }

        // Register main processor
        services.AddScoped<Audio3AProcessor>();

        return services;
    }
}
