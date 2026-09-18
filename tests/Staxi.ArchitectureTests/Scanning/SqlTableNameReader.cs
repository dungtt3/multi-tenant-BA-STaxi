using System.Text.RegularExpressions;

namespace Staxi.ArchitectureTests.Scanning;

internal static partial class SqlTableNameReader
{
    public static IEnumerable<string> Read(string sql)
        => MauBang().Matches(sql)
            .Select(khop => khop.Groups[1].Value)
            .Where(LaTenBangThat)
            .Select(ChuanHoa)
            .Where(ten => ten.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase);

    public static string ChuanHoa(string ten)
        => ten.Replace("[", string.Empty, StringComparison.Ordinal)
            .Replace("]", string.Empty, StringComparison.Ordinal)
            .Trim();

    private static bool LaTenBangThat(string ten)
        => !ten.StartsWith('#') && !ten.StartsWith('@') && !ten.StartsWith("dbo.#", StringComparison.OrdinalIgnoreCase);

    [GeneratedRegex(
        @"\b(?:from|join|insert\s+into|update|delete\s+from)\s+(\[[^\]]+\](?:\.\[[^\]]+\])*|[A-Za-z_#@][\w$#@.]*)",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex MauBang();
}
