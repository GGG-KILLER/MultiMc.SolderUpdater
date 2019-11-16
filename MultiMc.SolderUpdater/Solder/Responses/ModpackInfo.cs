using Newtonsoft.Json;
using System;

namespace MultiMc.SolderUpdater.Solder.Responses
{
    public readonly struct ModpackInfo
    {
        public String Name { get; }
        public String DisplayName { get; }
        public String Url { get; }
        public String Icon { get; }
        public String IconMd5 { get; }
        public String Logo { get; }
        public String LogoMd5 { get; }
        public String Background { get; }
        public String BackgroundMd5 { get; }
        public String RecommendedBuild { get; }
        public String LatestBuild { get; }
        public String[] Builds { get; }

        public ModpackInfo([JsonProperty("name")] String name,
                           [JsonProperty("display_name")] String displayName,
                           [JsonProperty("url")] String url,
                           [JsonProperty("icon")] String icon,
                           [JsonProperty("icon_md5")] String iconMd5,
                           [JsonProperty("logo")] String logo,
                           [JsonProperty("logo_md5")] String logoMd5,
                           [JsonProperty("background")] String background,
                           [JsonProperty("background_md5")] String backgroundMd5,
                           [JsonProperty("recommended")] String recommendedBuild,
                           [JsonProperty("latest")] String latestBuild,
                           [JsonProperty("builds")] String[] builds)
        {
            this.Name = name ?? throw new ArgumentNullException(nameof(name));
            this.DisplayName = displayName ?? throw new ArgumentNullException(nameof(displayName));
            this.Url = url;
            this.Icon = icon ?? throw new ArgumentNullException(nameof(icon));
            this.IconMd5 = iconMd5;
            this.Logo = logo ?? throw new ArgumentNullException(nameof(logo));
            this.LogoMd5 = logoMd5;
            this.Background = background ?? throw new ArgumentNullException(nameof(background));
            this.BackgroundMd5 = backgroundMd5;
            this.RecommendedBuild = recommendedBuild ?? throw new ArgumentNullException(nameof(recommendedBuild));
            this.LatestBuild = latestBuild ?? throw new ArgumentNullException(nameof(latestBuild));
            this.Builds = builds ?? throw new ArgumentNullException(nameof(builds));
        }
    }
}