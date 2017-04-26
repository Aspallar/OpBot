using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpBot
{
    internal static class TimeZones
    {
        private static readonly string[] timeZoneIds =
        {
            "Hawaiian Standard Time",
            "Alaskan Standard Time",
            "Pacific Standard Time",
            "Central America Standard Time",
            "Central Standard Time",
            "Easter Island Standard Time",
            "Canada Central Standard Time",
            "SA Pacific Standard Time",
            "Eastern Standard Time",
            "Haiti Standard Time",
            "US Eastern Standard Time",
            "Atlantic Standard Time",
            "Central Brazilian Standard Time",
            "SA Western Standard Time",
            "Pacific SA Standard Time",
            "E. South America Standard Time",
            "SA Eastern Standard Time",
            "Argentina Standard Time",
            "Greenland Standard Time",
            "Mid-Atlantic Standard Time",
            "Morocco Standard Time",
            "W. Central Africa Standard Time",
            "Namibia Standard Time",
            "Middle East Standard Time",
            "Egypt Standard Time",
            "South Africa Standard Time",
            "Turkey Standard Time",
            "Israel Standard Time",
            "Kaliningrad Standard Time",
            "Belarus Standard Time",
            "Russian Standard Time",
            "E. Africa Standard Time",
            "Russia Time Zone 3",
            "Georgian Standard Time",
            "West Asia Standard Time",
            "Pakistan Standard Time",
            "India Standard Time",
            "Central Asia Standard Time",
            "Bangladesh Standard Time",
            "N. Central Asia Standard Time",
            "SE Asia Standard Time",
            "North Asia Standard Time",
            "China Standard Time",
            "North Asia East Standard Time",
            "Singapore Standard Time",
            "W. Australia Standard Time",
            "Tokyo Standard Time",
            "Korea Standard Time",
            "Cen. Australia Standard Time",
            "AUS Central Standard Time",
            "E. Australia Standard Time",
            "AUS Eastern Standard Time",
            "West Pacific Standard Time",
            "Tasmania Standard Time",
            "Vladivostok Standard Time",
            "Russia Time Zone 10",
            "Central Pacific Standard Time",
            "Russia Time Zone 11",
            "New Zealand Standard Time",
            "Fiji Standard Time"
        };


        public static List<TimeZoneTime> GetZoneTimes(DateTime time)
        {
            var timeZoneTimes = new List<TimeZoneTime>();
            DateTime utcTime = new DateTime(time.Ticks, DateTimeKind.Utc);
            foreach (string id in timeZoneIds)
            {
                TimeZoneInfo zone = TimeZoneInfo.FindSystemTimeZoneById(id);
                DateTime zoneTime = TimeZoneInfo.ConvertTimeFromUtc(utcTime, zone);
                string name = zone.IsDaylightSavingTime(zoneTime) ? zone.DaylightName : zone.StandardName;
                timeZoneTimes.Add(new TimeZoneTime
                {
                    Time = zoneTime.TimeOfDay,
                    Name = name,
                });
            }
            return timeZoneTimes.OrderBy(x => x.Name).ToList();
        }

        public static string ToString(List<TimeZoneTime> timeZones)
        {
            StringBuilder sb = new StringBuilder(2048);
            foreach (TimeZoneTime time in timeZones)
            {
                sb.Append(time.Time.ToString(@"hh\:mm"));
                sb.Append(' ');
                sb.AppendLine(time.Name);
            }
            return sb.ToString();
        }
    }
}
