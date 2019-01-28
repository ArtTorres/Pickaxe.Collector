using System;

namespace Pickaxe.Collector.IO.Twitter.Events
{
    public class JsonObjectEventArgs : EventArgs
    {
        public string Json { get; set; }
    }
}
