using System;
using System.Net;

namespace MonitoringClient
{
    class Program
    {
        static void Main(string[] args)
        {
            string host = GetAddress();
            //string host = null;
            string port = "30000";
            try
            {
                MonitorHandle monitor = new MonitorHandle();
                monitor.StartMonitoring(host, port);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            finally
            {
                
            }
            
        }

        static string GetAddress()
        {
            string[] str = System.IO.File.ReadAllLines("server.conf");
            IPAddress address;
            if (IPAddress.TryParse(str[0].Trim(), out address))
                return str[0].Trim();
            else
            {
                Console.WriteLine("Invalid IPAddress Format --- e.g. 10.100.58.7");
                Environment.Exit(0);
                return null;
            }

        }
    }
}
