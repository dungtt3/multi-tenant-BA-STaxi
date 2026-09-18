using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Staxi.Platform.DependencyInjection;

public static class PlatformHost
{
    public static IHostBuilder UseStaxiServiceProviderValidation(this IHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.UseDefaultServiceProvider(options =>
        {
            options.ValidateScopes = true;
            options.ValidateOnBuild = true;
        });
    }
}
