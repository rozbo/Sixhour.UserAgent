namespace SixHour.UserAgent.Tests;

public class UserAgentUserAgentTests : SixHourUserAgentTestBase<SixHourUserAgentTestModule>
{
    private readonly IUserAgentParser _userAgentParser;

    public UserAgentUserAgentTests()
    {
        _userAgentParser = GetService<IUserAgentParser>();
    }

    [Fact]
    public void Test()
    {
        string uaString =
            "Mozilla/5.0 (iPhone; CPU iPhone OS 5_1_1 like Mac OS X) AppleWebKit/534.46 (KHTML, like Gecko) Version/5.1 Mobile/9B206 Safari/7534.48.3";
        var c = _userAgentParser.Parse(uaString);
        Assert.Equal("Mobile Safari", c.UA.Family);
        Assert.Equal("5", c.UA.Major);
        Assert.Equal("1", c.UA.Minor);
        Assert.Equal("iOS", c.OS.Family);
        Assert.Equal("5", c.OS.Major);
        Assert.Equal("1", c.OS.Minor);
        Assert.Equal("iPhone", c.Device.Family);
    }
}