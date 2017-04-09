using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpBot
{
    internal static class DiscordText
    {
        private static string[] _numbers = { ":zero:", ":one:", ":two:", ":three:", ":four:", ":five:", ":six:", ":seven:", ":eight:", ":nine:" };

        public const string Kiss = ":kiss:";
        public const string OkHand = ":ok_hand:";
        public const string Warning = ":warning:";
        public const string Stopwatch = ":stopwatch:";
        public const string NoEntry = ":no_entry_sign:";
        public const string CodeBlock = "```";

        public static string BigText(string text)
        {
            const int letterPosition = 20; // index of x in regionalIndicator
            StringBuilder regionalIndicator = new StringBuilder(":regional_indicator_x:");
            StringBuilder bigtext = new StringBuilder(regionalIndicator.Length * text.Length);
            foreach (char ch in text)
            {
                int numberIndex = "0123456789".IndexOf(ch);
                if ( numberIndex == -1)
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
                    bigtext.Append(_numbers[numberIndex]);
                }
            }
            return bigtext.ToString();
        }
    }
}
