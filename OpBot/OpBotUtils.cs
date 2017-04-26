using System;
using System.Diagnostics;
using System.Reflection;

namespace OpBot
{
    internal static class OpBotUtils
    {
        public static string GetVersionText()
        {
            Version version = Assembly.GetExecutingAssembly().GetName().Version;
            string text = $"{version.Major}.{version.Minor}.{version.Build}";
            return text;
        }
    }
}
