using QApp;
using QApp.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pickaxe.Collector
{
    class CollectorOptions : QOption
    {
        [Option("--ouput-directory", Alias = "-output", IsRequired = true)]
        public string OutputDirectory { get; set; }

        [Option("--cache-directory", Alias = "-cache", IsRequired = true)]
        public string CacheDirectory { get; set; }

        [Option("--max-timeline", Alias = "-timeline", IsRequired = true)]
        public int MaxTweetsByTimeline { get; set; }

        [Option("--resource-config", Alias = "-config", IsRequired = true)]
        public string ResourceConfigFile { get; set; }

        [Option("--default-config", Alias = "-default", IfPresent = true)]
        public bool DefaultConfigFile { get; set; }

        [Option("--discovery-mode", Alias = "-discovery", IfPresent = true)]
        public bool AllowDiscovery { get; set; }

        [Option("--rebuild-collector", Alias = "-rebuild", IfPresent = true)]
        public bool RebuildCache { get; set; }

        [Option("--no-profile", Alias = "-nopf", IfPresent = true)]
        public bool IgnoreUserProfile { get; set; }

        [Option("--no-timeline", Alias = "-notl", IfPresent = true)]
        public bool IgnoreTimeline { get; set; }

        [Option("--no-friends", Alias = "-nofd", IfPresent = true)]
        public bool IgnoreFriends { get; set; }

        [Option("--no-followers", Alias = "-nofw", IfPresent = true)]
        public bool IgnoreFollowers { get; set; }

        [Option("--include-retweets", Alias = "-rts", IfPresent = true)]
        public bool IncludeRetweets { get; set; }
    }
}
