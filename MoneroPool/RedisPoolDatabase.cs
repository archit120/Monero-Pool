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
        public List<Ban> Bans { get; private set; } 

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
            Information = new PoolInformation();
            Bans = new List<Ban>();

            //Start with blocks
            Deserialize(Blocks);
            Deserialize(Miners);
            Deserialize(BlockRewards);
            Deserialize(Shares);
            Deserialize(MinerWorkers);
            Deserialize(Information);
            Deserialize(Bans);

        }

        private void Deserialize<T>(T obj)
        {
            Type t = typeof(T);
            HashEntry[] hashEntries = RedisDb.HashGetAll(t.Name);
            try
            {
                foreach (var property in t.GetProperties())
                {
                    try
                    {
                        if (property.PropertyType == typeof (Int32))
                        {

                            property.SetValue(obj,
                                              JsonConvert.DeserializeObject<Int32>(
                                                  hashEntries.First(x => x.Name == property.Name).Value));
                        }
                        else if (property.PropertyType == typeof (List<string>))
                        {
                            property.SetValue(obj,
                                              JsonConvert.DeserializeObject<List<string>>(
                                                  hashEntries.First(x => x.Name == property.Name).Value));
                        }
                        else
                        {
                            property.SetValue(obj,
                                              JsonConvert.DeserializeObject(
                                                  hashEntries.First(x => x.Name == property.Name).Value));
                        }
                    }
                    catch   
                    {}
                }
            }
            catch
            {
            }
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

                    try
                    {
                        if (property.PropertyType == typeof (Int32))
                        {

                            property.SetValue(tobj,
                                              JsonConvert.DeserializeObject<Int32>(
                                                  hashEntries.First(x => x.Name == property.Name).Value));
                        }
                        else if (property.PropertyType == typeof (List<string>))
                        {
                            property.SetValue(tobj,
                                              JsonConvert.DeserializeObject<List<string>>(
                                                  hashEntries.First(x => x.Name == property.Name).Value));
                        }
                        else
                        {
                            property.SetValue(tobj,
                                              JsonConvert.DeserializeObject(
                                                  hashEntries.First(x => x.Name == property.Name).Value));
                        }
                    }
                    catch
                    {
                    }

                obj.Add(tobj);
            }
        }

        private void Serialize<T>(T obj)
        {
            Type t = typeof(T);
            PropertyInfo[] properties = t.GetProperties();

            HashEntry[] hashEntries = new HashEntry[properties.Length];
            int i = 0;
            foreach (PropertyInfo property in properties)
            {
                hashEntries[i] = new HashEntry(property.Name, JsonConvert.SerializeObject(property.GetValue(obj)));
                i++;
            }

            RedisDb.HashSet(t.Name, hashEntries);

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
            string guid = t.GetProperty("Identifier").GetValue(obj).ToString();
            RedisDb.SortedSetAdd(t.GetTypeInfo().Name,guid,RedisDb.SortedSetLength(t.GetTypeInfo().Name));
            RedisDb.HashSet(guid, hashEntries);
        }

        public void SaveChanges(Miner miner)
        {
            SaveChanges<Miner>(miner);

            for (int i = 0; i < Miners.Count; i++)
            {
                if (Miners[i].Identifier == miner.Identifier)
                {
                    Miners.RemoveAt(i);
                    Miners.Insert(i, miner);
                    return;
                }
            }
            Miners.Add(miner);
        }

        public void SaveChanges(MinerWorker minerWorker)
        {
            SaveChanges<MinerWorker>(minerWorker);


            for (int i = 0; i < MinerWorkers.Count; i++)
            {
                if (MinerWorkers[i].Identifier == minerWorker.Identifier)
                {
                    MinerWorkers.RemoveAt(i);
                    MinerWorkers.Insert(i, minerWorker);
                    return;
                }
            }
            MinerWorkers.Add(minerWorker);
        }

        public void SaveChanges(Share share)
        {
            SaveChanges<Share>(share);
            for (int i = 0; i < Shares.Count; i++)
            {
                if (Shares[i].Identifier == share.Identifier)
                {
                    Shares.RemoveAt(i);
                    Shares.Insert(i, share);
                    return;
                }
            }
            Shares.Add(share);
        }

        public void SaveChanges(BlockReward blockReward)
        {
            SaveChanges<BlockReward>(blockReward);

            for (int i = 0; i < BlockRewards.Count; i++)
            {
                if (BlockRewards[i].Identifier == blockReward.Identifier)
                {
                    BlockRewards.RemoveAt(i);
                    BlockRewards.Insert(i, blockReward);
                    return;
                }
            }
            BlockRewards.Add(blockReward);
        }
        public void SaveChanges(Block block)
        {
            SaveChanges<Block>(block);

            for (int i = 0; i < Blocks.Count; i++)
            {
                if (Blocks[i].Identifier == block.Identifier)
                {
                    Blocks.RemoveAt(i);
                    Blocks.Insert(i, block);
                    return;
                }
            }
            Blocks.Add(block);

        }
        public void SaveChanges(Ban ban)
        {
            SaveChanges<Ban>(ban);

            for (int i = 0; i < Bans.Count; i++)
            {
                if (Bans[i].Identifier == ban.Identifier)
                {
                    Bans.RemoveAt(i);
                    Bans.Insert(i, ban);
                    return;
                }
            }
            Bans.Add(ban);

        }
        public void SaveChanges(PoolInformation poolInformation)
        {
            Serialize(poolInformation);
            Information = poolInformation;
        }

        private void Remove<T>(T obj)
        {
            Type t = typeof (T);
            string guid = t.GetProperty("Identifier").GetValue(obj).ToString();
            RedisDb.SortedSetRemove(t.Name, guid);
            RedisDb.KeyDelete(guid);
        }
        public void Remove(MinerWorker worker)
        {
            Remove<MinerWorker>(worker);
            MinerWorkers.Remove(worker);
        }
    }

    public class Ban
    {
        public string IpBan { get; set; }
        public string AddressBan { get; set; }
        public string Identifier { get; set; }
        public DateTime Begin { get; set; }
        public int Minutes { get; set; }

        public Ban()
        {
            Identifier = Guid.NewGuid().ToString();
        }
    }

    public class PoolInformation
    {
        public int LastPaidBlock { get; set; }
        public int CurrentBlock { get; set; }
        public double NewtworkHashRate { get; set; }
        public double PoolHashRate { get; set; }

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
        public bool Orphan { get; set; }
        public DateTime FoundDateTime { get; set; }

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
        public Dictionary<DateTime,double> TimeHashRate { get; set; }
        public List<string> MinersWorker { get; set; }
        public List<string> BlockReward { get; set; }



        public Miner(string address, double hashRate)
        {
            MinersWorker = new List<string>();
            BlockReward = new List<string>();
            Address = address;
            TimeHashRate = new Dictionary<DateTime, double>();
            TimeHashRate.Add(DateTime.Now, hashRate);
            Identifier = Guid.NewGuid().ToString();
        }

        public Miner()
        {
        }
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

        public List<KeyValuePair<TimeSpan, ulong>> ShareDifficulty { get; private set; }

        private DateTime _lastjoborshare;
        private DateTime _share;

        public MinerWorker(string identifier,string miner, double hashRate)
        {
            Miner = miner;
            HashRate = hashRate;
            Connected = DateTime.Now;
            Identifier = identifier;
            ShareDifficulty = new List<KeyValuePair<TimeSpan, ulong>>();
        }

        public MinerWorker()
        {
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
            HashRate = Helpers.GetMinerWorkerHashRate(this);
        }
    }
}
