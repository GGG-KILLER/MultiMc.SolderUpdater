using System;
using System.Collections.Immutable;
using System.Text.Json.Serialization;

namespace MultiMc.SolderUpdater.Solder.Responses
{
    public readonly struct ModInfo
    {
        [JsonPropertyName ( "name" )]
        public String Name { get; }

        [JsonPropertyName ( "pretty_name" )]
        public String PrettyName { get; }

        [JsonPropertyName ( "author" )]
        public String Author { get; }

        [JsonPropertyName ( "description" )]
        public String Description { get; }

        [JsonPropertyName ( "link" )]
        public String Link { get; }

        [JsonPropertyName ( "link" )]
        public String Donate { get; }

        [JsonPropertyName ( "versions" )]
        public ImmutableArray<String> Versions { get; }

        [JsonConstructor]
        public ModInfo (
            String name, String prettyName, String author, String description, String link, String donate,
            ImmutableArray<String> versions )
        {
            if ( String.IsNullOrWhiteSpace ( name ) )
                throw new ArgumentException ( "Name cannot be null, empty or composed of whitespaces.", nameof ( name ) );
            if ( String.IsNullOrWhiteSpace ( prettyName ) )
                throw new ArgumentException ( "Pretty name cannot be null, empty or composed of whitespaces.", nameof ( prettyName ) );

            this.Name = name;
            this.PrettyName = prettyName;
            this.Author = author;
            this.Description = description;
            this.Link = link;
            this.Donate = donate;
            this.Versions = versions;
        }
    }
}