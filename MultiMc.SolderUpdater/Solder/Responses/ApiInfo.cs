using System;
using System.Text.Json.Serialization;

namespace MultiMc.SolderUpdater.Solder.Responses
{
    public readonly struct ApiInfo
    {
        public String Api { get; }

        public String Version { get; }

        public String Stream { get; }

        [JsonConstructor]
        public ApiInfo ( String api, String version, String stream )
        {
            if ( String.IsNullOrEmpty ( api ) )
                throw new ArgumentException ( "The name cannot be null or empty!", nameof ( api ) );
            if ( String.IsNullOrEmpty ( version ) )
                throw new ArgumentException ( "The version cannot be null or empty!", nameof ( version ) );
            if ( String.IsNullOrEmpty ( stream ) )
                throw new ArgumentException ( "The stream cannot be null or empty!", nameof ( stream ) );

            this.Api = api;
            this.Version = version;
            this.Stream = stream;
        }
    }
}