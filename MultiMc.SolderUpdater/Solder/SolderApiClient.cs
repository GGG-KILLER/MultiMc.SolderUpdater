using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using MultiMc.SolderUpdater.Solder.Responses;

namespace MultiMc.SolderUpdater.Solder
{
    public class SolderApiClient : HttpClient
    {
        public SolderApiClient ( Uri baseUri )
        {
            this.BaseAddress = new Uri ( baseUri.GetLeftPart ( UriPartial.Authority ) );
            this.DefaultRequestHeaders.UserAgent.Clear ( );
            this.DefaultRequestHeaders.UserAgent.Add ( new System.Net.Http.Headers.ProductInfoHeaderValue ( "MultiMc.SolderUpdater", "1.0.0" ) );
            this.DefaultRequestVersion = new Version ( 2, 0 );
        }

        public async Task<ApiInfo> GetApiInfoAsync ( ) =>
            await this.GetFromJsonAsync<ApiInfo> ( "/api" ).ConfigureAwait ( false );

        public async Task<ModInfo> GetModInfoAsync ( String modName )
        {
            if ( String.IsNullOrWhiteSpace ( modName ) )
                throw new ArgumentException ( "Mod name cannot be null, empty or composed of whitespaces.", nameof ( modName ) );

            return await this.GetFromJsonAsync<ModInfo> ( $"/api/mod/{modName}" ).ConfigureAwait ( false );
        }

        public async Task<ModVersion> GetModVersionAsync ( String name, String version )
        {
            if ( String.IsNullOrWhiteSpace ( name ) )
                throw new ArgumentException ( "Mod name cannot be null, empty or composed of whitespaces.", nameof ( name ) );
            if ( String.IsNullOrWhiteSpace ( version ) )
                throw new ArgumentException ( "Mod version cannot be null, empty or composed of whitespaces.", nameof ( version ) );

            return await this.GetFromJsonAsync<ModVersion> ( $"/api/mod/{name}/{version}" ).ConfigureAwait ( false );
        }

        public async Task<ModpackInfo> GetModpackInfoAsync ( String slug )
        {
            if ( String.IsNullOrWhiteSpace ( slug ) )
                throw new ArgumentException ( "Modpack slug cannot be null, empty or composed of whitespaces.", nameof ( slug ) );

            return await this.GetFromJsonAsync<ModpackInfo> ( $"/api/modpack/{slug}" ).ConfigureAwait ( false );
        }

        public async Task<ModpackBuild> GetModpackBuildAsync ( String slug, String build )
        {
            if ( String.IsNullOrWhiteSpace ( slug ) )
                throw new ArgumentException ( "Modpack slug cannot be null, empty or composed of whitespaces.", nameof ( slug ) );
            if ( String.IsNullOrWhiteSpace ( build ) )
                throw new ArgumentException ( "Modpack build cannot be null, empty or composed of whitespaces.", nameof ( build ) );

            return await this.GetFromJsonAsync<ModpackBuild> ( $"/api/modpack/{slug}/{build}" );
        }
    }
}