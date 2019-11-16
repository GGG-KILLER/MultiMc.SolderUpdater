using System;
using System.Collections.Immutable;

namespace MultiMc.SolderUpdater
{
    public readonly struct LocalState
    {
        public String ModpackVersion { get; }
        public ImmutableDictionary<String, LocalModState> LocalMods { get; }

        public LocalState(String modpackVersion, ImmutableDictionary<String, LocalModState> localMods)
        {
            this.ModpackVersion = modpackVersion;
            this.LocalMods = localMods;
        }
    }
}