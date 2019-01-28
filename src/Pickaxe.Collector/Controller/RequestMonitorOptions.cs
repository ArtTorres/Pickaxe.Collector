using Newtonsoft.Json;
using Pickaxe.Collector.Model;

namespace Pickaxe.Collector.Controller
{
    class RequestMonitorOptions
    {
        [JsonProperty("id")]
        public string ID { get; set; }
        [JsonProperty("window_time")]
        public int WindowTime { get; set; }
        [JsonProperty("friends_request_limit")]
        public int FriendsRequestLimit { get; set; }
        [JsonProperty("followers_request_limit")]
        public int FollowersRequestLimit { get; set; }
        [JsonProperty("users_request_limit")]
        public int UsersRequestLimit { get; set; }
        [JsonProperty("timeline_request_limit")]
        public int TimelineRequestLimit { get; set; }
        [JsonProperty("account")]
        public Account ConnectionAccount { get; set; }
        [JsonProperty("max_friends_by_request")]
        public int MaxFriendsByRequest { get; set; }
        [JsonProperty("max_followers_by_request")]
        public int MaxFollowersByRequest { get; set; }
    }
}
