﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StackExchange.Redis;

namespace Ircc
{
    public class RedisHelper
    {
        private static ConfigurationOptions configurationOptions;
        private RedisKey USERS = "Users";
        private RedisKey RANKINGS = "Rankings";
        private RedisKey CURRENTUSERS = "CurrentUsers";
        private RedisKey nextUserId = "nextUserId";
        private RedisKey ROOMS = "Rooms";
        private RedisKey nextRoomId = "nextRoomId";
        private string userPrefix = "user:";

        HashEntry[] entries;
        List<string> infoList;

        public RedisHelper(ConfigurationOptions configOptions)
        {
            if (null == configOptions)
                throw new ArgumentNullException("configOptions");
            configurationOptions = configOptions;
        }

        public bool IsConnected()
        {
            bool connected = false;
            try
            {
                connected = Database.IsConnected(USERS);
            }
            catch (Exception) { return false; }

            return connected;
        }

        public List<string> GetDataList(RedisKey key)
        {
            entries = Database.HashGetAll(key);
            infoList = new List<string>();
            foreach (HashEntry entry in entries)
            {
                infoList.Add("Id: " + entry.Value + " \tName: " + entry.Name);
            }
            return infoList;
        }

        public string GetInfoValue(string id, int num)
        {
            RedisKey infoKey = userPrefix + id;
            HashEntry[] entries = Database.HashGetAll(infoKey);

            return entries[num].Value;
        }    

        public List<string> GetInfo(RedisKey key)
        {
            entries = Database.HashGetAll(key);
            infoList = new List<string>();
            foreach (HashEntry entry in entries)
            {
                infoList.Add(entry.Name + " \t: " + entry.Value);
            }
            return infoList;
        }

            public long SignInDummy(long userId)
        {
            //TODO: what should this do?
            return userId;
        }
        
        public long SignIn(string username, string password)
        {
            RedisValue userId = Database.HashGet(USERS, username);
            if (userId.IsNull)
                return -1;
                //throw new Exception("Error: Sign in failed.");

            string realPassword = (string)Database.HashGet(userPrefix + (long)userId, "password");
            if (password != realPassword)
                return -1;
                //throw new Exception("Error: Sign in failed.");

            Database.SetAdd(CURRENTUSERS, userId);
            return (long)userId;
        }

        //TODO: maybe return int instead of bool for error return value consistency?
        public bool SignOut(long userId)
        {
            if (!Database.SetRemove(CURRENTUSERS, userId))
                return false;
            return true;
                //throw new Exception("Error: not in set");
        }

        public long CreateRoom(string roomname)
        {
            if (Database.HashExists(ROOMS, roomname))
                return -1;

            long roomId = Database.StringIncrement(nextRoomId);
            // add to "Rooms" hashset (roomname to id mapping)
            HashEntry[] roomnameMapping = { new HashEntry(roomname, roomId) };
            Database.HashSet(ROOMS, roomnameMapping);

            return roomId;
        }
        
        public long CreateDummy()
        {
            long userId = Database.StringIncrement(nextUserId);
            RedisKey dummy = userPrefix + userId;

            string username = "dummy" + userId;
            string password = "dummy" + userId;
            // create dummy's hashset
            HashEntry[] userData = { new HashEntry("username", username),
                                     new HashEntry("password", password),
                                     new HashEntry("isDummy", 1),
                                     new HashEntry("chatCount", 0) };

            Database.HashSet(dummy, userData);

            // add to "users" hashset (username to id mapping)
            HashEntry[] usernameMapping = { new HashEntry(username, userId) };
            Database.HashSet(USERS, usernameMapping);

            return userId;
        }
        
        public long CreateUser(string username, string password, bool isDummy = false, int chatCount = 0)
        {
            if (Database.HashExists(USERS, username))
                return -1;
                //throw new Exception("Error: Sign up failed.");

            long userId = Database.StringIncrement(nextUserId);
            RedisKey user = userPrefix + userId;
            
            // create user's hashset
            HashEntry[] userData = { new HashEntry("username", username),
                                     new HashEntry("password", password),
                                     new HashEntry("isDummy", isDummy ? 1 : 0),
                                     new HashEntry("chatCount", chatCount) };
            
            Database.HashSet(user, userData);

            // add to "users" hashset (username to id mapping)
            HashEntry[] usernameMapping = { new HashEntry(username, userId) };
            Database.HashSet(USERS, usernameMapping);

            // add to ranking sorted set
            Database.SortedSetAdd(RANKINGS, userId, chatCount);

            return userId;
        }
        
        public void IncrementUserChatCount(long userId)
        {
            Database.HashIncrement(userPrefix + userId, "chatCount");
            Database.SortedSetIncrement(RANKINGS, userId, 1);
        }
        
        public void UpdateUser(long userId, HashEntry[] userInfo)
        {
            Database.HashSet(userPrefix + userId, userInfo);

            for(int i = 0; i < userInfo.Length; i++)
            {
                if(userInfo[i].Name == "chatCount")
                {
                    Database.SortedSetAdd(RANKINGS, userId, (double)userInfo[i].Value);
                    break;
                }
            }
        }

        public void DestroyUser(long userId)
        {
            string username = (string)Database.HashGet(userPrefix + (long)userId, "username");
            Database.KeyDelete(userPrefix + userId);
            Database.HashDelete(USERS, username);
            Database.SortedSetRemove(RANKINGS, userId);
        }

        //TODO: return Dictionary
        public Dictionary<string, double> GetAllTimeRankings(int endRank)
        {
            SortedSetEntry[] ranking = Database.SortedSetRangeByRankWithScores(RANKINGS, 0, endRank, Order.Descending);
            return ranking.ToStringDictionary();
            //Database.SortedSetRangeByRank(RANKINGS, 0, endRank, Order.Descending);
        }

        public Dictionary<string, double> GetAllTimeRankings(int startRank, int endRank)
        {
            SortedSetEntry[] ranking = Database.SortedSetRangeByRankWithScores(RANKINGS, startRank, endRank, Order.Descending);
            return ranking.ToStringDictionary();
        }

        private static IDatabase Database
        {
            get
            {
                //return ConnectionMultiplexer.Connect(configurationOptions).GetDatabase();
                return Connection.GetDatabase();
            }
        }

        private static ConnectionMultiplexer Connection
        {
            get
            {
                return LazyConnection.Value;
            }
        }

        private static readonly Lazy<ConnectionMultiplexer> LazyConnection
            = new Lazy<ConnectionMultiplexer>(() => ConnectionMultiplexer.Connect(configurationOptions));

    }
}