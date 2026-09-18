namespace Staxi.Platform.Errors;

public sealed class ScopeEscalationAttemptException(string nguon, string ten) : InvalidOperationException(
    $"Request mang '{ten}' trong {nguon}. Phạm vi chỉ đến từ token đã ký — đổi công ty phải gọi select-company (AD-4).")
{
    public string Nguon { get; } = nguon;

    public string Ten { get; } = ten;
}
