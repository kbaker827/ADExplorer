using ADExplorer.Models;
using Xunit;

namespace ADExplorer.Tests.Models;

public class AdUserTests
{
    [Fact]
    public void PasswordExpiryDays_ReturnsNegative_WhenAlreadyExpired()
    {
        var reference = new DateTime(2026, 6, 15);
        var user = new AdUser
        {
            PasswordExpiryDate = reference.AddDays(-5)
        };
        Assert.True(user.PasswordExpiryDays(reference) < 0);
    }

    [Fact]
    public void PasswordExpiryDays_ReturnsPositive_WhenNotExpired()
    {
        var reference = new DateTime(2026, 6, 15);
        var user = new AdUser
        {
            PasswordExpiryDate = reference.AddDays(14)
        };
        Assert.Equal(14, user.PasswordExpiryDays(reference));
    }

    [Fact]
    public void PasswordExpiryDays_ReturnsNull_WhenNoExpiry()
    {
        var user = new AdUser { PasswordExpiryDate = null };
        Assert.Null(user.PasswordExpiryDays());
    }

    [Fact]
    public void PasswordExpiryDays_ReturnsNegative_WhenExpiredLessThanADayAgo()
    {
        var reference = new DateTime(2026, 6, 15, 12, 0, 0);
        var user = new AdUser { PasswordExpiryDate = reference.AddHours(-2) };
        Assert.Equal(-1, user.PasswordExpiryDays(reference));
    }

    [Theory]
    [InlineData(-3.0, "Expired (2026-06-12)")]
    [InlineData(0.5, "Expires in under a day (2026-06-16)")]
    [InlineData(1.0, "1 day (2026-06-16)")]
    [InlineData(14.0, "14 days (2026-06-29)")]
    public void PasswordExpiryDisplay_FormatsRelativeExpiry(double daysFromNow, string expected)
    {
        var reference = new DateTime(2026, 6, 15, 12, 0, 0);
        var user = new AdUser { PasswordExpiryDate = reference.AddDays(daysFromNow) };
        Assert.Equal(expected, user.PasswordExpiryDisplay(reference));
    }

    [Fact]
    public void PasswordExpiryDisplay_ReturnsNoExpiry_WhenNull()
    {
        Assert.Equal("No expiry", new AdUser().PasswordExpiryDisplay());
    }

    [Fact]
    public void LastLogonDisplay_ReturnsNever_WhenNull()
    {
        var user = new AdUser { LastLogon = null };
        Assert.Equal("Never", user.LastLogonDisplay);
    }

    [Fact]
    public void LastLogonDisplay_ReturnsFormattedDate_WhenSet()
    {
        var date = new DateTime(2026, 6, 14, 8, 2, 0);
        var user = new AdUser { LastLogon = date };
        Assert.Equal("2026-06-14 08:02", user.LastLogonDisplay);
    }
}
