using System;
using System.Reflection;

namespace OpBot
{
    internal static class OpBotUtils
    {
        public static string GetVersionText()
        {
            Version version = Assembly.GetExecutingAssembly().GetName().Version;
            string text = $"OpBot Version: {version.Major}.{version.Minor}.{version.Build}";
            return text;
        }
    }
}
