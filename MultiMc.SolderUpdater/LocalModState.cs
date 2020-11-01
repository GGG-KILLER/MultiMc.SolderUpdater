using System;
using System.Collections.Immutable;
using System.Text.Json.Serialization;

namespace MultiMc.SolderUpdater
{
    public readonly struct LocalModState
    {
        public String Name { get; }
        public String Version { get; }
        public ImmutableArray<String> Files { get; }

        [JsonConstructor]
        public LocalModState ( String name, String version, ImmutableArray<String> files )
        {
            this.Name = name;
            this.Version = version;
            this.Files = files;
        }
    }
}