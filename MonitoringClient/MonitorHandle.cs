using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Threading;
using Ircc;
using static Ircc.IrccHelper;
using System.Net.Sockets;

namespace MonitoringClient
{
    class MonitorHandle
    {
        RedisKey USERS = "Users";
        RedisKey RANKINGS = "Rankings";
        RedisKey ROOMS = "Rooms";
        string userPrefix = "user:";

        int sleepTime = 1000;

        RedisHelper redis = null;
        Thread mThread = null;
        ReceiveHandler recvHandler = null;
        MonitorClient server = null;

        static List<ServerHandle> servers = new List<ServerHandle>();

        CommandState state = CommandState.Idle;

        public enum CommandState
        {
            Idle, Main, Redis, Connect, Server, Finish
        }

        public MonitorHandle()
        {
            // Init Redis
            redis = InitializeRedis();

            Console.WriteLine("Initializing MonitorClient");
            recvHandler = new ReceiveHandler();
            mThread = new Thread(RealTimeMonitor);
        }

        public RedisHelper InitializeRedis()
        {
            // Connect to Redis
            Console.WriteLine("Connecting to Redis...\n");
            string configString = System.IO.File.ReadAllText("redis.conf"); // redis.conf Format : 10.100.58.7:26379,keepAlive=180
            ConfigurationOptions configOptions = ConfigurationOptions.Parse(configString);
            return new RedisHelper(configOptions);
        }

        public void StartMonitoring(string host, string port)
        {
            string command = null;

            while (true)
            {
                switch (state)
                {
                    // Initialize Redis
                    case CommandState.Idle:
                        state = CommandState.Main;
                        break;

                    // Main Screen - Monitor with or without Server //
                    case CommandState.Main:
                        DivideSection();
                        Console.WriteLine("-------Monitoring Command-------\n" +
                                          "|      0 : Exit                |\n" +
                                          "|      1 : Only Redis          |\n" +
                                          "|      2 : Connect Server      |\n" +
                                          "--------------------------------");
                        command = GetCommand();

                        if ("0" == command) state = CommandState.Finish;
                        else if ("1" == command) state = CommandState.Redis;
                        else if ("2" == command) state = CommandState.Connect;
                        else Console.WriteLine("Invalid Command...");

                        break;

                    // Monitor without Server, Show data from only Redis //
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
                        RedisCommand(command);
                        break;

                    // Begin Server Connection Sequence //
                    case CommandState.Connect:
                        DivideSection();
                        Console.WriteLine("-------Monitoring Command-------\n" +
                                          "|      0 : Exit                |\n" +
                                          "|      1 : Back to Main        |\n" +
                                          "|      2 : Connect Server      |\n" +
                                          "|      3 : Start Monitoring    |\n" +
                                          "--------------------------------");
                        command = GetCommand();

                        switch (command)
                        {
                            case "0":
                                state = CommandState.Finish;
                                break;

                            case "1":
                                state = CommandState.Main;
                                //server.CloseServer();
                                //servers.Clear();
                                break;

                            case "2":
                                Console.Write("\nIP Address: " + ((host == null) ? "LoopBack" : host) + "\nPORT: ");
                                port = Console.ReadLine();
                                server = new MonitorClient(host, port);
                                try
                                {
                                    ConnectServer(server);
                                }
                                catch (SocketException se)
                                {
                                    Console.WriteLine("\nFAIL to connect...");
                                    server = null;
                                    continue;
                                }
                                catch (Exception e)
                                {
                                    Console.WriteLine(e.Message);
                                    server = null;
                                    continue;
                                }
                                break;

                            case "3":
                                if (0 != servers.Count)
                                    state = CommandState.Server;
                                else Console.WriteLine("\nNot Connected to Server...");
                                break;

                            default:
                                Console.WriteLine("Invalid Command...");
                                break;
                        }

                        break;
                    
                    // Monitor with Server Connection //
                    case CommandState.Server:
                        DivideSection();

                        // Monitoring Thread
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
                                Console.WriteLine("Invalid Command...");
                                DivideSection();
                                break;
                        }

                        // Finish Thread
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
        void RealTimeMonitor()
        {
            int monitorCount = 0;
            int endRank = 10;
            while (true)
            {
                Console.Clear();
                DivideSection();
                Console.WriteLine("Monitoring Count : {0}\n", monitorCount);

                Console.WriteLine("-------Monitoring Command-------\n" +
                                  "|   0 : Exit                   |\n" +
                                  "|   1 : Back to Main           |\n" +
                                  "--------------------------------");

                // Show Chat Ranking List
                RankingList(endRank);

                lock (servers)
                {
                    // Request Current Server Information (User, Room)
                    Header reqHeader = new Header(Comm.CS, Code.MLIST, 0, (short)servers.Count);
                    Packet reqPacket = new Packet(reqHeader, null);

                    foreach (ServerHandle sh in servers)
                        sh.Send(reqPacket);
                }

                monitorCount++;
                // Update every [sleepTime] seconds
                Thread.Sleep(sleepTime);
            }
        }

        void RedisCommand(string command)
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
                        RankingList(endRank);
                    }
                    else Console.WriteLine("Invalid Command...");
                    break;
                default:
                    Console.WriteLine("Invalid Command...");
                    break;
            }
        }

        void RankingList(int endRank)
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

        string GetCommand()
        {
            Console.Write("Command: ");
            return Console.ReadLine();
        }

        void ShowList(List<string> list)
        {
            foreach (string str in list)
            {
                Console.WriteLine(str);
            }
        }

        bool IsDummy(string id, RedisHelper redis)
        {
            if ("1" == redis.GetInfoValue(id, 2))
                return true;
            else return false;
        }

        void DivideSection()
        {
            Console.WriteLine("\n-----------------------------------------------------------\n");
        }

        void ConnectServer(MonitorClient monitor)
        {
            monitor.ConnectServer();
            ServerHandle server = new ServerHandle(monitor.monitorSocket);
            servers.Add(server);
        }
    }
        
}
