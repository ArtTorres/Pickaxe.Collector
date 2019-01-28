using Pickaxe.Collector.Model;
using Pickaxe.Collector.Tools;
using System.Collections.Generic;
using Pickaxe.Collector.IO.Twitter.Model;
using Pickaxe.Collector.IO.Twitter.Service;

namespace Pickaxe.Collector.Controller
{
    enum RequestType
    {
        Timeline,
        User,
        Friends,
        Followers
    }

    class RequestMonitor
    {
        //private const char UNIT_SEPARATOR = '\u241F';

        #region Extended Options

        public bool IgnoreUserProfile{get;set;}
        public bool IgnoreTimeline{get;set;}
        public bool IgnoreFriends{get;set;}
        public bool IgnoreFollowers{get;set;}

        #endregion

        public bool HasUserRequests
        {
            get
            {
                return !this.IgnoreUserProfile && (_usersRequest < _options.UsersRequestLimit);
            }
        }
        public bool HasFriendsRequests
        {
            get
            {
                return !this.IgnoreFriends && (_friendsRequests < _options.FriendsRequestLimit);
            }
        }
        public bool HasFollowersRequests
        {
            get
            {
                return !this.IgnoreFollowers && (_followersRequest < _options.FollowersRequestLimit);
            }
        }
        public bool HasTimelineRequests
        {
            get
            {
                return !this.IgnoreTimeline && (_timelineRequest < _options.TimelineRequestLimit);
            }
        }
        public bool HasTime
        {
            get
            {
                return (_window.LeftTime > 10);
            }
        }
        public bool HasRequests
        {
            get
            {
                return this.HasTime && (this.HasUserRequests || this.HasFriendsRequests || this.HasFollowersRequests || this.HasTimelineRequests);
            }
        }

        public int LeftTime
        {
            get
            {
                return _window.LeftTime;
            }
        }

        public bool IsWindowActive
        {
            get
            {
                return _window.IsActive;
            }
        }

        public string ID
        {
            get
            {
                return _options.ID;
            }
        }

        private RequestMonitorOptions _options;
        private TwitterSearch _manager;
        private Timer _window;
        private string _proxy;

        //private int _accountIndex = 0;
        private int _friendsRequests = 0;
        private int _followersRequest = 0;
        private int _usersRequest = 0;
        private int _timelineRequest = 0;

        public RequestMonitor(RequestMonitorOptions options)
        {
            _options = options;
            //_jsonCache = new StringBuilder();
            _window = new Timer(_options.WindowTime);
            this.SetAccount(_options.ConnectionAccount);
            _proxy = _options.ConnectionAccount.Proxy;
        }

        private void SetAccount(Account account)
        {
            _manager = new TwitterSearch(account.ApiKey, account.ApiSecret, account.TokenKey, account.TokenSecret, true);
        }

        private void SetProxy()
        {
            if (!string.IsNullOrEmpty(_proxy))
            {
                _manager.SetProxy(_proxy);
            }
        }

        #region Wrapped Operations

        public TimelineResponse GetUserTimeline(long userId, int maxTweetByRequest = 200, long sinceId = 0, long maxId = 0, bool includeRts = false)
        {
            this.SetProxy();

            var response = _manager.GetUserTimeline(userId, maxTweetByRequest, sinceId, maxId, includeRts);

            this.CountRequest(RequestType.Timeline);

            return response;
        }

        public IEnumerable<long> GetFriends(long userId)
        {
            this.SetProxy();

            var response = _manager.GetFriends(userId, _options.MaxFriendsByRequest, true);

            this.CountRequest(RequestType.Friends);

            return response;
        }

        public IEnumerable<long> GetFollowers(long userId)
        {
            this.SetProxy();

            var response = _manager.GetFollowers(userId, _options.MaxFollowersByRequest, true);

            this.CountRequest(RequestType.Followers);

            return response;
        }

        public string GetUser(long userId)
        {
            this.SetProxy();

            var response = _manager.GetUser(userId);

            this.CountRequest(RequestType.User);

            return response;
        }

        #endregion

        private void CountRequest(RequestType type)
        {
            switch (type)
            {
                case RequestType.Followers:
                    _followersRequest++;
                    break;
                case RequestType.Friends:
                    _friendsRequests++;
                    break;
                case RequestType.Timeline:
                    _timelineRequest++;
                    break;
                case RequestType.User:
                    _usersRequest++;
                    break;
            }
        }

        public void StartMonitor()
        {
            _window.Start();
            _friendsRequests = 0;
            _followersRequest = 0;
            _usersRequest = 0;
            _timelineRequest = 0;
        }

        public void StopMonitor()
        {
            _window.Stop();
        }
    }
}
