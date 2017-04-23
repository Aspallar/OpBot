using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpBot
{
    internal static class DiscordText
    {
        private static string[] _digits = { ":zero:", ":one:", ":two:", ":three:", ":four:", ":five:", ":six:", ":seven:", ":eight:", ":nine:" };

        public const string Kiss = ":kiss:";
        public const string OkHand = ":ok_hand:";
        public const string Warning = ":warning:";
        public const string Stopwatch = ":stopwatch:";
        public const string NoEntry = ":no_entry_sign:";
        public const string CodeBlock = "```";
        public const string StuckOutTongue = ":stuck_out_tongue:";

        public static string BigText(int value)
        {
            return BigText(value.ToString());
        }

        public static string BigText(string text)
        {
            const int letterPosition = 20; // position of x in regionalIndicator
            StringBuilder regionalIndicator = new StringBuilder(":regional_indicator_x:");
            StringBuilder bigtext = new StringBuilder(regionalIndicator.Length * text.Length);
            foreach (char ch in text)
            {
                int digitIndex = "0123456789".IndexOf(ch);
                if (digitIndex == -1)
                {
                    char lowerCh = char.ToLowerInvariant(ch);
                    if ("abcdefghijklmnopqrstuvwxyz".IndexOf(lowerCh) >= 0)
                    {
                        regionalIndicator[letterPosition] = lowerCh;
                        bigtext.Append(regionalIndicator);
                    }
                    else
                    {
                        bigtext.Append(ch);
                    }
                }
                else
                {
                    bigtext.Append(_digits[digitIndex]);
                }
            }
            return bigtext.ToString();
        }
    }
}
