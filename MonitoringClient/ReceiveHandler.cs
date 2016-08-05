using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Ircc;
using static Ircc.IrccHelper;
using StackExchange.Redis;

namespace MonitoringClient
{
    class ReceiveHandler
    {
        ServerHandle server;
        Packet recvPacket;
        RedisHelper redis;
        Header NoResponseHeader = new Header(-1, 0, 0);
        public static List<ServerHandle> servers;
        static int currentRoomCount = 0;
        static int currentUserCount = 0;
        static short mListCount = 0;

        public ReceiveHandler()
        {
            servers = new List<ServerHandle>();
        }

        public ReceiveHandler(Packet recvPacket, RedisHelper redis)
        {
            this.recvPacket = recvPacket;
            this.redis = redis;
        }

        public ReceiveHandler(ServerHandle server, Packet recvPacket)
        {
            this.server = server;
            this.recvPacket = recvPacket;
            servers.Add(server);
        }

        public void AddServer(ServerHandle server)
        {
            if (!servers.Contains(server))
                servers.Add(server);
        }

        public List<ServerHandle> GetServerList()
        {
            return servers;
        }

        //public Packet PacketHandler()
        public Packet GetResponse()
        {
            Packet returnPacket;
            Header returnHeader = new Header();
            byte[] returnData = null;

            bool debug = false;

            if (debug)
                Console.WriteLine("==RECEIVED: \n" + PacketDebug(recvPacket));

            //Client to Server side
            if (Comm.CS == recvPacket.header.comm)
            {
                switch (recvPacket.header.code)
                {
                    //------------No action from client----------
                    case -1:
                        returnHeader = new Header(Comm.CS, Code.HEARTBEAT, 0);
                        returnData = null;
                        break;

                    //------------CREATE------------
                    case Code.CREATE:
                        //CL -> FE side
                        break;
                    case Code.CREATE_DUPLICATE_ERR:
                        //FE -> CL side
                        break;
                    case Code.CREATE_FULL_ERR:
                        //FE -> CL side
                        break;


                    //------------DESTROY------------
                    case Code.DESTROY:
                        //CL -> FE side

                        break;
                    case Code.DESTROY_ERR:
                        //FE -> CL side
                        break;


                    //------------FAIL------------
                    case Code.FAIL:
                        returnHeader = NoResponseHeader;
                        returnData = null;
                        break;


                    //------------HEARTBEAT------------
                    case Code.HEARTBEAT:
                        //FE -> CL side
                        break;
                    case Code.HEARTBEAT_RES:
                        //CL -> FE side
                        returnHeader = NoResponseHeader;
                        returnData = null;
                        break;


                    //------------JOIN------------
                    case Code.JOIN:
                        //CL -> FE side
                        break;
                    case Code.JOIN_FULL_ERR:
                        //FE -> CL side
                        break;
                    case Code.JOIN_NULL_ERR:
                        //FE -> CL side
                        break;


                    //------------LEAVE------------
                    case Code.LEAVE:
                        //CL -> FE side
                        break;
                    case Code.LEAVE_ERR:
                        //FE -> CL side
                        break;


                    //------------LIST------------
                    case Code.LIST:
                        //CL -> FE side
                        break;
                    case Code.LIST_ERR:
                        //FE -> CL side
                        break;
                    case Code.LIST_RES:
                        //FE -> CL side
                        break;

                    //------------MONITORING------------
                    case Code.MLIST:
                        //MCL -> FE side
                        break;
                    case Code.MLIST_ERR:
                        //FE -> MCL side
                        Console.WriteLine("MLIST Error");
                        returnHeader = NoResponseHeader;
                        returnData = null;
                        break;
                    case Code.MLIST_RES:
                        //FE -> MCL side

                        byte[] roomBytes = new byte[4];
                        byte[] userBytes = new byte[4];
                        Array.Copy(recvPacket.data, 0, roomBytes, 0, sizeof(int));
                        Array.Copy(recvPacket.data, sizeof(int), userBytes, 0, sizeof(int));

                        currentRoomCount += BitConverter.ToInt32(roomBytes, 0);
                        currentUserCount += BitConverter.ToInt32(userBytes, 0);
                        mListCount++;

                        if (mListCount == recvPacket.header.sequence)
                        {
                            Console.WriteLine("Current Room Count: {0}", currentRoomCount);
                            Console.WriteLine("Current User Count: {0}", currentUserCount);
                            currentRoomCount = 0;
                            currentUserCount = 0;
                            mListCount = 0;
                        }

                        returnHeader = NoResponseHeader;
                        returnData = null;
                        break;

                    //------------MSG------------
                    case Code.MSG:
                        //CL <--> FE side

                        break;
                    case Code.MSG_ERR:
                        //CL <--> FE side
                        break;


                    //------------SIGNIN------------
                    case Code.SIGNIN:
                        //CL -> FE -> BE side

                        break;
                    case Code.SIGNIN_ERR:
                        //BE -> FE -> CL side
                        break;
                    case Code.SIGNIN_RES:
                        //BE -> FE -> CL side
                        break;
                    case Code.SIGNIN_DUM:
                        //CL -> FE
                        break;


                    //------------SIGNUP------------
                    case Code.SIGNUP:
                        //CL -> FE -> BE side
                        break;
                    case Code.SIGNUP_ERR:
                        //BE -> FE -> CL side
                        //error handling
                        break;
                    case Code.SIGNUP_RES:
                        //BE -> FE -> CL side
                        //success
                        break;


                    //------------SUCCESS------------
                    case Code.SUCCESS:
                        //
                        break;

                    default:
                        if (debug)
                            Console.WriteLine("Unknown code: {0}\n", recvPacket.header.code);
                        break;
                }
            }
            //Server to Server Side
            else if (Comm.SS == recvPacket.header.comm)
            {
                switch (recvPacket.header.code)
                {
                    //------------No action from client----------
                    case -1:
                        returnHeader = new Header(Comm.SS, Code.HEARTBEAT, 0);
                        returnData = null;
                        break;

                    //------------HEARTBEAT------------
                    case Code.HEARTBEAT:
                        //FE -> CL side
                        returnHeader = new Header(Comm.SS, Code.HEARTBEAT_RES, 0);
                        returnData = null;
                        break;
                    case Code.HEARTBEAT_RES:
                        //CL -> FE side
                        returnHeader = NoResponseHeader;
                        returnData = null;
                        break;

                    //------------SDESTROY------------
                    case Code.SDESTROY:
                        //FE side
                        break;
                    case Code.SDESTROY_ERR:
                        //FE side
                        break;


                    //------------SJOIN------------
                    case Code.SJOIN:
                        //FE side
                        break;
                    case Code.SJOIN_RES:
                        //FE side
                        break;
                    case Code.SJOIN_ERR:
                        //FE side
                        break;

                    //------------SLEAVE-----------
                    case Code.SLEAVE:
                        //FE side
                        break;
                    case Code.SLEAVE_ERR:
                        //FE side
                        break;
                    case Code.SLEAVE_RES:
                        //FE side
                        break;

                    //------------SLIST------------
                    case Code.SLIST:
                        //FE side
                        break;
                    case Code.SLIST_ERR:
                        //FE side
                        break;

                    case Code.SLIST_RES:
                        //FE side
                        break;


                    //------------SMSG------------                
                    case Code.SMSG:
                        //FE side
                        break;
                    case Code.SMSG_ERR:
                        //FE side
                        break;
                }
            }
            //Dummy to Server Side
            else if (Comm.DUMMY == recvPacket.header.comm)
            {

            }

            returnPacket = new Packet(returnHeader, returnData);
            if (debug && returnPacket.header.comm != -1)
                Console.WriteLine("==SEND: \n" + PacketDebug(returnPacket));

            return returnPacket;
        }

        private long ToInt64(byte[] bytes, int startIndex)
        {
            long result = 0;
            try
            {
                result = BitConverter.ToInt64(bytes, startIndex);
            }
            catch (Exception)
            {
                Console.WriteLine("bytes to int64: fuck you. you messsed up");
            }

            return result;
        }
    }
}
