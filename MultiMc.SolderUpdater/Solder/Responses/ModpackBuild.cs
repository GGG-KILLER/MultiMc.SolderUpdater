using System;
using System.Collections.Immutable;
using System.Text.Json.Serialization;

namespace MultiMc.SolderUpdater.Solder.Responses
{
    public readonly struct ModpackBuild
    {
        [JsonPropertyName ( "minecraft" )]
        public String MinecraftVersion { get; }

        [JsonPropertyName ( "minecraft_md5" )]
        public String MinecraftMd5 { get; }

        [JsonPropertyName ( "forge" )]
        public String ForgeVersion { get; }

        [JsonPropertyName ( "mods" )]
        public ImmutableArray<ModVersion> Mods { get; }

        [JsonConstructor]
        public ModpackBuild ( String minecraftVersion, String minecraftMd5, String forgeVersion, ImmutableArray<ModVersion> mods )
        {
            this.MinecraftVersion = minecraftVersion;
            this.MinecraftMd5 = minecraftMd5;
            this.ForgeVersion = forgeVersion;
            this.Mods = mods;
        }
    }
}