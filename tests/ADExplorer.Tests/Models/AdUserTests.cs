using ADExplorer.Models;
using Xunit;

namespace ADExplorer.Tests.Models;

public class AdUserTests
{
    [Fact]
    public void PasswordExpiryDays_ReturnsNegative_WhenAlreadyExpired()
    {
        var user = new AdUser
        {
            PasswordExpiryDate = DateTime.Now.AddDays(-5)
        };
        Assert.True(user.PasswordExpiryDays < 0);
    }

    [Fact]
    public void PasswordExpiryDays_ReturnsPositive_WhenNotExpired()
    {
        var user = new AdUser
        {
            PasswordExpiryDate = DateTime.Now.AddDays(14)
        };
        Assert.InRange(user.PasswordExpiryDays, 13, 15);
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
