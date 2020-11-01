using System;
using System.Text.Json.Serialization;

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

        [JsonConstructor]
        public ModpackInfo ( String name,
                             String display_name,
                             String url,
                             String icon,
                             String icon_md5,
                             String logo,
                             String logo_md5,
                             String background,
                             String background_md5,
                             String recommended,
                             String latest,
                             String[] builds )
        {
            this.Name = name ?? throw new ArgumentNullException ( nameof ( name ) );
            this.DisplayName = display_name ?? throw new ArgumentNullException ( nameof ( display_name ) );
            this.Url = url;
            this.Icon = icon ?? throw new ArgumentNullException ( nameof ( icon ) );
            this.IconMd5 = icon_md5;
            this.Logo = logo ?? throw new ArgumentNullException ( nameof ( logo ) );
            this.LogoMd5 = logo_md5;
            this.Background = background ?? throw new ArgumentNullException ( nameof ( background ) );
            this.BackgroundMd5 = background_md5;
            this.RecommendedBuild = recommended ?? throw new ArgumentNullException ( nameof ( recommended ) );
            this.LatestBuild = latest ?? throw new ArgumentNullException ( nameof ( latest ) );
            this.Builds = builds ?? throw new ArgumentNullException ( nameof ( builds ) );
        }
    }
}