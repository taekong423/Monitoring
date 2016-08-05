using System;
using System.Collections.Generic;
using Ircc;
using static Ircc.IrccHelper;

namespace MonitoringClient
{
    class ReceiveHandler
    {
        ServerHandle server;
        Packet recvPacket;
        Header NoResponseHeader = new Header(-1, 0, 0);
        Packet NoResponsePacket = new Packet(new Header(-1, 0, 0), null);

        public static List<ServerHandle> servers;
        static int currentRoomCount = 0;
        static int currentUserCount = 0;
        static short mListCount = 0;
        static List<long> id = new List<long>();

        public ReceiveHandler()
        {
            servers = new List<ServerHandle>();
        }

        public ReceiveHandler(ServerHandle server, Packet recvPacket)
        {
            this.server = server;
            this.recvPacket = recvPacket;
            servers.Add(server);
        }

        //public Packet PacketHandler()
        public Packet GetResponse()
        {
            Packet returnPacket;
            Header returnHeader = new Header();
            byte[] returnData = null;

            bool debug = false;

            if (debug)
                Console.WriteLine("==RECEIVED:\n" + PacketDebug(recvPacket));

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
                        returnHeader = new Header(Comm.CS, Code.HEARTBEAT_RES, 0);
                        returnData = null;
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
                        // size of list of rooms received
                        const int userSize = sizeof(int);
                        const int roomSize = sizeof(long);

                        int recvSize = recvPacket.header.size - userSize;

                        byte[] userBytes = new byte[userSize];
                        byte[] roomBytes = new byte[recvSize];

                        Array.Copy(recvPacket.data, 0, userBytes, 0, userSize);
                        Array.Copy(recvPacket.data, userSize, roomBytes, 0, recvSize);

                        currentUserCount += BitConverter.ToInt32(userBytes, 0);

                        for (int i = 0; i < roomSize; i += roomSize)
                        {
                            byte[] tempByte = new byte[roomSize];
                            Array.Copy(roomBytes, i, tempByte, 0, roomSize);
                            long tempId = BitConverter.ToInt64(tempByte, 0);
                            if (0 != tempId)
                            {
                                if (!id.Contains(tempId))
                                    id.Add(tempId);
                            }
                        }
                        
                        mListCount++;

                        if (mListCount == recvPacket.header.sequence)
                        {
                            Console.WriteLine("\nCurrent User Count: {0}", currentUserCount);
                            currentRoomCount = id.Count;
                            Console.WriteLine("Current Room Count: {0}", currentRoomCount);

                            currentRoomCount = 0;
                            currentUserCount = 0;
                            mListCount = 0;
                            id.Clear();
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
                    case Code.SIGNIN_DUMMY:
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
                    
                    //-----------Wrong Code Value-----------
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
    }
}
