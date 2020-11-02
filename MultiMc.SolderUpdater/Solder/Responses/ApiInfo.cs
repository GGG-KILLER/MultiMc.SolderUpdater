using System;
using System.Text.Json.Serialization;

namespace MultiMc.SolderUpdater.Solder.Responses
{
    public readonly struct ApiInfo
    {
        [JsonPropertyName ( "api" )]
        public String ApiName { get; }

        [JsonPropertyName ( "version" )]
        public String Version { get; }

        [JsonPropertyName ( "stream" )]
        public String Stream { get; }

        [JsonConstructor]
        public ApiInfo ( String apiName, String version, String stream )
        {
            if ( String.IsNullOrEmpty ( apiName ) )
                throw new ArgumentException ( "The name cannot be null or empty!", nameof ( apiName ) );
            if ( String.IsNullOrEmpty ( version ) )
                throw new ArgumentException ( "The version cannot be null or empty!", nameof ( version ) );
            if ( String.IsNullOrEmpty ( stream ) )
                throw new ArgumentException ( "The stream cannot be null or empty!", nameof ( stream ) );

            this.ApiName = apiName;
            this.Version = version;
            this.Stream = stream;
        }
    }
}