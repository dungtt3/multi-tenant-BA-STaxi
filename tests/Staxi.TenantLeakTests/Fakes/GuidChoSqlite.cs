using System.Data;
using Dapper;

namespace Staxi.TenantLeakTests.Fakes;

public sealed class GuidChoSqlite : SqlMapper.TypeHandler<Guid>
{
    private static bool _daDangKy;

    public static void DangKyMotLan()
    {
        if (_daDangKy)
        {
            return;
        }

        SqlMapper.AddTypeHandler(new GuidChoSqlite());
        _daDangKy = true;
    }

    public override Guid Parse(object value)
        => value switch
        {
            Guid guid => guid,
            string chuoi => Guid.Parse(chuoi),
            byte[] bytes => new Guid(bytes),
            _ => throw new InvalidCastException($"Không đổi được '{value?.GetType().Name}' sang Guid."),
        };

    public override void SetValue(IDbDataParameter parameter, Guid value)
    {
        parameter.DbType = DbType.String;
        parameter.Value = value.ToString("n");
    }
}
