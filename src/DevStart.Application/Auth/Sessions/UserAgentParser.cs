namespace DevStart.Application.Auth.Sessions
{
    public enum DeviceKind
    {
        Unknown = 0,
        Desktop = 1,
        Mobile = 2,
        Tablet = 3,
    }

    public sealed record UserAgentInfo(string Browser, string Os, DeviceKind Kind)
    {
        public const string UnknownLabel = "Неизвестно";

        public static readonly UserAgentInfo Unknown = new(UnknownLabel, UnknownLabel, DeviceKind.Unknown);

        /// <summary>The name shown in the devices list, e.g. "Chrome на Windows".</summary>
        public string Label => $"{Browser} на {Os}";
    }

    /// <summary>
    /// Just enough User-Agent parsing to label a session in the UI. A pure function rather than a
    /// package: the alternatives ship large regex corpora to answer questions this screen never asks.
    /// Order of the checks is load-bearing — Edge and Opera both advertise "Chrome", and Chrome
    /// advertises "Safari".
    /// </summary>
    public static class UserAgentParser
    {
        /// <summary>Matches the width of the user_agent column; anything longer is noise for this purpose.</summary>
        private const int MaxLength = 512;

        public static UserAgentInfo Parse(string? userAgent)
        {
            if (string.IsNullOrWhiteSpace(userAgent))
            {
                return UserAgentInfo.Unknown;
            }

            string ua = userAgent.Length > MaxLength ? userAgent[..MaxLength] : userAgent;

            return new UserAgentInfo(ParseBrowser(ua), ParseOs(ua), ParseKind(ua));
        }

        private static string ParseBrowser(string ua)
        {
            if (Has(ua, "Edg/") || Has(ua, "Edge/") || Has(ua, "EdgiOS/") || Has(ua, "EdgA/"))
            {
                return "Edge";
            }
            if (Has(ua, "OPR/") || Has(ua, "Opera"))
            {
                return "Opera";
            }
            if (Has(ua, "YaBrowser"))
            {
                return "Yandex Browser";
            }
            if (Has(ua, "Firefox/") || Has(ua, "FxiOS/"))
            {
                return "Firefox";
            }
            if (Has(ua, "CriOS/") || Has(ua, "Chrome/") || Has(ua, "Chromium/"))
            {
                return "Chrome";
            }
            // Only Safari itself pairs "Version/" with "Safari/"; Chrome and friends carry Safari alone.
            if (Has(ua, "Safari/") && Has(ua, "Version/"))
            {
                return "Safari";
            }

            return UserAgentInfo.UnknownLabel;
        }

        private static string ParseOs(string ua)
        {
            if (Has(ua, "Windows NT") || Has(ua, "Windows Phone"))
            {
                return "Windows";
            }
            if (Has(ua, "Android"))
            {
                return "Android";
            }
            if (Has(ua, "iPhone") || Has(ua, "iPad") || Has(ua, "iPod") || Has(ua, "iOS"))
            {
                return "iOS";
            }
            if (Has(ua, "Mac OS X") || Has(ua, "Macintosh"))
            {
                return "macOS";
            }
            if (Has(ua, "CrOS"))
            {
                return "ChromeOS";
            }
            if (Has(ua, "Linux") || Has(ua, "X11"))
            {
                return "Linux";
            }

            return UserAgentInfo.UnknownLabel;
        }

        private static DeviceKind ParseKind(string ua)
        {
            // Tablets first: an Android tablet UA contains "Android" but not "Mobi".
            if (Has(ua, "iPad") || Has(ua, "Tablet") || (Has(ua, "Android") && !Has(ua, "Mobi")))
            {
                return DeviceKind.Tablet;
            }
            if (Has(ua, "Mobi") || Has(ua, "iPhone") || Has(ua, "iPod") || Has(ua, "Android") || Has(ua, "Windows Phone"))
            {
                return DeviceKind.Mobile;
            }
            if (Has(ua, "Windows NT") || Has(ua, "Macintosh") || Has(ua, "X11") || Has(ua, "CrOS") || Has(ua, "Linux"))
            {
                return DeviceKind.Desktop;
            }

            return DeviceKind.Unknown;
        }

        private static bool Has(string ua, string token)
            => ua.Contains(token, StringComparison.OrdinalIgnoreCase);
    }
}
