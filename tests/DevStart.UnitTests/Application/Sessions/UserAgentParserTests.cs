using DevStart.Application.Auth.Sessions;

namespace DevStart.UnitTests.Application.Sessions
{
    public class UserAgentParserTests
    {
        private const string Chrome =
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126.0.0.0 Safari/537.36";
        private const string Edge = Chrome + " Edg/126.0.0.0";
        private const string Opera = Chrome + " OPR/110.0.0.0";
        private const string Yandex =
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 YaBrowser/24.4.0.0 Safari/537.36";
        private const string Safari =
            "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.5 Safari/605.1.15";
        private const string Firefox =
            "Mozilla/5.0 (X11; Linux x86_64; rv:127.0) Gecko/20100101 Firefox/127.0";
        private const string IPhone =
            "Mozilla/5.0 (iPhone; CPU iPhone OS 17_5 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.5 Mobile/15E148 Safari/604.1";
        private const string IPad =
            "Mozilla/5.0 (iPad; CPU OS 17_5 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.5 Safari/604.1";
        private const string AndroidPhone =
            "Mozilla/5.0 (Linux; Android 14; Pixel 8) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126.0.0.0 Mobile Safari/537.36";

        // Edge, Opera and Yandex all advertise "Chrome", and Chrome advertises "Safari" — the order of
        // the checks is what keeps these apart.
        [Theory]
        [InlineData(Chrome, "Chrome")]
        [InlineData(Edge, "Edge")]
        [InlineData(Opera, "Opera")]
        [InlineData(Yandex, "Yandex Browser")]
        [InlineData(Safari, "Safari")]
        [InlineData(Firefox, "Firefox")]
        public void Parse_IdentifiesBrowser(string userAgent, string expected)
        {
            Assert.Equal(expected, UserAgentParser.Parse(userAgent).Browser);
        }

        [Theory]
        [InlineData(Chrome, "Windows")]
        [InlineData(Safari, "macOS")]
        [InlineData(Firefox, "Linux")]
        [InlineData(IPhone, "iOS")]
        [InlineData(AndroidPhone, "Android")]
        public void Parse_IdentifiesOs(string userAgent, string expected)
        {
            Assert.Equal(expected, UserAgentParser.Parse(userAgent).Os);
        }

        [Theory]
        [InlineData(Chrome, DeviceKind.Desktop)]
        [InlineData(Safari, DeviceKind.Desktop)]
        [InlineData(IPhone, DeviceKind.Mobile)]
        [InlineData(AndroidPhone, DeviceKind.Mobile)]
        [InlineData(IPad, DeviceKind.Tablet)]
        public void Parse_IdentifiesDeviceKind(string userAgent, DeviceKind expected)
        {
            Assert.Equal(expected, UserAgentParser.Parse(userAgent).Kind);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Parse_ReturnsUnknown_ForMissingUserAgent(string? userAgent)
        {
            Assert.Equal(UserAgentInfo.Unknown, UserAgentParser.Parse(userAgent));
        }

        [Fact]
        public void Parse_HandlesOverlongInput_WithoutThrowing()
        {
            string overlong = Chrome + new string('x', 5000);

            Assert.Equal("Chrome", UserAgentParser.Parse(overlong).Browser);
        }

        [Fact]
        public void Label_CombinesBrowserAndOs()
        {
            Assert.Equal("Chrome на Windows", UserAgentParser.Parse(Chrome).Label);
        }
    }
}
