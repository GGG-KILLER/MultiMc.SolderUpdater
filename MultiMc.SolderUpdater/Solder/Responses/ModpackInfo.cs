using System;
using System.Collections.Immutable;
using System.Text.Json.Serialization;

namespace MultiMc.SolderUpdater.Solder.Responses
{
    public readonly struct ModpackInfo
    {
        [JsonPropertyName ( "name" )]
        public String Name { get; }

        [JsonPropertyName ( "display_name" )]
        public String DisplayName { get; }

        [JsonPropertyName ( "url" )]
        public String Url { get; }

        [JsonPropertyName ( "icon" )]
        public String Icon { get; }

        [JsonPropertyName ( "icon_md5" )]
        public String IconMd5 { get; }

        [JsonPropertyName ( "logo" )]
        public String Logo { get; }

        [JsonPropertyName ( "logo_md5" )]
        public String LogoMd5 { get; }

        [JsonPropertyName ( "background" )]
        public String Background { get; }

        [JsonPropertyName ( "background_md5" )]
        public String BackgroundMd5 { get; }

        [JsonPropertyName ( "recommended" )]
        public String RecommendedBuild { get; }

        [JsonPropertyName ( "latest" )]
        public String LatestBuild { get; }

        [JsonPropertyName ( "builds" )]
        public ImmutableArray<String> Builds { get; }

        [JsonConstructor]
        public ModpackInfo ( String name,
                             String displayName,
                             String url,
                             String icon,
                             String iconMd5,
                             String logo,
                             String logoMd5,
                             String background,
                             String backgroundMd5,
                             String recommendedBuild,
                             String latestBuild,
                             ImmutableArray<String> builds )
        {
            this.Name = name ?? throw new ArgumentNullException ( nameof ( name ) );
            this.DisplayName = displayName ?? throw new ArgumentNullException ( nameof ( displayName ) );
            this.Url = url;
            this.Icon = icon ?? throw new ArgumentNullException ( nameof ( icon ) );
            this.IconMd5 = iconMd5;
            this.Logo = logo ?? throw new ArgumentNullException ( nameof ( logo ) );
            this.LogoMd5 = logoMd5;
            this.Background = background ?? throw new ArgumentNullException ( nameof ( background ) );
            this.BackgroundMd5 = backgroundMd5;
            this.RecommendedBuild = recommendedBuild ?? throw new ArgumentNullException ( nameof ( recommendedBuild ) );
            this.LatestBuild = latestBuild ?? throw new ArgumentNullException ( nameof ( latestBuild ) );
            this.Builds = builds;
        }
    }
}