using Microsoft.AspNetCore.Builder;

namespace Staxi.Platform.Authorization;

public sealed class ThieuQuyenException(Quyen quyen) : InvalidOperationException(
    $"Thiếu quyền '{quyen.Ma}'.")
{
    public Quyen Quyen { get; } = quyen;
}

public static class PermissionEndpointExtensions
{
    public static TBuilder RequirePermission<TBuilder>(this TBuilder builder, Quyen quyen)
        where TBuilder : IEndpointConventionBuilder
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(quyen);

        builder.Add(endpoint => endpoint.Metadata.Add(new QuyenBatBuoc(quyen)));

        return builder;
    }
}

public sealed record QuyenBatBuoc(Quyen Quyen);
