using System;

namespace Pickaxe.Collector.Model
{
    [Serializable]
    class ProfileCheck
    {
        public long ID { get; set; }
        public int Level { get; set; }
        public bool Completed { get; set; }
    }
}
