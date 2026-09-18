namespace Staxi.Platform.Tenancy;

public sealed class TenantScopeMissingException : InvalidOperationException
{
    public TenantScopeMissingException(string thaoTac)
        : base($"Không có TenantScope cho thao tác '{thaoTac}'. AD-2: mọi truy cập dữ liệu phải nằm trong một phạm vi đã phân giải từ token đã ký.")
    {
        ThaoTac = thaoTac;
    }

    public string ThaoTac { get; }
}

public sealed class TenantScopeConflictException : InvalidOperationException
{
    public TenantScopeConflictException(TenantScope dangCo, TenantScope muonDat)
        : base($"Đang có TenantScope '{dangCo}' mà cố đặt sang '{muonDat}'. Phạm vi bất biến trong một request (AD-4).")
    {
        DangCo = dangCo;
        MuonDat = muonDat;
    }

    public TenantScope DangCo { get; }

    public TenantScope MuonDat { get; }
}

public sealed class TenantConnectionMismatchException : InvalidOperationException
{
    public TenantConnectionMismatchException(TenantScope scope, string databaseMongDoi, string databaseThucTe)
        : base($"Rò rỉ phạm vi: hãng '{scope.TenantCode}' mong đợi database '{databaseMongDoi}' nhưng kết nối vật lý đang ở '{databaseThucTe}' (AD-18).")
    {
        Scope = scope;
        DatabaseMongDoi = databaseMongDoi;
        DatabaseThucTe = databaseThucTe;
    }

    public TenantScope Scope { get; }

    public string DatabaseMongDoi { get; }

    public string DatabaseThucTe { get; }
}

public sealed class TenantKhongTonTaiException : InvalidOperationException
{
    public TenantKhongTonTaiException(string tenantCode, string lyDo)
        : base($"Hãng '{tenantCode}' không dùng được: {lyDo}")
    {
        TenantCode = tenantCode;
    }

    public string TenantCode { get; }
}
