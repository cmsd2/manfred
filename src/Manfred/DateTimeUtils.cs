using System;
using System.Globalization;

namespace Manfred
{
    public class DateTimeUTils
    {
        public static string ToIsoString(DateTime dt)
        {
            return dt.ToUniversalTime().ToString("o");
        }

        public static DateTime FromIsoString(string s)
        {
            return DateTime.Parse(s, null, DateTimeStyles.RoundtripKind);
        }

        public static DateTime ConvertFromUnixTimestamp(double timestamp)
        {
            DateTime origin = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            return origin.AddSeconds(timestamp);
        }

        public static double ConvertToUnixTimestamp(DateTime date)
        {
            DateTime origin = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            TimeSpan diff = date.ToUniversalTime() - origin;
            return Math.Floor(diff.TotalSeconds);
        }
    }
}