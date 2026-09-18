using Xunit;

namespace Staxi.ArchitectureTests.Assertions;

internal static class ArchitectureAssert
{
    public static void NoViolations(string rule, string decision, string requirement, IEnumerable<string> violations)
    {
        var locations = violations.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();
        if (locations.Length > 0)
        {
            Assert.Fail($"{rule} ({decision}): {requirement}{Environment.NewLine}Các vị trí vi phạm:{Environment.NewLine}"
                        + string.Join(Environment.NewLine, locations.Select(location => $"- {location}")));
        }
    }
}
