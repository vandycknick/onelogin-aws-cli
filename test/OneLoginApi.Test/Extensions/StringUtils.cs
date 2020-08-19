using System;
using System.Globalization;

namespace OneLoginApi.Test.Extensions
{
    public static class StringUtils
    {
        public static DateTime ToDateTime(this string dateString) =>
            DateTime.ParseExact(dateString, "yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'FFFFFFFZ", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
    }
}
