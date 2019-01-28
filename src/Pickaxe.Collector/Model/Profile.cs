using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Pickaxe.Collector.Model
{
    [Serializable]
    class Profile
    {
        public long ID { get; private set; }
        public int Level { get; private set; }

        public long MaxID { get; set; }
        public int Tweets { get; set; }

        public string UserProfile { get; set; }
        public long[] Friends { get; set; }
        public long[] Followers { get; set; }
        public string Timeline { get; set; }

        public SourceType Source { get; set; }

        [NonSerialized]
        private int _cycle = 0;
        public int Cycle
        {
            get
            {
                return _cycle;
            }
            set
            {
                _cycle = value;
            }
        }

        public Profile(long id, int level)
        {
            this.ID = id;
            this.Level = level;
        }

        public bool HasProfile
        {
            get
            {
                return !string.IsNullOrEmpty(this.UserProfile);
            }
        }

        public bool HasFriends
        {
            get
            {
                return this.Friends != null;
            }
        }

        public bool HasFollowers
        {
            get
            {
                return this.Followers != null;
            }
        }

        public bool HasTimeline
        {
            get
            {
                return !string.IsNullOrEmpty(this.Timeline) && this.MaxID == 0;
            }
        }

        public bool IsComplete
        {
            get
            {
                return (
                    this.HasProfile &&
                    this.HasFriends &&
                    this.HasFollowers &&
                    this.HasTimeline &&
                    this.MaxID == 0
                );
            }
        }

        public bool IsEmpty
        {
            get
            {
                return (
                    !this.HasProfile &&
                    !this.HasFriends &&
                    !this.HasFollowers &&
                    !this.HasTimeline
                );
            }
        }
    }
}
