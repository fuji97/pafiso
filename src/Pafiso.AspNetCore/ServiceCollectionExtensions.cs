using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Pafiso.AspNetCore;

/// <summary>
/// Extension methods for configuring Pafiso in an ASP.NET Core application.
/// </summary>
public static class ServiceCollectionExtensions {
    /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
    extension(IServiceCollection services) {
        /// <summary>
        /// Adds Pafiso services to the specified <see cref="IServiceCollection"/>.
        /// </summary>
        /// <param name="configure">An optional action to configure <see cref="PafisoSettings"/>.</param>
        /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
        /// <remarks>
        /// This method will automatically configure the <see cref="PafisoSettings.PropertyNamingPolicy"/>
        /// from MVC's <see cref="JsonOptions"/> if not explicitly configured.
        /// </remarks>
        public IServiceCollection AddPafiso(Action<PafisoSettings>? configure = null) {
            services.AddSingleton(sp => {
                var settings = new PafisoSettings();

                // Auto-configure from MVC's JsonSerializerOptions if available
                var jsonOptions = sp.GetService<IOptions<JsonOptions>>();
                if (jsonOptions?.Value?.JsonSerializerOptions?.PropertyNamingPolicy != null) {
                    settings.PropertyNamingPolicy = jsonOptions.Value.JsonSerializerOptions.PropertyNamingPolicy;
                }

                // Apply custom configuration (can override auto-detected values)
                configure?.Invoke(settings);

                // Also set as the global default
                PafisoSettings.Default = settings;

                return settings;
            });

            return services;
        }

        /// <summary>
        /// Adds Pafiso services to the specified <see cref="IServiceCollection"/>
        /// with a pre-configured settings instance.
        /// </summary>
        /// <param name="settings">The pre-configured <see cref="PafisoSettings"/> instance.</param>
        /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
        public IServiceCollection AddPafiso(PafisoSettings settings) {
            // Set as the global default
            PafisoSettings.Default = settings;

            services.AddSingleton(settings);

            return services;
        }
    }
}
