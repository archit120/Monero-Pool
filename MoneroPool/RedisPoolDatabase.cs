using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using StackExchange.Redis;
using Newtonsoft.Json;
using System.Reflection;

namespace MoneroPool
{
    public class RedisPoolDatabase
    {
        public IDatabase RedisDb { get; set; }

        public PoolInformation Information { get; set; }

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
            Deserialize(Blocks);
            Deserialize(Miners);
            Deserialize(BlockRewards);
            Deserialize(Shares);
            Deserialize(MinerWorkers);

        }


        private void Deserialize<T>(List<T> obj)
        {
            Type t = typeof(T);
            RedisValue[] redisValues = RedisDb.SortedSetRangeByScore(t.GetTypeInfo().Name);
            foreach (string redisValue in redisValues)
            {
                T tobj = (T) Activator.CreateInstance(t);
                HashEntry[] hashEntries = RedisDb.HashGetAll(redisValue);
                foreach (var property in t.GetProperties())
                {
                    if (property.PropertyType == typeof (Int32))
                    {
                        property.SetValue(tobj,
                                         JsonConvert.DeserializeObject<Int32>(
                                              hashEntries.First(x => x.Name == property.Name).Value));
                    }
                    else if(property.PropertyType==typeof(List<string>))
                    {
                        property.SetValue(tobj,
                                        JsonConvert.DeserializeObject <List<string>>(
                                              hashEntries.First(x => x.Name == property.Name).Value));
                    }
                    else
                    {
                        property.SetValue(tobj,
                                        JsonConvert.DeserializeObject(
                                              hashEntries.First(x => x.Name == property.Name).Value));
                    }
                } 
                foreach (var field in t.GetFields())
                {
                    if (field.GetType() == typeof(Int32))
                    {
                        field.SetValue(tobj,
                                      (Int32)JsonConvert.DeserializeObject(
                                           hashEntries.First(x => x.Name == field.Name).Value));
                    }
                    else
                    {
                        field.SetValue(tobj,
                                       JsonConvert.DeserializeObject(
                                           hashEntries.First(x => x.Name == field.Name).Value)); 
                    }
                }
                obj.Add(tobj);
            }
        }

        private void SaveChanges<T>(T obj)
        {
            Type t = typeof (T);
            PropertyInfo[] properties = t.GetProperties();
            FieldInfo[] fields = t.GetFields();
            HashEntry[] hashEntries = new HashEntry[properties.Length + fields.Length];
            int i = 0;
            foreach (PropertyInfo property in properties)
            {
                hashEntries[i] = new HashEntry(property.Name, JsonConvert.SerializeObject(property.GetValue(obj)));
                i++;
            } 
            foreach (FieldInfo field in fields)
            {
                hashEntries[i] = new HashEntry(field.Name, JsonConvert.SerializeObject(field.GetValue(obj)));
                i++;
            }

            string guid = Guid.NewGuid().ToString();
            RedisDb.SortedSetAdd(t.GetTypeInfo().Name,guid,RedisDb.SortedSetLength(t.GetTypeInfo().Name));
            RedisDb.HashSet(guid, hashEntries);
        }

        public void SaveChanges(Miner miner)
        {
            SaveChanges<Miner>(miner);
            Miners.Add(miner);

        }

        public void SaveChanges(MinerWorker minerWorker)
        {
            SaveChanges<MinerWorker>(minerWorker);
            MinerWorkers.Add(minerWorker);

        }

        public void SaveChanges(Share share)
        {
            SaveChanges<Share>(share);
            Shares.Add( share);

        }

        public void SaveChanges(BlockReward blockReward)
        {
            SaveChanges<BlockReward>(blockReward);
            BlockRewards.Add( blockReward);

        }
        public void SaveChanges(Block block)
        {
            SaveChanges<Block>(block);
            Blocks.Add(block);

        }

    }

    public class PoolInformation
    {
        public int LastPaidBlock;
        public int CurrentBlock;
        public int NewtworkHashRate { get; set; }
        public int PoolHashRate { get; set; }

        public PoolInformation()
        {
            
        }
    }

    public class Block
    {
        public string Identifier { get; set; }
        public string Founder { get; set; }
        public bool Found { get; set; }
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
