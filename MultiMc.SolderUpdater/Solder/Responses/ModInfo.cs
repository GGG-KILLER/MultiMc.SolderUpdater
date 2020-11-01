using System;
using System.Text.Json.Serialization;

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
        public ModInfo (
            String name, String pretty_name, String author, String description, String link, String donate,
            String[] versions )
        {
            if ( String.IsNullOrWhiteSpace ( name ) )
                throw new ArgumentException ( "Name cannot be null, empty or composed of whitespaces.", nameof ( name ) );
            if ( String.IsNullOrWhiteSpace ( pretty_name ) )
                throw new ArgumentException ( "Pretty name cannot be null, empty or composed of whitespaces.", nameof ( pretty_name ) );

            this.Name = name;
            this.PrettyName = pretty_name;
            this.Author = author;
            this.Description = description;
            this.Link = link;
            this.Donate = donate;
            this.Versions = versions ?? throw new ArgumentNullException ( nameof ( versions ) );
        }
    }
}