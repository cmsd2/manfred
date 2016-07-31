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
    }
}