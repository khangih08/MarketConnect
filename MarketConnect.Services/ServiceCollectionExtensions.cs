using MarketConnect.Data;
using MarketConnect.Services.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using System;

namespace MarketConnect.Services
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddAuthService(this IServiceCollection services, IConfiguration config)
        {
            // bind jwt settings from configuration: Jwt:Secret, Jwt:Issuer, Jwt:Audience, Jwt:ExpiryMinutes
            var jwt = new JwtSettings();
            config.GetSection("Jwt").Bind(jwt);

            if (string.IsNullOrWhiteSpace(jwt.Secret))
                throw new InvalidOperationException("JWT Secret is not configured. Set configuration section 'Jwt:Secret'.");

            services.AddSingleton(jwt);
            services.AddScoped<IAuthService, AuthService>();
            // Register application services
            services.AddScoped<IProductService, ProductService>();
            services.AddScoped<IMultiMarketProductService, MultiMarketProductService>();

            return services;
        }
    }
}
