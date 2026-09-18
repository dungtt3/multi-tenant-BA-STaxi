using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Staxi.Platform.AspNetCore;
using Staxi.Platform.Caching;
using Staxi.Platform.Data;
using Staxi.Platform.Errors;
using Staxi.Platform.Tenancy;
using Staxi.Platform.Time;

namespace Staxi.Platform.DependencyInjection;

public static class PlatformServiceCollectionExtensions
{
    public static IServiceCollection AddStaxiPlatform(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddLogging();
        services.AddMemoryCache();

        services.TryAddSingleton<AsyncLocalTenantScopeAccessor>();
        services.TryAddSingleton<ITenantScopeAccessor>(sp => sp.GetRequiredService<AsyncLocalTenantScopeAccessor>());
        services.TryAddSingleton<ITenantScopeBinder>(sp => sp.GetRequiredService<AsyncLocalTenantScopeAccessor>());

        services.TryAddSingleton<ICorrelationIdAccessor, AsyncLocalCorrelationIdAccessor>();
        services.TryAddSingleton<IClock, SystemClock>();
        services.TryAddSingleton<ITenantCache, MemoryTenantCache>();
        services.TryAddSingleton<IAcrossTenantAuditSink, LoggingAcrossTenantAuditSink>();

        services.TryAddSingleton<ITenantRegistry, CachingTenantRegistry>();

        services.TryAddSingleton<IDbConnectionSource, SqlServerConnectionSource>();
        services.TryAddSingleton<IConnectionScopeVerifier, SqlServerConnectionScopeVerifier>();
        services.TryAddScoped<ITenantConnectionFactory, TenantConnectionFactory>();
        services.TryAddScoped<IAuthenticationConnectionFactory, AuthenticationConnectionFactory>();

        services.TryAddSingleton<ScopeParameterGuardOptions>();

        return services;
    }
}

public static class PlatformApplicationBuilderExtensions
{
    public static IApplicationBuilder UseStaxiPlatformErrors(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.UseMiddleware<PlatformProblemDetailsMiddleware>();
        app.UseMiddleware<ScopeParameterGuardMiddleware>();

        return app;
    }

    public static IApplicationBuilder UseStaxiTenantScope(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        return app.UseMiddleware<HttpContextTenantScopeMiddleware>();
    }
}
