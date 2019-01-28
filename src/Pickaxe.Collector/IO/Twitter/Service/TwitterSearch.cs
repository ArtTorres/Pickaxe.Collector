using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using Tweetinvi.Models;
using Tweetinvi.Parameters;
using Pickaxe.Collector.IO.Twitter.Events;
using Pickaxe.Collector.IO.Twitter.Model;
using TweetSearch = Tweetinvi.Search;

namespace Pickaxe.Collector.IO.Twitter.Service
{
    public class TwitterSearch : TwitterManager
    {
        private class OperationAdapter<T>
        {
            private Func<T, T> _operation;
            private T _input;

            public OperationAdapter(Func<T, T> operation, T input)
            {
                _operation = operation;
                _input = input;
            }

            public T Execute()
            {
                return _operation(_input);
            }
        }

        public event EventHandler<TweetReceivedEventArgs> TweetReceived;
        //public event EventHandler<LimitReachedEventArgs> LimitReached;
        public event EventHandler<JsonObjectEventArgs> JsonObjectReceived;

        #region Event Definition
        private void OnTweetReceived(TweetReceivedEventArgs e)
        {
            if (TweetReceived != null)
                TweetReceived(this, e);
        }
        private void OnJsonObjectReceived(JsonObjectEventArgs e)
        {
            if (JsonObjectReceived != null)
                JsonObjectReceived(this, e);
        }
        //private void OnLimitReached(LimitReachedEventArgs e)
        //{
        //    if (LimitReached != null)
        //        LimitReached(this, e);
        //}
        #endregion

        public TwitterSearch(string consumerKey, string consumerSecret, string accessTokenKey, string accessTokenSecret, bool threadless = false)
            : base(consumerKey, consumerSecret, accessTokenKey, accessTokenSecret, threadless)
        {
        }

        public IEnumerable<Tweet> Search(string query, int maxResults = 100, long sinceId = 0, long maxId = 0, string location = null, DateTime? since = null, DateTime? until = null)
        {
            // Search Parameter
            var searchParameter = Tweetinvi.Search.CreateTweetSearchParameter(query);
            searchParameter.MaximumNumberOfResults = maxResults;
            searchParameter.TweetSearchType = TweetSearchType.All;

            if (sinceId != 0)
                searchParameter.SinceId = sinceId;

            if (maxId != 0)
                searchParameter.MaxId = maxId;

            if (since.HasValue)
                searchParameter.Since = since.Value;

            if (until.HasValue)
                searchParameter.Until = until.Value;

            if (!string.IsNullOrEmpty(location))
            {
                string[] param = location.Split(new char[] { ',' });
                if (param.Length == 3)
                {
                    double latitude = double.Parse(param[0]);
                    double longitude = double.Parse(param[1]);
                    double radius = double.Parse(param[2]);
                    searchParameter.SetGeoCode(latitude, longitude, radius, DistanceMeasure.Kilometers);
                }
            }

            // Execution
            foreach (var tweet in TweetSearch.SearchTweets(searchParameter))
            {
                if (tweet.IdStr.Equals(maxId.ToString()))
                {
                    continue;
                }
                Tweet t = ParseTweet(tweet);
                this.OnJsonObjectReceived(new JsonObjectEventArgs() { Json = JsonConvert.SerializeObject(tweet.TweetDTO) });
                this.OnTweetReceived(TweetReceivedEventArgs.Create(t));


                yield return t;
            }
        }

        public TimelineResponse GetUserTimeline(long userId, int maxTweetByTimeline = 200, long sinceId = 0, long maxId = 0, bool includeRts = false)
        {
            var url = "https://api.twitter.com/1.1/statuses/user_timeline.json?count=" + maxTweetByTimeline + "&trim_user=1&user_id=" + userId;

            if (includeRts) url += "&include_rts=1"; else url += "&include_rts=0";
            if (maxId != 0) url += "&max_id=" + maxId;
            if (sinceId != 0) url += "&since_id=" + sinceId;

            var response = this.Threadless ? ExecuteThreadlessJsonRequest(url) : Tweetinvi.TwitterAccessor.ExecuteGETQueryReturningJson(url);

            if (response != null)
            {
                var document = JArray.Parse(response);

                long min_id = -1;
                long max_id = 0;
                int count = 0;

                foreach (var tweet in document)
                {
                    long id = long.Parse(tweet.SelectToken("$.id").ToString());

                    if (max_id < id) max_id = id;
                    if (id < min_id || min_id == -1) min_id = id;

                    count++;
                }

                return new TimelineResponse() { Content = response, MaxID = max_id, MinID = min_id, Tweets = count };
            }

            return new TimelineResponse();
        }

        public IEnumerable<long> GetFriends(long userId, int maxFriends = 5000, bool randomize = false)
        {
            var url = "https://api.twitter.com/1.1/friends/ids.json?user_id=" + userId;

            var response = this.Threadless ? ExecuteThreadlessJsonRequest(url) : Tweetinvi.TwitterAccessor.ExecuteGETQueryReturningJson(url);

            List<long> output = null;

            if (response != null)
            {
                var document = JObject.Parse(response);

                var idToken = document.SelectToken("$.ids");

                if (idToken != null)
                {
                    var ids = (JArray)idToken;

                    output = new List<long>();

                    foreach (var id in ids)
                    {
                        output.Add(long.Parse(id.ToString()));
                    }

                    if (output.Count > maxFriends)
                    {
                        if (randomize)
                        {
                            return output.OrderBy(x => Guid.NewGuid()).Take(maxFriends);
                        }
                        else
                            return output.GetRange(0, maxFriends);
                    }
                }
            }

            return output;
        }

        public IEnumerable<long> GetFollowers(long userId, int maxFollowers = 5000, bool randomize = false)
        {
            var url = "https://api.twitter.com/1.1/followers/ids.json?user_id=" + userId;

            var response = this.Threadless ? ExecuteThreadlessJsonRequest(url) : Tweetinvi.TwitterAccessor.ExecuteGETQueryReturningJson(url);

            List<long> output = null;

            if (response != null)
            {
                var document = JObject.Parse(response);

                var idToken = document.SelectToken("$.ids");

                if (idToken != null)
                {
                    var ids = (JArray)idToken;

                    output = new List<long>();

                    foreach (var id in ids)
                    {
                        output.Add(long.Parse(id.ToString()));
                    }

                    if (output.Count > maxFollowers)
                    {
                        if (randomize)
                        {
                            return output.OrderBy(x => Guid.NewGuid()).Take(maxFollowers);
                        }
                        else
                            return output.GetRange(0, maxFollowers);
                    }
                }
            }

            return output;
        }

        public string GetUser(long userId)
        {
            var url = "https://api.twitter.com/1.1/users/show.json?user_id=" + userId;

            if (this.Threadless)
            {
                return ExecuteThreadlessJsonRequest(url);
            }

            return Tweetinvi.TwitterAccessor.ExecuteGETQueryReturningJson("https://api.twitter.com/1.1/users/show.json?user_id=" + userId);
        }

        private string ExecuteThreadlessJsonRequest(string input)
        {
            var adapter = new OperationAdapter<string>(Tweetinvi.TwitterAccessor.ExecuteGETQueryReturningJson, input);

            return Tweetinvi.Auth.ExecuteOperationWithCredentials<string>(
                Tweetinvi.Auth.CreateCredentials(
                    this.Credential.ApiKey,
                    this.Credential.ApiSecret,
                    this.Credential.TokenKey,
                    this.Credential.TokenSecret
                ),
                new Func<string>(adapter.Execute)
            );
        }

        private List<T> GetRandom<T>(List<T> list, int length)
        {
            var r = new Random();
            var output = new List<T>();

            for (int i = 0; i < length; i++)
            {
                int ix = r.Next(length);
                output.Add(list[ix]);
                list.RemoveAt(ix);
            }

            return output;
        }
    }
}
