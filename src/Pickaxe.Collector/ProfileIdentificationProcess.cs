using MagnetArgs;
using Newtonsoft.Json.Linq;
using QApp;
using QApp.Events;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Pickaxe.Collector
{
    class ProfileIdentificationOptions : MagnetOption
    {
        [Arg("--input-directory", Alias = "-input"), IsRequired]
        public string InputDirectories { get; set; }
        [Arg("--cache-directory", Alias = "-cache")]
        public string CacheDirectory { get; set; }
        [Arg("--ouput-directory", Alias = "-output")]
        public string OutputDirectory { get; set; }

        public bool SaveProfileOnCache
        {
            get
            {
                return (!string.IsNullOrEmpty(this.CacheDirectory));
            }
        }

        [Arg("--restore-identification", Alias = "-restore"), IfPresent]
        public bool RestoreCache { get; set; }
    }

    class ProfileIdentificationProcess : QTask
    {
        #region Events
        public event EventHandler<MessageEventArgs> DocumentRead;
        public event EventHandler<MessageEventArgs> DocumentFailed;
        private void OnDocumentRead(MessageEventArgs e)
        {
            if (DocumentRead != null)
                DocumentRead(this, e);
        }
        private void OnDocumentFailed(MessageEventArgs e)
        {
            if (DocumentFailed != null)
                DocumentFailed(this, e);
        }
        #endregion

        public IEnumerable<long> Profiles
        {
            get
            {
                return _profiles;
            }
        }

        private const char UNIT_SEPARATOR = '\u241F';
        private ProfileIdentificationOptions _options;

        private List<long> _pool;
        private IEnumerable<long> _profiles;

        private int _documents = 0;
        private int _failedDocuments = 0;
        private int _files = 0;
        private int _directories = 0;

        public ProfileIdentificationProcess(ProfileIdentificationOptions options)
        {
            _options = options;
            _pool = new List<long>();
            _profiles = new List<long>();
        }

        private void Initialize()
        {
            if (!Directory.Exists(_options.CacheDirectory))
                Directory.CreateDirectory(_options.CacheDirectory);
        }

        public override void Start()
        {
            try
            {
                this.OnStarted(new MessageEventArgs("The identification process is started.", MessageType.Info));

                if (_options.RestoreCache)
                {
                    // Process cache directory
                    this.OnProgress(new MessageEventArgs("Restore identifiers enabled.", MessageType.Warning));
                    this.LoadProfilesFromDirectoryCache(_options.CacheDirectory);
                }
                else
                {
                    // Process directories
                    var directories = _options.InputDirectories.Split(';');
                    foreach (var dir in directories)
                    {
                        _directories++;
                        this.ReadDirectory(dir);
                    }
                }

                // Show Statistics
                this.OnProgress(new MessageEventArgs(
                    MessageType.Resume,
                    MessagePriority.Medium,
                    "Directories: {0}, Files: {1}, Documents: {2}/{3}",
                    _directories, _files, _documents - _failedDocuments, _documents
                ));

                _profiles = _pool.Distinct();

                this.OnProgress(new MessageEventArgs(
                    MessageType.Resume,
                    MessagePriority.Medium,
                    "Identified Users: {0}",
                    _profiles.Count()
                ));

                if (_options.SaveProfileOnCache && !_options.RestoreCache)
                    this.SaveProfilesOnCache(_profiles);
                
                this.OnCompleted(new MessageEventArgs("The identification process is completed.", MessageType.Info));
            }
            catch (Exception ex)
            {
                this.OnFailed(new MessageEventArgs("The identification process failed with the message: " + ex.Message, MessageType.Error));
            }
        }

        private void ReadDirectory(string directoryPath)
        {
            if (Directory.Exists(directoryPath))
            {
                var info = new DirectoryInfo(directoryPath);
                var files = info.GetFiles().OrderBy(s => s.CreationTime).ToArray();

                for (int i = 0; i < files.Count(); i++)
                {
                    var file = files[i];

                    if (DateTime.Now.Subtract(file.LastAccessTime).TotalSeconds > 60)
                    {
                        this.ReadFile(file.FullName);
                        this.OnDocumentRead(new MessageEventArgs(MessageType.Info, MessagePriority.Medium, "File processed: {0}", file.FullName));
                    }
                }
            }
            else
                this.OnProgress(new MessageEventArgs(MessageType.Error, MessagePriority.Medium, "Directory Not Found: {0}", directoryPath));
        }

        private void ReadFile(string filename)
        {
            _files++;

            if (File.Exists(filename))
            {
                var cache = new StringBuilder();

                using (var reader = new StreamReader(filename))
                {
                    int s = -1;
                    do
                    {
                        s = reader.Read();

                        if (s == UNIT_SEPARATOR)
                        {
                            try
                            {
                                _documents++;

                                // Recover document
                                var document = cache.ToString();

                                // Get ID
                                var profileId = long.Parse(this.GetProfileId(document));

                                // Add to profile pool.
                                _pool.Add(profileId);
                            }
                            catch
                            {
                                _failedDocuments++;
                            }
                            finally
                            {
                                cache.Clear();
                            }
                        }
                        else
                        {
                            cache.Append((char)s);
                        }
                    } while (s != -1);
                }
            }
        }

        private string GetProfileId(string document)
        {
            var json = ParseJson(document);

            var id = GetId(json);

            return id;
        }

        private string GetId(JObject json)
        {
            return json.SelectToken("$.user.id").ToString();
        }

        private JObject ParseJson(string content)
        {
            return JObject.Parse(content);
        }

        private void SaveProfilesOnCache(IEnumerable<long> profiles)
        {
            var filename = DateTime.Now.ToString("yyyyMMddhhmmss") + ".list";
            var path = Path.Combine(_options.CacheDirectory, filename);

            using (var writer = new StreamWriter(path, false))
            {
                foreach (var p in profiles)
                {
                    writer.WriteLine(p);
                }
            }
        }

        private void LoadProfilesFromDirectoryCache(string directoryPath)
        {
            var dir = new DirectoryInfo(directoryPath);
            if (dir.Exists)
            {
                foreach (var file in dir.GetFiles("*.list"))
                {
                    this.LoadProfilesFromFileCache(file.FullName);
                }

                _directories++;
            }
            else
                this.OnProgress(new MessageEventArgs(MessageType.Error, MessagePriority.Medium, "Directory Not Found: {0}", directoryPath));
        }

        private void LoadProfilesFromFileCache(string filename)
        {
            using (var reader = new StreamReader(filename))
            {
                while (reader.Peek() >= 0)
                {
                    var line = reader.ReadLine();
                    if (!string.IsNullOrEmpty(line))
                    {
                        long id = -1;
                        if (long.TryParse(line, out id) && id != -1)
                        {
                            _pool.Add(id);
                        }
                    }
                }

                _documents++;

                this.OnDocumentRead(new MessageEventArgs(MessageType.Info, MessagePriority.Medium, "File processed: {0}", filename));
            }
        }
    }
}
