using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using StackExchange.Redis;
using Ircc;
using static Ircc.IrccHelper;
using System.Net.Sockets;

namespace MonitoringClient
{
    class Program
    {
        private static RedisKey USERS = "Users";
        private static RedisKey RANKINGS = "Rankings";
        private static RedisKey ROOMS = "Rooms";
        private static string userPrefix = "user:";

        private static CommandState state = CommandState.Idle;

        static List<ServerHandle> servers = new List<ServerHandle>();

        public enum CommandState
        {
            Idle, Main, Redis, Connect, Server, Finish
        }

        static void Main(string[] args)
        {
            string host = "10.100.58.5";
            //string host = null;
            string port = "30000";
            string command = null;
            
            TcpMonitor server;

            //string configString = "10.100.58.7:26379,keepAlive=180";
            Console.WriteLine("Connecting to Redis...\n");
            string configString = System.IO.File.ReadAllText("redis.conf");
            ConfigurationOptions configOptions = ConfigurationOptions.Parse(configString);
            RedisHelper redis = new RedisHelper(configOptions);

            Console.WriteLine("Initializing lobby and rooms...");
            ReceiveHandler recvHandler = new ReceiveHandler();

            Thread mThread = new Thread(() => RealTimeMonitor(redis));
            
            while (true)
            {
                switch (state)
                {
                    // Initialize Redis
                    case CommandState.Idle:
                        state = CommandState.Main;
                        break;

                    // Main Screen
                    case CommandState.Main:
                        DivideSection();
                        Console.WriteLine("-------Monitoring Command-------\n" +
                                          "|      0 : Exit                |\n" +
                                          "|      1 : Only Redis          |\n" +
                                          "|      2 : Connect Server      |\n" +
                                          "--------------------------------" );
                        command = GetCommand();

                        if ("0" == command) state = CommandState.Finish;
                        else if ("1" == command)  state = CommandState.Redis;
                        else if ("2" == command)  state = CommandState.Connect;
                        else InvalidCommand();
                        
                        break;

                    case CommandState.Redis:
                        DivideSection();
                        Console.WriteLine("-------Monitoring Command-------\n" +
                                          "|     0 : Exit                 |\n" +
                                          "|     1 : Back to Main         |\n" +
                                          "|     2 : Show User LIst       |\n" +
                                          "|     3 : Show User Info       |\n" +
                                          "|     4 : Show Room List       |\n" +
                                          "|     5 : Show Chat Ranking    |\n" +
                                          "--------------------------------");
                        command = GetCommand();

                        switch (command)
                        {
                            case "0":
                            case "1":
                            case "2":
                            case "3":
                            case "4":
                            case "5":
                                HandleCommand(command, redis);
                                break;

                            default:
                                InvalidCommand();
                                break;
                        }
                        
                        break;

                    case CommandState.Connect:
                        DivideSection();
                        Console.WriteLine("-------Monitoring Command-------\n" +
                                          "|      0 : Exit                |\n" +
                                          "|      1 : Back to Main        |\n" +
                                          "|      2 : Connect Server      |\n" +
                                          "|      3 : Finish Connect      |\n" +
                                          "--------------------------------");
                        command = GetCommand();

                        switch (command)
                        {
                            case "0":
                                state = CommandState.Finish;
                                break;

                            case "1":
                                state = CommandState.Main;
                                break;

                            case "2":
                                Console.Write("\nIP Address: " + ((host == null) ? "LoopBack" : host)  + "\nPORT: ");
                                port = Console.ReadLine();
                                server = new TcpMonitor(host, port);

                                try
                                {
                                    ConnectServer(server);
                                }
                                catch (SocketException se)
                                {
                                    Console.WriteLine("\nFAIL to connect...");
                                    continue;
                                }

                                break;

                            case "3":
                                state = CommandState.Server;
                                break;

                            default:
                                InvalidCommand();
                                break;
                        }

                        break;

                    case CommandState.Server:
                        DivideSection();
                        mThread.Start();
                        
                        command = GetCommand();

                        switch (command)
                        {
                            case "0":
                                state = CommandState.Finish;
                                break;

                            case "1":
                                state = CommandState.Main;
                                break;

                            default:
                                InvalidCommand();
                                DivideSection();
                                break;
                        }
                        mThread.Abort();
                        break;

                    case CommandState.Finish:
                        Environment.Exit(0);
                        break;

                    default:
                        Console.WriteLine("Wrong State!!");
                        break;
                }
            }
        }

        static void RealTimeMonitor(RedisHelper redis)
        {
            while(true)
            {
                Console.Clear();

                Console.WriteLine("-------Monitoring Command-------\n" +
                                  "|   0 : Exit                   |\n" +
                                  "|   1 : Back to Main           |\n" +
                                  "--------------------------------");

                RankingList(5, redis);

                Header reqHeader = new Header(Comm.CS, Code.MLIST, 0, (short)servers.Count);
                Packet reqPacket = new Packet(reqHeader, null);

                foreach (ServerHandle sh in servers)
                {
                    sh.Send(reqPacket);
                }

                
                Thread.Sleep(5000);
            }
        }

        static void HandleCommand(string command, RedisHelper redis)
        {
            switch (command)
            {
                case "0":
                    state = CommandState.Finish;
                    break;

                case "1":
                    state = CommandState.Main;
                    break;

                case "2":
                    DivideSection();
                    Console.WriteLine("< Redis User List >");
                    ShowList(redis.GetDataList(USERS));
                    break;

                case "3":
                    Console.Write("Enter UserId: ");
                    string userId = Console.ReadLine();
                    RedisKey user = userPrefix + userId;
                    DivideSection();
                    Console.WriteLine("< Redis User Info >");
                    ShowList(redis.GetInfo(user));
                    break;

                case "4":
                    DivideSection();
                    Console.WriteLine("< Redis Room List >");
                    ShowList(redis.GetDataList(ROOMS));
                    break;

                case "5":
                    DivideSection();
                    Console.Write("Enter EndRank: ");
                    int endRank;
                    if (int.TryParse(Console.ReadLine(), out endRank))
                    {
                        RankingList(endRank, redis);
                    }
                    else InvalidCommand();
                    break;
            }
        }

        static void RankingList(int endRank, RedisHelper redis)
        {
            Console.WriteLine("< Redis Chat Ranking >");
            Dictionary<string, double> ranking = new Dictionary<string, double>();
            ranking = redis.GetAllTimeRankings(endRank);
            foreach (KeyValuePair<string, double> pair in ranking)
            {
                //if (!IsDummy(pair.Key, redis))
                    Console.WriteLine("User: " + pair.Key + " \tCount: " + pair.Value);
            }
        }

        static string GetCommand()
        {
            Console.Write("Command: ");
            return Console.ReadLine();
        }

        static void ShowString(string str)
        {
            Console.Clear();
            Console.WriteLine(str);
        }

        static void ShowList(List<string> list)
        {
            foreach (string str in list)
            {
                Console.WriteLine(str);
            }
        }

        static bool IsDummy(string id, RedisHelper redis)
        {
            if ("1" == redis.GetInfoValue(id, 2))
                return true;
            else return false;
        }

        static void InvalidCommand()
        {
            Console.WriteLine("Invalid Input!!");
        }

        static void DivideSection()
        {
            Console.WriteLine("\n\n-----------------------------------------------------------\n");
        }

        static void ConnectServer(TcpMonitor monitor)
        {
            monitor.ConnectServer();
            ServerHandle server = new ServerHandle(monitor.monitorSocket);
            servers.Add(server);
        }
    }
}
