using System;
using System.Collections.Generic;

namespace OpBot
{
    internal static class GroupFinder
    {
        private static readonly string[] order = { "DP", "SV", "EV", "DF", "RAV", "KP", "EC", "TOS", "TFB" };
        private static readonly DateTime baseDate = new DateTime(2017, 3, 6);

        public static string OperationOn(DateTime dt)
        {
            int opIndex = GetOrderIndex(dt);
            return order[opIndex];
        }

        public static List<string> NextDays(int numDays)
        {
            List<string> week = new List<string>();
            int opIndex = GetOrderIndex(DateTime.Now.Date);
            for (int k = 0; k < numDays; k++)
            {
                week.Add(order[opIndex]);
                if (++opIndex >= order.Length)
                    opIndex = 0;
            }
            return week;
        }

        private static int GetOrderIndex(DateTime dt)
        {
            TimeSpan toBase = dt.Date - baseDate;
            int opIndex = toBase.Days % order.Length;
            return opIndex;
        }
    }
}
