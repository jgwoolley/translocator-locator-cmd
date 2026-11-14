using Xunit;

namespace Nf3t.VintageStory.Screenshots.Tests;

public class NavigatorCompatibilityTests
{
    [Theory]
    [InlineData("2.2.0", true)]
    [InlineData("2.10.1", true)]
    [InlineData("2.1.0", false)]
    [InlineData("1.0.3", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void Routes_need_2_2_0_or_later(string? version, bool expected)
    {
        Assert.Equal(expected, NavigatorCompatibility.SupportsRouteEvent(version));
    }
}
