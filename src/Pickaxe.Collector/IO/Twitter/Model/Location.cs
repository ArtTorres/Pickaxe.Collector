using System.Runtime.Serialization;

namespace Pickaxe.Collector.IO.Twitter.Model
{
    [DataContract]
    public class Location
    {
        [DataMember(Name = "latitude1")]
        public double Latitude1 { get; set; }

        [DataMember(Name = "longitude1")]
        public double Longitude1 { get; set; }

        [DataMember(Name = "latitude2")]
        public double Latitude2 { get; set; }

        [DataMember(Name = "longitude2")]
        public double Longitude2 { get; set; }
    }
}
