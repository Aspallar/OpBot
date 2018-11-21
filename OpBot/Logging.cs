using log4net.Appender;
using log4net.Config;
using log4net.Core;
using log4net.Layout;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpBot
{
    public static class Logging
    {
        public static void Configure(string logConfigFileName)
        {
            if (File.Exists(logConfigFileName))
            {
                using (Stream sr = File.OpenRead(logConfigFileName))
                    XmlConfigurator.Configure(sr);
            }
            else
            {
                ConfigureBasicLogger();
            }
        }

        public static void ConfigureBasicLogger()
        {
            var layout = new PatternLayout("%d %-5level %message%newline");
            var consoleAppender = new ConsoleAppender
            {
                Layout = layout,
                Threshold = Level.Info,
            };
            layout.ActivateOptions();
            consoleAppender.ActivateOptions();
            BasicConfigurator.Configure(consoleAppender);
            return;
        }
    }
}
