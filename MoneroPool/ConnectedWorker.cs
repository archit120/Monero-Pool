using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MoneroPool
{
    public struct ShareJob
    {
        public int Seed;
        private ulong _currentDifficulty;
        public ulong CurrentDifficulty
        {
            get
            {
                return _currentDifficulty;
            }
            set
            {
                if (value > (uint)Statics.CurrentBlockTemplate["difficulty"])
                    value = (uint)Statics.CurrentBlockTemplate["difficulty"];
                if (value <= uint.Parse(Statics.Config.IniReadValue("base-difficulty")))
                    value = uint.Parse(Statics.Config.IniReadValue("base-difficulty"));
                _currentDifficulty = value;
            }
        }
    }
    public class ConnectedWorker
    {
        public string Address { get; set; }
        public DateTime LastSeen { get; set; }
        public List<KeyValuePair<TimeSpan,ulong>> ShareDifficulty { get; private set; }
        public DateTime LastShare { get; set; }

        public System.Net.Sockets.TcpClient TcpClient { get; set; }

        public List<KeyValuePair<string, ShareJob>> JobSeed { get; set; }
        public int CurrentBlock { get; set; }

        public int TotalShares { get; set; }
        public int RejectedShares { get; set; }

        public uint LastDifficulty { get; set; }
        public uint PendingDifficulty { get; set; }

        private DateTime _lastjoborshare;
        private DateTime _share;

        public ConnectedWorker()
        {
            JobSeed = new List<KeyValuePair<string, ShareJob>>();
            ShareDifficulty = new List<KeyValuePair<TimeSpan, ulong>>();
        }


        public void NewJobRequest()
        {
            _lastjoborshare = DateTime.Now;
        }

        public void ShareRequest(ulong difficulty)
        {
            _share = DateTime.Now;
            ShareDifficulty.Add(new KeyValuePair<TimeSpan, ulong>(_share - _lastjoborshare, difficulty));
            _lastjoborshare = _share;
            LastShare = DateTime.Now;
        }
    }
}
