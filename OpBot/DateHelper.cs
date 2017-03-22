using System;
using System.Linq;

namespace OpBot
{
    internal static class DateHelper
    {
        private static string[] _dayNames =
        {
                "MON",
                "MONDAY",
                "TUE",
                "TUES",
                "TUESDAY",
                "WED",
                "WEDNESDAY",
                "THU",
                "THUR",
                "THURSDAY",
                "FRI",
                "FRIDAY",
                "SAT",
                "SATURDAY",
                "SUN",
                "SUNDAY"
        };

        public static DateTime GetDateForNextOccuranceOfDay(string day)
        {
            DateTime dt = DateTime.Now.Date;
            DayOfWeek requiredDay = DayToDayOfWeek(day);
            while (dt.DayOfWeek != requiredDay)
                dt = dt.AddDays(1);
            return dt;
        }


        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
        public static DayOfWeek DayToDayOfWeek(string day)
        {
            switch (day.ToUpperInvariant())
            {
                case "MON":
                case "MONDAY":
                    return DayOfWeek.Monday;
                case "TUE":
                case "TUES":
                case "TUESDAY":
                    return DayOfWeek.Tuesday;
                case "WED":
                case "WEDNESDAY":
                    return DayOfWeek.Wednesday;
                case "THU":
                case "THUR":
                case "THURSDAY":
                    return DayOfWeek.Thursday;
                case "FRI":
                case "FRIDAY":
                    return DayOfWeek.Friday;
                case "SAT":
                case "SATURDAY":
                    return DayOfWeek.Saturday;
                case "SUN":
                case "SUNDAY":
                    return DayOfWeek.Sunday;
                default:
                    throw new OpBotInvalidValueException($"{day} is not a recognizable day name");
            }
        }

        public static bool IsDayName(string dayName)
        {
            return _dayNames.Contains(dayName);
        }

    }
}
