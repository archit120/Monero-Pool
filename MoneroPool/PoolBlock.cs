using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MoneroPool
{
    public class PoolBlock
    {
        public byte[] BlockData { get; set; }
        public int BlockHeight { get; set; }
        public string BlockHash { get; set; }
        
        public PoolBlock(byte[] blockData, int blockHeight, string blockHash)
        {
            BlockData = blockData;
            BlockHash = blockHash;
            BlockHeight = blockHeight;
        }
    }
}
