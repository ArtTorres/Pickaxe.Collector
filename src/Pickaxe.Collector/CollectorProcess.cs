using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Pickaxe.Collector.Controller;
using Pickaxe.Collector.IO;
using Pickaxe.Collector.Model;
using QApp;
using QApp.Events;
using QApp.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace Pickaxe.Collector
{
    class CollectorProcess : QTask
    {
        //private const char UNIT_SEPARATOR = '\u241F';

        private CollectorOptions _options;
        private ResourceManager _resources;
        private UserPool _pool;
        private UserBench _bench;
        private bool _benchPriority = false;
        private int _collectedProfiles = 0;

        private int _benchLimit = 2;// 5

        public CollectorProcess(CollectorOptions options)
        {
            _options = options;
            _bench = UserBench.Create(_options.CacheDirectory);
            _pool = UserPool.Create(_options.CacheDirectory);

            var requestOptions = LoadRequestConfiguration(_options.ResourceConfigFile);

            _resources = new ResourceManager(
                requestOptions,
                options.IgnoreUserProfile,
                options.IgnoreFriends,
                options.IgnoreFollowers,
                options.IgnoreTimeline
            );
            _resources.NoResourceAvailable += _resources_NoResourceAvailable;
        }

        public void RestoreCache()
        {
            this.OnStarted(new MessageEventArgs("Restoring cache...", MessageType.Info));

            if (File.Exists(_bench.CacheFilename))
            {
                _bench.LoadCache();

                if (_bench.HasUsers)
                    _benchPriority = true;
            }

            if (File.Exists(_pool.CacheFilename))
                _pool.LoadCache();
        }

        public void RebuildCache()
        {
            this.OnStarted(new MessageEventArgs("Rebuilding cache...", MessageType.Info));

            if (File.Exists(_bench.CacheFilename))
                File.Delete(_bench.CacheFilename);

            if (File.Exists(_pool.CacheFilename))
                File.Delete(_pool.CacheFilename);

            foreach (var u in IdentifyIDsFromFolder())
            {
                _pool.AddUser(u, 0, true);
            }
        }

        private IEnumerable<long> IdentifyIDsFromFolder()
        {
            var output = new List<long>();
            var dir = new DirectoryInfo(_options.OutputDirectory);

            foreach (var file in dir.GetFiles("*" + IO.ProfileFile.TRACK_EXT))
            {
                int ix = file.Name.IndexOf('.');
                string id = file.Name.Substring(0, ix);
                output.Add(long.Parse(id));
            }

            return output;
        }

        private RequestMonitorOptions[] LoadRequestConfiguration(string filename)
        {
            RequestMonitorOptions[] output = null;

            using (var reader = new StreamReader(filename))
            {
                output = JArray.Parse(reader.ReadToEnd()).ToObject<RequestMonitorOptions[]>();
            }

            return output;
        }

        public static void SaveRequestConfiguration(RequestMonitorOptions[] options, string filename)
        {
            using (var writer = new StreamWriter(filename))
            {
                writer.Write(JsonConvert.SerializeObject(options));
            }
        }

        private void _resources_NoResourceAvailable(object sender, WindowMessageArgs e)
        {
            // Save pool on cache
            _pool.SaveCache();

            // Save bench on cache
            _bench.SaveCache();

            // Stop all resources
            _resources.StopResources();

            // Free resources
            System.GC.Collect();
            System.GC.WaitForPendingFinalizers();
  
            // Wait until new window is available
            this.OnProgress(new MessageEventArgs(MessageType.Warning, MessagePriority.Medium, "No resources available. New start time: {0:hh:mm:ss} on {0:dd/MM/yy}", DateTime.Now.Add(e.WaitTime)));
            Thread.Sleep(e.WaitTime);

            // Give priority to the bench
            _benchPriority = true;
        }

        public void AddToPool(IEnumerable<long> users, int level = 0)
        {
            if (level > 1) return;

            foreach (var u in users)
            {
                _pool.AddUser(u, level);
            }
        }

        public override void Start()
        {
            try
            {
                this.OnStarted(new MessageEventArgs("The profile collector process is started.", MessageType.Info));

                this.Collect();

                this.OnCompleted(new MessageEventArgs("The profile collector process is completed.", MessageType.Info));
            }
            catch (Exception ex)
            {
                this.OnFailed(new MessageEventArgs("The profile collector process failed with the message: " + ex.Message, MessageType.Error));
            }
        }

        private void Collect()
        {
            while (_pool.HasUsers || _bench.HasUsers)
            {
                if (_benchPriority && _bench.TotalUsers == 0)
                    _benchPriority = false;

                // Take User
                var user = (_benchPriority && _bench.HasUsers) || (!_pool.HasUsers && _bench.HasUsers) ? _bench.TakeUser() : _pool.TakeUser();

                // Request Data
                if (!user.HasProfile)
                {
                    if (_options.IgnoreUserProfile)
                    {
                        // Default Value
                        user.UserProfile = "{}";
                    }
                    else
                    {
                        var resource = _resources.GetResource(RequestType.User);
                        if (resource != null)
                        {
                            var response = resource.GetUser(user.ID);
                            user.UserProfile = response;
                        }
                    }
                }

                if (!user.HasFriends)
                {
                    if (_options.IgnoreFriends)
                    {
                        // Default Value
                        user.Friends = new long[0];
                    }
                    else
                    {
                        var resource = _resources.GetResource(RequestType.Friends);
                        if (resource != null)
                        {
                            var response = resource.GetFriends(user.ID);

                            if (response != null)
                                user.Friends = response.ToArray();
                        }
                    }
                }

                if (!user.HasFollowers)
                {
                    if (_options.IgnoreFollowers)
                    {
                        // Default Value
                        user.Followers = new long[0];
                    }
                    else
                    {
                        var resource = _resources.GetResource(RequestType.Followers);
                        if (resource != null)
                        {
                            var response = resource.GetFollowers(user.ID);

                            if (response != null)
                                user.Followers = response.ToArray();
                        }
                    }
                }

                if (!user.HasTimeline)
                {
                    if (_options.IgnoreTimeline)
                    {
                        // Default Value
                        user.Timeline = "[]";
                    }
                    else
                    {
                    checkpoint:

                        var resource = _resources.GetResource(RequestType.Timeline);
                        if (resource != null)
                        {
                            var response = resource.GetUserTimeline(user.ID, 200, 0, user.MaxID, _options.IncludeRetweets);

                            if (!string.IsNullOrEmpty(response.Content))
                            {
                                if (response.Tweets > 0)
                                {
                                    user.Timeline += response.Content + ProfileFile.UNIT_SEPARATOR;
                                    user.Tweets += response.Tweets;

                                    if (response.Tweets > 50 && user.Tweets < _options.MaxTweetsByTimeline)
                                    {
                                        user.MaxID = response.MinID;
                                        goto checkpoint;
                                    }

                                    user.MaxID = 0;
                                }
                                else
                                {
                                    user.Timeline += response.Content + ProfileFile.UNIT_SEPARATOR;
                                    user.Tweets += response.Tweets;
                                }
                            }
                        }
                    }
                }

                if (user.IsComplete)
                {
                    // Add users to Pool
                    if (_options.AllowDiscovery)
                    {
                        this.AddToPool(user.Friends, user.Level + 1);
                        this.AddToPool(user.Followers, user.Level + 1);
                    }

                    // Save Data
                    var file = new ProfileFile(user, _options.OutputDirectory);
                    file.SaveProfile();

                    _pool.SetCompleted(user.ID);
                    _collectedProfiles++;
                }
                else
                {
                    if (user.Source == SourceType.Bench && user.Cycle < _benchLimit)
                    {
                        user.Cycle++;

                        // Enqueue until next window
                        _bench.AddUser(user);
                    }
                    else if (user.Source == SourceType.Bench && user.Cycle == _benchLimit)
                    {
                        // Set Default Values
                        if (!user.HasProfile)
                            user.UserProfile = "{}";
                        if (!user.HasFriends)
                            user.Friends = new long[0];
                        if (!user.HasFollowers)
                            user.Followers = new long[0];
                        if (!user.HasTimeline)
                            user.Timeline = "[]";

                        // Save Data
                        var partialDirectory = Path.Combine(_options.OutputDirectory, "part");
                        if (!Directory.Exists(partialDirectory))
                            Directory.CreateDirectory(partialDirectory);

                        var file = new ProfileFile(user, partialDirectory);
                        file.SaveProfile();

                        _pool.SetCompleted(user.ID);
                        _collectedProfiles++;
                    }
                    else
                    {
                        // Enqueue until next window
                        _bench.AddUser(user);
                    }

                    if (_pool.HasUsers && user.Source == SourceType.Bench)
                        _benchPriority = false;
                }
               
                this.OnProgress(new MessageEventArgs(MessageType.Progress, MessagePriority.Medium,
                    "Users: {0} / Pool: {1} / Bench: {2} / RC: {3}--",
                    _collectedProfiles,
                    _pool.TotalUsers,
                    _bench.TotalUsers,
                    _resources.Footprint
                ));

                _resources.RestartFootprint();

                _resources.CheckAvailability();
            }
        }
    }
}
