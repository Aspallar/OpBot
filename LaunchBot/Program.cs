using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace LaunchBot
{
    class Program
    {
        static void Main(string[] args)
        {
            byte[] data = { 101 };
            UdpClient udpClient = new UdpClient("192.168.0.11", 10666);
            udpClient.Send(data, data.Length);
        }
    }
}
