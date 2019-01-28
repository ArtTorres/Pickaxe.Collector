
namespace Pickaxe.Collector.IO.Twitter.Model
{
    public class TimelineResponse
    {
        public string Content { get; set; }
        public long MaxID { get; set; }
        public long MinID { get; set; }
        public int Tweets { get; set; }
    }
}
