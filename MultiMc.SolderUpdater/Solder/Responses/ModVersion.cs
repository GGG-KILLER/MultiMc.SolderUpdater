using System;
using System.Text.Json.Serialization;

namespace MultiMc.SolderUpdater.Solder.Responses
{
    public readonly struct ModVersion
    {
        [JsonPropertyName ( "name" )]
        public String Name { get; }

        [JsonPropertyName ( "version" )]
        public String Version { get; }

        [JsonPropertyName ( "md5" )]
        public String Md5 { get; }

        [JsonPropertyName ( "url" )]
        public String Url { get; }

        [JsonConstructor]
        public ModVersion ( String md5, String url, String name = null, String version = null )
        {
            this.Name = name;
            this.Version = version;
            this.Md5 = md5;
            this.Url = url;
        }
    }
}