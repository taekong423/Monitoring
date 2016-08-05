using System;

namespace MonitoringClient
{
    class Program
    {
        static void Main(string[] args)
        {
            string host = "10.100.58.5";
            //string host = null;
            string port = "30000";

            MonitorHandle monitor = new MonitorHandle();
            monitor.StartMonitoring(host, port);
        }
    }
}
