namespace Staxi.Platform.Time;

public interface IClock
{
    public DateTimeOffset UtcNow { get; }

    public DateTime LegacyLocalNow { get; }
}

public sealed class SystemClock : IClock
{
    public static readonly TimeSpan OffsetGioVietNam = TimeSpan.FromHours(7);

    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;

    public DateTime LegacyLocalNow => DateTime.SpecifyKind(UtcNow.ToOffset(OffsetGioVietNam).DateTime, DateTimeKind.Unspecified);
}
