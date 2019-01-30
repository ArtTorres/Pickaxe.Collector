using MagnetArgs;

namespace Pickaxe.Collector
{
    class CollectorOptions : MagnetOption
    {
        [Arg("--ouput-directory", Alias = "-output"), IsRequired]
        public string OutputDirectory { get; set; }

        [Arg("--cache-directory", Alias = "-cache"), IsRequired]
        public string CacheDirectory { get; set; }

        [Arg("--max-timeline", Alias = "-timeline"), IsRequired]
        public int MaxTweetsByTimeline { get; set; }

        [Arg("--resource-config", Alias = "-config"), IsRequired]
        public string ResourceConfigFile { get; set; }

        [Arg("--default-config", Alias = "-default"), IfPresent]
        public bool DefaultConfigFile { get; set; }

        [Arg("--discovery-mode", Alias = "-discovery"), IfPresent]
        public bool AllowDiscovery { get; set; }

        [Arg("--rebuild-collector", Alias = "-rebuild"), IfPresent]
        public bool RebuildCache { get; set; }

        [Arg("--no-profile", Alias = "-nopf"), IfPresent]
        public bool IgnoreUserProfile { get; set; }

        [Arg("--no-timeline", Alias = "-notl"), IfPresent]
        public bool IgnoreTimeline { get; set; }

        [Arg("--no-friends", Alias = "-nofd"), IfPresent]
        public bool IgnoreFriends { get; set; }

        [Arg("--no-followers", Alias = "-nofw"), IfPresent]
        public bool IgnoreFollowers { get; set; }

        [Arg("--include-retweets", Alias = "-rts"), IfPresent]
        public bool IncludeRetweets { get; set; }
    }
}
