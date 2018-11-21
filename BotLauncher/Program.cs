using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace BotLauncher
{
    class Program
    {
        static void Main(string[] args)
        {
            //Directory.SetCurrentDirectory(@"D:\T\OpBot\OpBot\bin\Debug");
            IPEndPoint groupEP = new IPEndPoint(IPAddress.Any, 10666);
            UdpClient listener = new UdpClient();
            listener.Client.Bind(groupEP);
            Console.WriteLine($"{CurrentTime} Listening...");
            while (true)
            {
                byte[] bytes = listener.Receive(ref groupEP);
                Console.WriteLine($"{CurrentTime} Starting OpBot");
                Process.Start(@"OpBot.exe");
            }
        }

        static string CurrentTime
        {
            get
            {
                DateTime now = DateTime.Now;
                return $"{now.ToLongDateString()} {now.ToLongTimeString()}";
            }
        }
    }
}
