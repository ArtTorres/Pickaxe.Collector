using Pickaxe.Collector.Model;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace Pickaxe.Collector.Controller
{
    class UserBench
    {
        private static UserBench _controller;

        private string _cacheDirectory;
        private int _fileIndex;

        private Queue<Profile> _bufferA;
        private Queue<Profile> _bufferB;
        private bool _enableA = true;
        private int _bufferSize = 1000;
        private int _cacheSize = 0;

        private string _bufferFilename;
        public string CacheFilename { get; private set; }

        private UserBench(string cacheDirectory)
        {
            _cacheDirectory = cacheDirectory;

            _bufferFilename = Path.Combine(_cacheDirectory, "bench[{0}].bin");
            this.CacheFilename = string.Format(_bufferFilename, 0);

            _bufferA = new Queue<Profile>(_bufferSize);
            _bufferB = new Queue<Profile>(_bufferSize);
        }

        public static UserBench Create(string cacheDirectory)
        {
            if (_controller == null)
                _controller = new UserBench(cacheDirectory);

            return _controller;
        }

        public bool HasUsers
        {
            get
            {
                return _cacheSize > 0;
            }
        }

        public int TotalUsers
        {
            get
            {
                return _cacheSize;
            }
        }

        public Profile TakeUser()
        {
            if (_enableA && _bufferA.Count == 0)
            { // Buffer A
                this.LoadCache(ref _bufferB);
                _enableA = false;
            }
            else if (!_enableA && _bufferB.Count == 0)
            { // Buffer B
                this.LoadCache(ref _bufferA);
                _enableA = true;
            }

            _cacheSize--;

            if (_enableA)
                return _bufferA.Dequeue();
            else
                return _bufferB.Dequeue();
        }

        public void AddUser(Profile profile)
        {
            if (_enableA && _bufferA.Count == _bufferSize)
            {
                _enableA = false;

                this.SaveCache(_bufferA, ++_fileIndex);
                _bufferA = new Queue<Profile>(_bufferSize);
            }
            else if (!_enableA && _bufferB.Count == _bufferSize)
            {
                _enableA = true;

                this.SaveCache(_bufferB, ++_fileIndex);
                _bufferB = new Queue<Profile>(_bufferSize);
            }

            profile.Source = SourceType.Bench;

            _cacheSize++;

            if (_enableA)
                _bufferA.Enqueue(profile);
            else
                _bufferB.Enqueue(profile);
        }

        private void SaveCache(Queue<Profile> buffer, int fileIndex)
        {
            if (Directory.Exists(_cacheDirectory))
            {
                IFormatter formatter = new BinaryFormatter();

                var filename = string.Format(_bufferFilename, fileIndex);

                using (Stream stream = new FileStream(filename, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    formatter.Serialize(stream, buffer);
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
                    formatter.Serialize(stream, _enableA ? _bufferA : _bufferB);
                }
            }
        }

        private void LoadCache(ref Queue<Profile> buffer)
        {
            var directory = new DirectoryInfo(_cacheDirectory);

            if (directory.Exists)
            {
                int fid = 0;

                var files = directory.GetFiles("*.bin");
                foreach (var file in files)
                {
                    var ixi = file.FullName.LastIndexOf('[');
                    var ixl = file.FullName.LastIndexOf(']');
                    var len = ixl - ixi - 1;
                    if (len <= 0) continue;

                    var tmp = int.Parse(file.FullName.Substring(ixi + 1, len));

                    if (fid == 0) fid = tmp;
                    else if (tmp < fid) fid = tmp;
                }

                IFormatter formatter = new BinaryFormatter();
                var filename = string.Format(_bufferFilename, fid);

                using (Stream stream = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    buffer = (Queue<Profile>)formatter.Deserialize(stream);
                }

                try
                {
                    File.Copy(filename, filename.Replace(".bin", ".old"));
                    File.Delete(filename);
                }
                catch
                {
                    // No error, continue
                }
            }
        }

        public void LoadCache()
        {
            var directory = new DirectoryInfo(_cacheDirectory);

            if (directory.Exists)
            {
                int fid = 0;
                int fileCount = 0;

                var files = directory.GetFiles("*.bin");
                foreach (var file in files)
                {
                    var ixi = file.FullName.LastIndexOf('[');
                    var ixl = file.FullName.LastIndexOf(']');
                    var len = ixl - ixi - 1;
                    if (len <= 0) continue;

                    fid = int.Parse(file.FullName.Substring(ixi + 1, len));

                    if (fid == 0) continue;
                    else fileCount++;
                }

                _fileIndex = fileCount;

                IFormatter formatter = new BinaryFormatter();

                using (Stream stream = new FileStream(this.CacheFilename, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    if (_enableA)
                    {
                        _bufferA = (Queue<Profile>)formatter.Deserialize(stream);
                        _cacheSize = fileCount * _bufferSize + _bufferA.Count;
                    }
                    else
                    {
                        _bufferB = (Queue<Profile>)formatter.Deserialize(stream);
                        _cacheSize = fileCount * _bufferSize + _bufferB.Count;
                    }
                }
            }
        }
    }
}
