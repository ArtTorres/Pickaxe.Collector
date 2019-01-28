using System.Linq;
using System.Net;
using Tweetinvi;
using Tweetinvi.Models;
using Pickaxe.Collector.IO.Twitter.Model;
using Pickaxe.Collector.IO.Twitter.Security;

namespace Pickaxe.Collector.IO.Twitter
{
    public abstract class TwitterManager
    {
        public string ConsumerKey { get; private set; }
        public string ConsumerSecret { get; private set; }
        public string AccessTokenKey { get; private set; }
        public string AccessTokenSecret { get; private set; }

        public bool Threadless { get; private set; }

        public TwitterCredential Credential { get; private set; }

        public TwitterManager(string consumerKey, string consumerSecret, string accessTokenKey, string accessTokenSecret, bool threadless = false)
        {
            this.ConsumerKey = consumerKey;
            this.ConsumerSecret = consumerSecret;
            this.AccessTokenKey = accessTokenKey;
            this.AccessTokenSecret = accessTokenSecret;
            this.Threadless = threadless;

            this.Credential = new TwitterCredential()
            {
                ApiKey = consumerKey,
                ApiSecret = consumerSecret,
                TokenKey = accessTokenKey,
                TokenSecret = accessTokenSecret
            };

            if (!threadless)
            {
                Auth.ApplicationCredentials = new TwitterCredentials(
                               consumerKey,
                               consumerSecret,
                               accessTokenKey,
                               accessTokenSecret
               );
            }
        }

        public void SetProxy(string proxyConnectionString, int requestTimeout = 0, int uploadTimeout = 0)
        {
            this.SetProxy(new ProxyInfo(proxyConnectionString), requestTimeout, uploadTimeout);
        }

        public void SetProxy(ProxyInfo proxy, int requestTimeout = 0, int uploadTimeout = 0)
        {
            WebRequest.DefaultWebProxy = new WebProxy(proxy.FullAddress, false, new string[0], new NetworkCredential(proxy.User, proxy.Password));

            //if (requestTimeout != 0)
            //    TweetinviConfig.CurrentThreadSettings.HttpRequestTimeout = requestTimeout;

            //if (uploadTimeout != 0)
            //    TweetinviConfig.CurrentThreadSettings.UploadTimeout = uploadTimeout;
        }

        public static Model.Tweet ParseTweet(ITweet tweet)
        {
            return new Model.Tweet()
            {
                ID = tweet.IdStr,
                Text = tweet.Text,
                CreatedAt = tweet.CreatedAt,
                CreatorID = tweet.CreatedBy.IdStr,
                CreatorName = tweet.CreatedBy.Name,
                CreatorLanguage = tweet.CreatedBy.Language.ToString(),
                Language = tweet.Language.ToString(),
                Coordinates = tweet.Coordinates == null ? string.Empty : string.Format("[{0},{1}]", tweet.Coordinates.Latitude, tweet.Coordinates.Longitude),
                FavoriteCount = tweet.FavoriteCount,
                RetweetCount = tweet.RetweetCount,
                ReplyStatusID = tweet.InReplyToStatusIdStr,
                HashTags = tweet.Hashtags.Select(s => s.Text).ToArray(),
                Urls = tweet.Urls.Select(s => s.ExpandedURL).ToArray()
            };
        }
    }
}
