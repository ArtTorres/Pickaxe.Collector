using Newtonsoft.Json.Linq;
using Pickaxe.Collector.Model;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Pickaxe.Collector.IO
{
    class ProfileFile
    {
        public const char UNIT_SEPARATOR = '\u241F';
        public const char REC_SEPARATOR = '\u241E';

        // Extension for user information files
        public const string USER_EXT = ".user";

        // Extension for timeline files
        public const string PROFILE_EXT = ".data";

        // Extension for metadata files
        // TODO: change extension to .meta
        public const string TRACK_EXT = ".info";

        // Extension for social links files
        // TODO: change extension to .slnk
        public const string SOCIAL_LINKS_EXT = ".list";

        // Filenames
        private string _userFilename;
        private string _profileFilename;
        private string _trackFilename;
        private string _relationFilename;

        // Counters
        private string _screenName = string.Empty;
        private int _totalFriends = 0;
        private int _totalFollowers = 0;
        private int _totalTweets = 0;
        //private long _maxId = 0;
        //private long _sinceId = 0;

        private Profile _profile;

        public ProfileFile(string directory, long userId)
        {
            _userFilename = Path.Combine(directory, userId + USER_EXT);
            _profileFilename = Path.Combine(directory, userId + PROFILE_EXT);
            _trackFilename = Path.Combine(directory, userId + TRACK_EXT);
            _relationFilename = Path.Combine(directory, userId + SOCIAL_LINKS_EXT);
        }

        public ProfileFile(Profile profile, string outputDirectory)
        {
            _profile = profile;
            _userFilename = Path.Combine(outputDirectory, profile.ID + USER_EXT);
            _profileFilename = Path.Combine(outputDirectory, profile.ID + PROFILE_EXT);
            _trackFilename = Path.Combine(outputDirectory, profile.ID + TRACK_EXT);
            _relationFilename = Path.Combine(outputDirectory, profile.ID + SOCIAL_LINKS_EXT);
        }

        public void SaveProfile()
        {
            // Save User Profile
            var doc = JObject.Parse(_profile.UserProfile);
            var screenName = doc.SelectToken("$.screen_name");
            _screenName = screenName == null ? "unknown" : screenName.ToString();
            _totalTweets = _profile.Tweets;

            this.AppendOnDocument(_userFilename, REC_SEPARATOR, _profile.UserProfile);
            this.AppendOnDocument(_profileFilename, REC_SEPARATOR, _profile.Timeline);

            // Save Friends
            _totalFriends = _profile.Friends.Count();

            var dataFriends = new JArray();
            foreach (var f in _profile.Friends)
            {
                dataFriends.Add(f);
            }

            _totalFollowers = _profile.Followers.Count();

            var dataFollowers = new JArray();
            foreach (var f in _profile.Followers)
            {
                dataFollowers.Add(f);
            }

            this.AppendOnDocument(_relationFilename, REC_SEPARATOR, dataFriends.ToString(), dataFollowers.ToString());

            this.SaveData();
        }

        public void SaveUserProfile(string document)
        {
            var doc = JObject.Parse(document);

            _screenName = doc.SelectToken("$.screen_name").ToString();

            this.AppendOnDocument(_userFilename, document, REC_SEPARATOR);
        }
        public void SaveTimeLine(string document, int totalTweets)//, long maxId, long minId
        {
            _totalTweets = totalTweets;
            //_maxId = maxId;
            //_sinceId = minId;

            this.AppendOnDocument(_profileFilename, document, REC_SEPARATOR);
        }

        public void SaveFriends(IEnumerable<long> friends)
        {
            _totalFriends = friends.Count();

            var data = new JArray();
            foreach (var f in friends)
            {
                data.Add(f);
            }

            this.AppendOnDocument(_relationFilename, data.ToString(), REC_SEPARATOR);
        }
        public void SaveFollowers(IEnumerable<long> followers)
        {
            _totalFollowers = followers.Count();

            var data = new JArray();
            foreach (var f in followers)
            {
                data.Add(f);
            }

            this.AppendOnDocument(_relationFilename, data.ToString(), REC_SEPARATOR);
        }

        public void SaveData()
        {
            var data = new JObject();
            data.Add("screen_name", _screenName);
            data.Add("friends", _totalFriends);
            data.Add("followers", _totalFollowers);
            data.Add("timeline", _totalTweets);
            //data.Add("max_id", _maxId);
            //data.Add("since_id", _sinceId);

            using (var writer = new StreamWriter(_trackFilename, true))
            {
                writer.Write(data.ToString());
            }
        }

        private void AppendOnDocument(string filename, string content, char separator)
        {
            using (var writer = new StreamWriter(filename, true))
            {
                writer.Write(content);
                writer.Write(separator);
            }
        }

        private void AppendOnDocument(string filename, char separator, params string[] content)
        {
            using (var writer = new StreamWriter(filename, true))
            {
                foreach (var c in content)
                {
                    writer.Write(c);
                    writer.Write(separator);
                }
            }
        }
    }
}
