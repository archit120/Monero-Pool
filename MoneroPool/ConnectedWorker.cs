using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MoneroPool
{
    public class ConnectedWorker
    {
        public string Address { get; set; }
        public DateTime LastSeen { get; set; }
        public List<KeyValuePair<TimeSpan,ulong>> ShareDifficulty { get; private set; }
        private uint _currentDifficulty;
        public uint CurrentDifficulty { get
        {
            return _currentDifficulty;
        }
            set
            {
                if (value > (uint) Statics.CurrentBlockTemplate["difficulty"])
                    value = (uint) Statics.CurrentBlockTemplate["difficulty"];
                _currentDifficulty = value;
            } }
        public int JobSeed { get; set; }
        public int CurrentBlock { get; set; }

        public int TotalShares { get; set; }
        public int RejectedShares { get; set; }

        private DateTime _lastjoborshare;
        private DateTime _share;

        public ConnectedWorker()
        {
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
        }
    }
}
