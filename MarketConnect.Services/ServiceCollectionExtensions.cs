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
            services.AddScoped<IProductService, ProductService>();
            services.AddScoped<IMultiMarketProductService, MultiMarketProductService>();
            services.AddScoped<IMerchantStoreService, MerchantStoreService>();
            services.AddScoped<IContentModerationService, ContentModerationService>();
            services.AddScoped<IMultiMerchantCartService, MultiMerchantCartService>();
            services.AddScoped<IReviewAbuseService, ReviewAbuseService>();
            services.AddScoped<IAdService, AdService>();
            services.AddScoped<IMobileVendorService, MobileVendorService>();
            services.AddScoped<IAuditLogService, AuditLogService>();

            return services;
        }
    }
}
