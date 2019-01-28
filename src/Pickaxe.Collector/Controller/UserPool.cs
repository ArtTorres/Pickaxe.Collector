using Pickaxe.Collector.Model;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace Pickaxe.Collector.Controller
{
    class UserPool
    {
        private static UserPool _controller;

        private Dictionary<long, ProfileCheck> _checkPool;
        private Queue<Profile> _userQueue;
        private string _cacheDirectory;

        public string CacheFilename { get; private set; }

        private UserPool(string cacheDirectory)
        {
            _cacheDirectory = cacheDirectory;
            _checkPool = new Dictionary<long, ProfileCheck>();
            _userQueue = new Queue<Profile>();

            this.CacheFilename = Path.Combine(_cacheDirectory, "pool.bin");
        }

        public static UserPool Create(string cacheDirectory)
        {
            if (_controller == null)
                _controller = new UserPool(cacheDirectory);

            return _controller;
        }

        public bool HasUsers
        {
            get
            {
                return _userQueue.Count > 0;
            }
        }

        public int TotalUsers
        {
            get
            {
                return _userQueue.Count;
            }
        }

        public Profile TakeUser()
        {
            return _userQueue.Dequeue();
        }

        public void SetCompleted(long userId)
        {
            _checkPool[userId].Completed = true;
        }

        public void AddUser(long userId, int level = 0, bool completed = false, SourceType source = SourceType.Pool)
        {
            if (!_checkPool.ContainsKey(userId))
            {
                var check = new ProfileCheck()
                {
                    ID = userId,
                    Level = level,
                    Completed = completed
                };

                _checkPool.Add(userId, check);

                if (!completed)
                {
                    var profile = new Profile(check.ID, check.Level) { Source = source };
                    _userQueue.Enqueue(profile);
                }
            }
        }

        public void SaveCache()
        {
            if (Directory.Exists(_cacheDirectory))
            {
                IFormatter formatter = new BinaryFormatter();

                using (Stream stream = new FileStream(this.CacheFilename, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    formatter.Serialize(stream, _checkPool);
                    formatter.Serialize(stream, _userQueue);
                }
            }
        }

        public void LoadCache()
        {
            if (Directory.Exists(_cacheDirectory))
            {
                IFormatter formatter = new BinaryFormatter();

                using (Stream stream = new FileStream(this.CacheFilename, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    _checkPool = (Dictionary<long, ProfileCheck>)formatter.Deserialize(stream);
                    _userQueue = (Queue<Profile>)formatter.Deserialize(stream);
                }
            }
        }
    }
}
