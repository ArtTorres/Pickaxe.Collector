using System;

namespace Pickaxe.Collector.IO.Twitter.Events
{
    public class LimitReachedEventArgs : EventArgs
    {
        public int NumberOfTweetsNotReceived { get; set; }
        public static LimitReachedEventArgs Create(int numberOfTweetsNotReceived)
        {
            return new LimitReachedEventArgs() { NumberOfTweetsNotReceived = numberOfTweetsNotReceived };
        }
    }
}
