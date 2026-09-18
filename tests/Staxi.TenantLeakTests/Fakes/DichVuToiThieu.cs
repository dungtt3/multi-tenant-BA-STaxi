using System.Data.Common;
using Microsoft.Extensions.DependencyInjection;
using Staxi.Platform.Data;
using Staxi.Platform.DependencyInjection;
using Staxi.Platform.Tenancy;

namespace Staxi.TenantLeakTests.Fakes;

public sealed class DichVuToiThieu : IAsyncDisposable
{
    private readonly ServiceProvider _provider;

    public DichVuToiThieu(ITenantRegistryStore store)
    {
        var services = new ServiceCollection();

        services.AddSingleton<IDbConnectionSource, SqliteConnectionSource>();
        services.AddSingleton<IConnectionScopeVerifier, SqliteConnectionScopeVerifier>();
        services.AddSingleton(store);
        services.AddStaxiPlatform();

        _provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateScopes = true,
            ValidateOnBuild = true,
        });
    }

    public IServiceProvider Services => _provider;

    public async Task<DbConnection> MoKetNoiAsync(TenantScope scope)
    {
        using var diScope = _provider.CreateScope();
        var binder = _provider.GetRequiredService<ITenantScopeBinder>();
        var factory = diScope.ServiceProvider.GetRequiredService<ITenantConnectionFactory>();

        using var _ = binder.Bind(scope);

        return await factory.OpenAsync();
    }

    public async ValueTask DisposeAsync() => await _provider.DisposeAsync();
}
