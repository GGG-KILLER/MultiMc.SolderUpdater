using Newtonsoft.Json;
using System;

namespace MultiMc.SolderUpdater.Solder.Responses
{
    public readonly struct ModpackBuild
    {
        public String MinecraftVersion { get; }
        public String MinecraftMd5 { get; }
        public String ForgeVersion { get; }
        public ModVersion[] Mods { get; }

        [JsonConstructor]
        public ModpackBuild([JsonProperty("minecraft")] String minecraftVersion,
                            [JsonProperty("minecraft_md5")] String minecraftMd5,
                            [JsonProperty("forge")] String forgeVersion,
                            [JsonProperty("mods")] ModVersion[] mods)
        {
            this.MinecraftVersion = minecraftVersion;
            this.MinecraftMd5 = minecraftMd5;
            this.ForgeVersion = forgeVersion;
            this.Mods = mods;
        }
    }
}