using System;
using System.Text.Json.Serialization;

namespace MultiMc.SolderUpdater.Solder.Responses
{
    public readonly struct ModpackBuild
    {
        public String MinecraftVersion { get; }
        public String MinecraftMd5 { get; }
        public String ForgeVersion { get; }
        public ModVersion[] Mods { get; }

        [JsonConstructor]
        public ModpackBuild ( String minecraft, String minecraft_md5, String forge, ModVersion[] mods )
        {
            this.MinecraftVersion = minecraft;
            this.MinecraftMd5 = minecraft_md5;
            this.ForgeVersion = forge;
            this.Mods = mods;
        }
    }
}