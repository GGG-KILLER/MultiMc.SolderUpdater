using System;
using System.Collections.Immutable;

namespace MultiMc.SolderUpdater
{
    public readonly struct LocalModState
    {
        public String Name { get; }
        public String Version { get; }
        public ImmutableArray<String> Files { get; }

        public LocalModState ( String name, String version, ImmutableArray<String> files )
        {
            this.Name = name;
            this.Version = version;
            this.Files = files;
        }
    }
}