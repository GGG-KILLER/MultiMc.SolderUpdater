using Newtonsoft.Json;
using System;

namespace MultiMc.SolderUpdater.Solder.Responses
{
    public readonly struct ApiInfo
    {
        public String Name { get; }

        public String Version { get; }

        public String Stream { get; }

        [JsonConstructor]
        public ApiInfo([JsonProperty("api")] String name, [JsonProperty("version")] String version, [JsonProperty("stream")] String stream)
        {
            if (String.IsNullOrEmpty(name))
                throw new ArgumentException("The name cannot be null or empty!", nameof(name));
            if (String.IsNullOrEmpty(version))
                throw new ArgumentException("The version cannot be null or empty!", nameof(version));
            if (String.IsNullOrEmpty(stream))
                throw new ArgumentException("The stream cannot be null or empty!", nameof(stream));

            this.Name = name;
            this.Version = version;
            this.Stream = stream;
        }
    }
}