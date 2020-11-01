using System;
using System.Collections.Immutable;
using Newtonsoft.Json;

namespace MultiMc.SolderUpdater
{
    public readonly struct LocalState
    {
        public Version UpdaterVersion { get; }
        public String ModpackVersion { get; }
        public ImmutableDictionary<String, LocalModState> LocalMods { get; }

        [JsonConstructor]
        public LocalState ( Version updaterVersion, String modpackVersion, ImmutableDictionary<String, LocalModState> localMods )
        {
            this.UpdaterVersion = updaterVersion;
            this.ModpackVersion = modpackVersion;
            this.LocalMods = localMods;
        }
    }
}