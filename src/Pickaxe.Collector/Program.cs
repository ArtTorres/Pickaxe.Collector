using MagnetArgs;
using Newtonsoft.Json.Linq;
using Pickaxe.Collector.Controller;
using QApp;
using QApp.Events;
using QApp.Options;
using System.IO;

namespace Pickaxe.Collector
{
    class ApplicationOptions : MagnetOption
    {
        [Arg("--identification-only", Alias = "-identify"), IfPresent]
        public bool IdentificationOnly { get; set; }

        [Arg("--collector-only", Alias = "-collect"), IfPresent]
        public bool CollectorOnly { get; set; }
    }

    class Application : QApplication
    {
        [OptionSet]
        private ApplicationOptions ApplicationOptions { get; set; }

        [OptionSet]
        private ProfileIdentificationOptions IdentificationOptions { get; set; }

        [OptionSet]
        private CollectorOptions CollectorOptions { get; set; }

        public override void ExecutionProcess()
        {
            // Re-create default configuration file
            if (CollectorOptions.DefaultConfigFile && !string.IsNullOrEmpty(CollectorOptions.ResourceConfigFile))
            {
                CollectorProcess.SaveRequestConfiguration(
                    new RequestMonitorOptions[]{
                        new RequestMonitorOptions(){
                            ConnectionAccount=new Model.Account()
                        }
                    },
                    CollectorOptions.ResourceConfigFile
                );

                return;
            }

            // Identification process
            var identification = new ProfileIdentificationProcess(IdentificationOptions);
            this.MonitorTask(identification);
            identification.DocumentRead += process_ProcessProgress;
            identification.DocumentFailed += process_ProcessProgress;


            // Collector process
            var collector = new CollectorProcess(CollectorOptions);
            this.MonitorTask(collector);


            if (!ApplicationOptions.CollectorOnly)
            {
                identification.Start();
            }

            if (CollectorOptions.RebuildCache)
            {
                collector.RebuildCache();
            }

            if (!ApplicationOptions.IdentificationOnly)
            {
                collector.RestoreCache();

                if (!ApplicationOptions.CollectorOnly)
                    collector.AddToPool(identification.Profiles);

                collector.Start();
            }
        }

        void process_ProcessProgress(object sender, MessageEventArgs e)
        {
            this.Print(e.Message);
        }

        void process_ProcessFailed(object sender, MessageEventArgs e)
        {
            this.Print(e.Message);
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            var application = new Application();
            application.Execute(args);
        }

        static RequestMonitorOptions[] LoadRequestConfiguration(string filename)
        {
            RequestMonitorOptions[] output = null;

            using (var reader = new StreamReader(filename))
            {
                output = JArray.Parse(reader.ReadToEnd()).ToObject<RequestMonitorOptions[]>();
            }

            return output;
        }

        //static void SaveRequestConfiguration(RequestMonitorOptions[] options)
        //{
        //    var o = JArray.FromObject(options);

        //    using (var writer = new StreamWriter(@"F:\TEST 6\config.json"))
        //    {
        //        writer.Write(o.ToString());
        //    }
        //}
    }
}
