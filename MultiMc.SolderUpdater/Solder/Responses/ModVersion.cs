using Newtonsoft.Json;
using System;

namespace MultiMc.SolderUpdater.Solder.Responses
{
    public readonly struct ModVersion
    {
        public String Name { get; }
        public String Version { get; }
        public String Md5 { get; }
        public String Url { get; }

        [JsonConstructor]
        public ModVersion(String md5, String url, String name = null, String version = null)
        {
            this.Name = name;
            this.Version = version;
            this.Md5 = md5;
            this.Url = url;
        }
    }
}