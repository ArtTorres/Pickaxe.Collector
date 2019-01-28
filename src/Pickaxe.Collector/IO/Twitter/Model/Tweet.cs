using System;

namespace Pickaxe.Collector.IO.Twitter.Model
{
    public class Tweet
    {
        public string ID { get; set; }
        public string Text { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Language { get; set; }
        public string Coordinates { get; set; }
        public string CreatorID { get; set; }
        public string CreatorName { get; set; }
        public string CreatorLanguage { get; set; }
        public int FavoriteCount { get; set; }
        public int RetweetCount { get; set; }
        public string ReplyStatusID { get; set; }

        public string JSON { get; set; }

        private string[] _hashTags;
        public string[] HashTags
        {
            get
            {
                if (_hashTags == null)
                    _hashTags = new string[0];
                return _hashTags;
            }
            set
            {
                _hashTags = value;
            }
        }

        private string[] _urls;
        public string[] Urls
        {
            get
            {
                if (_urls == null)
                    _urls = new string[0];
                return _urls;
            }
            set
            {
                _urls = value;
            }
        }

        public override string ToString()
        {
            return string.Format("{0},{1},{2}",
                ID,
                CreatorName,
                Text
            );
        }
    }
}
