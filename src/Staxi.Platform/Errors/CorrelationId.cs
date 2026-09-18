namespace Staxi.Platform.Errors;

public static class PlatformHeaders
{
    public const string CorrelationId = "corrId";
}

public interface ICorrelationIdAccessor
{
    public string Current { get; }
}

public sealed class AsyncLocalCorrelationIdAccessor : ICorrelationIdAccessor
{
    private static readonly AsyncLocal<string?> HienTai = new();

    public string Current => HienTai.Value ?? string.Empty;

    internal static IDisposable Dat(string corrId)
    {
        var truoc = HienTai.Value;
        HienTai.Value = corrId;
        return new KhoiPhuc(truoc);
    }

    private sealed class KhoiPhuc(string? truoc) : IDisposable
    {
        public void Dispose() => HienTai.Value = truoc;
    }
}
