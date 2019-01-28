using System;
using System.Linq;

namespace Pickaxe.Collector.Controller
{
    class WindowMessageArgs : EventArgs
    {
        public TimeSpan WaitTime { get; set; }
    }

    class ResourceManager
    {
        #region Events
        public event EventHandler<WindowMessageArgs> NoResourceAvailable;
        private void OnNoResourceAvailable(WindowMessageArgs e)
        {
            if (NoResourceAvailable != null)
                NoResourceAvailable(this, e);
        }
        #endregion

        private RequestMonitor[] _resources;
        private int _index = 0;

        // Resource Flags
        private bool _outOfUser = false;
        private bool _outOfFriends = false;
        private bool _outOfFollowers = false;
        private bool _outOfTimeline = false;

        // Resource Counters
        private double _userRatio = 0.0;
        private double _friendsRatio = 0.0;
        private double _followersRatio = 0.0;
        private double _timelineRatio = 0.0;

        private int _userRequests = 0;
        private int _friendsRequests = 0;
        private int _followersRequests = 0;
        private int _timelineRequests = 0;
        private int _totalRequests = 0;

        // Restrictions
        private bool _ignoreUser;
        private bool _ignoreFriends;
        private bool _ignoreFollowers;
        private bool _ignoreTimeline;


        private int[] _footprint;
        public string Footprint
        {
            get
            {
                return string.Join("", _footprint);
            }
        }

        public ResourceManager(RequestMonitorOptions[] options, bool ignoreUser = false, bool ignoreFriends = false, bool ignoreFollowers = false, bool ignoreTimeline = false)
        {
            _resources = new RequestMonitor[options.Length];
            _footprint = new int[options.Length];

            for (int i = 0; i < options.Length; i++)
            {
                _resources[i] = new RequestMonitor(options[i])
                {
                    IgnoreUserProfile = ignoreUser,
                    IgnoreFriends = ignoreFriends,
                    IgnoreFollowers = ignoreFollowers,
                    IgnoreTimeline = ignoreTimeline
                };
            }

            _ignoreUser = ignoreUser;
            if (_ignoreUser) _outOfUser = true;

            _ignoreFriends = ignoreFriends;
            if (_ignoreFriends) _outOfFriends = true;

            _ignoreFollowers = ignoreFollowers;
            if (_ignoreFollowers) _outOfFollowers = true;

            _ignoreTimeline = ignoreTimeline;
            if (_ignoreTimeline) _outOfTimeline = true;
        }

        private void SetOutOf(RequestType type)
        {
            switch (type)
            {
                case RequestType.User:
                    _outOfUser = true;
                    break;
                case RequestType.Friends:
                    _outOfFriends = true;
                    break;
                case RequestType.Followers:
                    _outOfFollowers = true;
                    break;
                case RequestType.Timeline:
                    _outOfTimeline = true;
                    break;
            }
        }

        private void CountOf(RequestType type)
        {
            switch (type)
            {
                case RequestType.User:
                    _userRequests++;
                    break;
                case RequestType.Friends:
                    _friendsRequests++;
                    break;
                case RequestType.Followers:
                    _followersRequests++;
                    break;
                case RequestType.Timeline:
                    _timelineRequests++;
                    break;
            }

            _totalRequests++;

            // Update Ratios
            _userRatio = (double)_userRequests / _totalRequests;
            _friendsRatio = (double)_friendsRequests / _totalRequests;
            _followersRatio = (double)_followersRequests / _totalRequests;
            _timelineRatio = (double)_timelineRequests / _totalRequests;
        }

        private int GetBestResource(RequestType type)
        {
            this.CountOf(type);

            int c = 0; // checked

            // return resource available or look for another resource
            while (_index < _resources.Length)
            {
                if (!_resources[_index].IsWindowActive)
                    _resources[_index].StartMonitor();

                if (
                    (type == RequestType.User && _userRatio >= 0.05 && _resources[_index].HasUserRequests && _resources[_index].HasTime) ||
                    (type == RequestType.Friends && _friendsRatio >= 0.05 && _resources[_index].HasFriendsRequests && _resources[_index].HasTime) ||
                    (type == RequestType.Followers && _followersRatio >= 0.05 && _resources[_index].HasFollowersRequests && _resources[_index].HasTime) ||
                    (type == RequestType.Timeline && _timelineRatio >= 0.05 && _resources[_index].HasTimelineRequests && _resources[_index].HasTime)
                )
                    return _index;
                else
                    _index++;

                if (_index == _resources.Length)
                    _index = 0;

                if (++c == _resources.Length)
                    break;
            }

            this.SetOutOf(type);

            return -1;
        }

        public RequestMonitor GetResource(RequestType type)
        {
            var ix = this.GetBestResource(type);

            if (ix == -1)
                return null;
            else
            {
                _footprint[ix]++;
                var r = _resources[ix];

                // Point to a future resource
                _index++;
                if (_index == _resources.Length)
                    _index = 0;

                return r;
            }

        }

        public void CheckAvailability()
        {
            // If no resources available
            if (_outOfUser && _outOfFriends && _outOfFollowers && _outOfTimeline)
            {
                // Calculate wait time
                int seconds = _resources.Max(s => s.LeftTime);

                if (seconds < 30)
                    seconds = 30;

                // Restart resource warnings
                _outOfUser = _ignoreUser;// false;
                _outOfFriends = _ignoreFriends;// false;
                _outOfFollowers = _ignoreFollowers;// false;
                _outOfTimeline = _ignoreTimeline;// false;

                // Launch no resources event
                this.OnNoResourceAvailable(new WindowMessageArgs() { WaitTime = TimeSpan.FromSeconds(seconds) });
            }
        }

        public void StopResources()
        {
            for (int i = 0; i < _resources.Length; i++)
            {
                _resources[i].StopMonitor();
            }

            _userRequests = 0;
            _friendsRequests = 0;
            _followersRequests = 0;
            _timelineRequests = 0;
            _totalRequests = 0;
        }

        public void RestartFootprint()
        {
            for (int i = 0; i < _footprint.Length; i++)
                _footprint[i] = 0;
        }
    }
}
