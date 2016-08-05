using System;
using System.Net;
using System.Net.Sockets;

namespace MonitoringClient
{
    class MonitorClient
    {
        public Socket monitorSocket;

        string host;
        int port;
        IPEndPoint ipLocalEP;

        public MonitorClient(string hname, string pname)
        {
            host = hname;
            if (!Int32.TryParse(pname, out port))
            {
                Console.Error.WriteLine("Error: Port arg must be int. given: \"{0}\"", pname);
                Environment.Exit(0);
            }

            monitorSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            IPAddress ipAddress;
            if (host == null)
                ipAddress = IPAddress.Loopback;
            else
                ipAddress = IPAddress.Parse(host);

            ipLocalEP = new IPEndPoint(ipAddress, port);
        }



        public void ConnectServer()
        {
            Console.WriteLine("Connecting to {0}:{1} ...", (host == null ? "LOOPBACK" : host), port);
            monitorSocket.Connect(ipLocalEP);
            if (monitorSocket.Connected)
                Console.WriteLine("Connected to {0}:{1} ...", (host == null ? "LOOPBACK" : host), port);
        }

        public void CloseServer()
        {
            Console.WriteLine("Closing Connection {0}:{1} ...", (host == null ? "LOOPBACK" : host), port);
            monitorSocket.Close();
            if (!monitorSocket.Connected)
                Console.WriteLine("Closed Connection {0}:{1} ...", (host == null ? "LOOPBACK" : host), port);
        }

        


    }
}
