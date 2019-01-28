using Newtonsoft.Json;

namespace Pickaxe.Collector.Model
{
    class Account
    {
        [JsonProperty("api_key")]
        public string ApiKey { get; set; }
        [JsonProperty("api_secret")]
        public string ApiSecret { get; set; }
        [JsonProperty("token_key")]
        public string TokenKey { get; set; }
        [JsonProperty("token_secret")]
        public string TokenSecret { get; set; }
        [JsonProperty("proxy")]
        public string Proxy { get; set; }
    }
}
