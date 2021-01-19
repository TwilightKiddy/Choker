using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Choker
{
    public class UserChokeData
    {
        public DateTime Time;
        public DateTime LastMute;
        public List<byte> PcmData;
        public int Count;
        public int SessionChokes;
        public int Chokes;
    }

    public class ServerSessionChokeStash : Dictionary<ulong, UserChokeData>
    {
        public DateTime SessionStart { get; }
        public ServerSessionChokeStash()
        {
            SessionStart = DateTime.Now;
        }

        public async Task<UserChokeData> GetOrCreateUserData(ulong userId)
        {
            if (ContainsKey(userId))
                return this[userId];
            else
            {
                Add(userId, new UserChokeData
                {
                    Time = DateTime.Now,
                    LastMute = DateTime.MinValue,
                    PcmData = new List<byte>(),
                    Count = 0,
                    SessionChokes = 0,
                    Chokes = await DataBase.GetUserChokes(userId)
                });
                return this[userId];
            }

        }
    }
    public class ChokeDataStash : Dictionary<ulong, ServerSessionChokeStash>
    {
        public ServerSessionChokeStash GetServerSessionStash(ulong serverId)
            => ContainsKey(serverId) ? this[serverId] : null;

        public void StartServerSession(ulong serverId)
            => Add(serverId, new ServerSessionChokeStash());

        public void EndServerSesson(ulong serverId)
            => Remove(serverId);
    }
}
