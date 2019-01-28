using System;
using Pickaxe.Collector.IO.Twitter.Model;

namespace Pickaxe.Collector.IO.Twitter.Events
{
    public class TweetReceivedEventArgs : EventArgs
    {
        public Tweet Tweet { get; private set; }

        public static TweetReceivedEventArgs Create(Tweet tweet)
        {
            return new TweetReceivedEventArgs() { Tweet = tweet };
        }
    }
}
