using ADExplorer.Services;
using Xunit;

namespace ADExplorer.Tests.Services;

public class AdServiceTests
{
    [Fact]
    public void ParseGroupType_ReturnsSecurity_ForNegativeValue()
    {
        var result = AdService.ParseGroupType(-2147483646);
        Assert.Equal("Security", result);
    }

    [Fact]
    public void ParseGroupType_ReturnsDistribution_ForPositiveValue()
    {
        var result = AdService.ParseGroupType(2);
        Assert.Equal("Distribution", result);
    }

    [Fact]
    public void ParseFileTime_ReturnsNull_ForZero()
    {
        var result = AdService.ParseFileTime(0);
        Assert.Null(result);
    }

    [Fact]
    public void ParseFileTime_ReturnsNull_ForMaxValue()
    {
        var result = AdService.ParseFileTime(long.MaxValue);
        Assert.Null(result);
    }

    [Fact]
    public void ParseFileTime_ReturnsDateTime_ForValidValue()
    {
        // Use noon UTC so local-time conversion does not cross a year boundary
        var fileTime = new DateTime(2026, 6, 15, 12, 0, 0, DateTimeKind.Utc).ToFileTime();
        var result = AdService.ParseFileTime(fileTime);
        Assert.NotNull(result);
        Assert.Equal(2026, result!.Value.ToUniversalTime().Year);
    }

    [Fact]
    public void EscapeLdap_EscapesSpecialCharacters()
    {
        var result = AdService.EscapeLdap("test*(user)\\name");
        Assert.Equal("test\\2a\\28user\\29\\5cname", result);
    }

    [Fact]
    public void EscapeLdap_ReturnsUnchanged_ForPlainString()
    {
        var result = AdService.EscapeLdap("john doe");
        Assert.Equal("john doe", result);
    }

    [Fact]
    public void EscapeLdap_EscapesNul()
    {
        Assert.Equal("a\\00b", AdService.EscapeLdap("a\0b"));
    }

    [Theory]
    [InlineData("CN=John Doe,OU=Staff,DC=corp,DC=local", "John Doe")]
    [InlineData("CN=Smith\\, John,OU=Staff,DC=corp,DC=local", "Smith, John")]
    [InlineData("CN=Ren\\C3\\A9e Dupont,OU=Staff,DC=corp", "Renée Dupont")]
    [InlineData("CN=Domain Admins,CN=Users,DC=corp,DC=local", "Domain Admins")]
    [InlineData("OU=Staff,DC=corp", "Staff")]
    [InlineData("plain", "plain")]
    [InlineData("", "")]
    public void ParseCN_ReturnsUnescapedFirstRdnValue(string dn, string expected)
    {
        Assert.Equal(expected, AdService.ParseCN(dn));
    }

    [Fact]
    public void EscapeAdsPath_EscapesForwardSlash()
    {
        Assert.Equal("CN=A\\/B,DC=corp", AdService.EscapeAdsPath("CN=A/B,DC=corp"));
    }

    [Theory]
    [InlineData(-1L)]
    [InlineData(long.MinValue)]
    public void ParseFileTime_ReturnsNull_ForNegative(long value)
    {
        Assert.Null(AdService.ParseFileTime(value));
    }
}
