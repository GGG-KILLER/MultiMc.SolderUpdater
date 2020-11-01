using System;
using Newtonsoft.Json;

namespace MultiMc.SolderUpdater.Solder.Responses
{
    public readonly struct ModInfo
    {
        public String Name { get; }
        public String PrettyName { get; }
        public String Author { get; }
        public String Description { get; }
        public String Link { get; }
        public String Donate { get; }
        public String[] Versions { get; }

        [JsonConstructor]
        public ModInfo ( [JsonProperty ( "name" )] String name,
                         [JsonProperty ( "pretty_name" )] String prettyName,
                         [JsonProperty ( "author" )] String author,
                         [JsonProperty ( "description" )] String description,
                         [JsonProperty ( "link" )] String link,
                         [JsonProperty ( "donate" )] String donate,
                         [JsonProperty ( "versions" )] String[] versions )
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
            this.Versions = versions ?? throw new ArgumentNullException ( nameof ( versions ) );
        }
    }
}