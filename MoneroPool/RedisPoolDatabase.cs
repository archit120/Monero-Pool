using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StackExchange.Redis;
using Newtonsoft.Json;

namespace MoneroPool
{
    public class RedisPoolDatabase
    {
        public IDatabase RedisDb { get; set; }

        public List<Block> Blocks { get; private set; }
        public List<Miner> Miners { get; private set; }
        public List<BlockReward> BlockRewards { get; private set; }
        public List<Share> Shares { get; private set; }
        public List<MinerWorker> MinerWorkers { get; private set; } 

        public RedisPoolDatabase(IDatabase redisDb)
        {
            RedisDb = redisDb;
            UpdateLists();
        }

        public void UpdateLists()
        {
            Blocks = new List<Block>();
            Miners = new List<Miner>();
            BlockRewards = new List<BlockReward>();
            Shares = new List<Share>();
            MinerWorkers = new List<MinerWorker>();

            //Start with blocks
            HashEntry[] blocks = RedisDb.HashGetAll("blocks");
            Parallel.ForEach(blocks, block => Blocks.Add(JsonConvert.DeserializeObject<Block>(block.Value)));

            HashEntry[] miners = RedisDb.HashGetAll("miners");
            Parallel.ForEach(miners, miner => Miners.Add(JsonConvert.DeserializeObject<Miner>(miner.Value)));

            HashEntry[] blockrewards = RedisDb.HashGetAll("blockrewards");
            Parallel.ForEach(blockrewards, blockreward => BlockRewards.Add(JsonConvert.DeserializeObject<BlockReward>(blockreward.Value)));

            HashEntry[] shares = RedisDb.HashGetAll("shares");
            Parallel.ForEach(shares, share => Shares.Add(JsonConvert.DeserializeObject<Share>(share.Value)));

            HashEntry[] minerworkers = RedisDb.HashGetAll("minerworkers");
            Parallel.ForEach(minerworkers, minerworker => MinerWorkers.Add(JsonConvert.DeserializeObject<MinerWorker>(minerworker.Value)));
        }

        public void SaveChanges(Block block)
        {
            string stringify = JsonConvert.SerializeObject(block);
            RedisDb.HashSet( "blocks",block.Identifier, stringify);

            UpdateLists();
        }

        public void SaveChanges(Miner miner)
        {
            string stringify = JsonConvert.SerializeObject(miner);
            RedisDb.HashSet("miners",miner.Identifier, stringify);

            UpdateLists();

        }

        public void SaveChanges(MinerWorker minerWorker)
        {
            string stringify = JsonConvert.SerializeObject(minerWorker);
            RedisDb.HashSet("minerworkers",minerWorker.Identifier, stringify);

            UpdateLists();

        }

        public void SaveChanges(Share share)
        {
            string stringify = JsonConvert.SerializeObject(share);
            RedisDb.HashSet("shares", share.Identifier, stringify);
            UpdateLists();

        }

        public void SaveChanges(BlockReward blockreward)
        {
            string stringify = JsonConvert.SerializeObject(blockreward);
            RedisDb.HashSet("blockrewards", blockreward.Identifier, stringify);
            UpdateLists();

        }
    }

    public class Block
    {
        public string Identifier { get; set; }

        public int BlockHeight { get; set; }

        public List<string> BlockRewards { get; set; }

        public Block(int blockHeight)
        {
            Identifier= Guid.NewGuid().ToString();
            BlockRewards = new List<string>();
            BlockHeight = blockHeight;
        }
        public Block(){}
    }

    public class Miner
    {
        public string Identifier { get; set; }
        public string Address { get; set; }
        public double HashRate { get; set; }
        public List<string> MinersWorker { get; set; }
        public List<string> BlockReward { get; set; } 

        public Miner(string address, double hashRate)
        {
            MinersWorker = new List<string>();
            BlockReward = new List<string>();
            Address = address;
            HashRate = hashRate;
            Identifier=Guid.NewGuid().ToString();
        }
        public Miner()
        {}
    }

    public class BlockReward
    {
        public string Identifier { get; set; }
        public string Miner { get; set; }
        public string Block { get; set; }

        public List<string> Shares { get; set; }

        public BlockReward(string miner, string block)
        {
            Shares = new List<string>();
            Block = block;
            Miner = miner;
            Identifier = Guid.NewGuid().ToString();
        }

        public BlockReward()
        {
        }
    }

    public class Share
    {
        public string Identifier { get; set; }
        public string BlockReward { get; set; }

        public DateTime DateTime { get; set; }
        public double Value { get; set; }

        public Share(string blockReward, double value)
        {
            BlockReward = blockReward;
            Value = value;
            Identifier = Guid.NewGuid().ToString();
        }

        public Share()
        {
        }
    }

    public class MinerWorker
    {
        public string Identifier { get; set; }
        public DateTime Connected { get; set; }
        public string Miner { get; set; }
        public double HashRate { get; set; }

        public MinerWorker(string miner, double hashRate)
        {
            Miner = miner;
            HashRate = hashRate;
            Connected = DateTime.Now;
            Identifier = Guid.NewGuid().ToString();
        }

        public MinerWorker()
        {
        }
    }
}
