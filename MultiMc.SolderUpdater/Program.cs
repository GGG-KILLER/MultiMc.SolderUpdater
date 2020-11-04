using System;
using System.Collections.Immutable;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using MultiMc.SolderUpdater.Solder;
using MultiMc.SolderUpdater.Solder.Responses;
using Tsu.Timing;

namespace MultiMc.SolderUpdater
{
    internal static class Program
    {
        public static readonly Version UpdaterVersion = new Version ( "1.4.0" );
        private static readonly TimingLogger logger = new ConsoleTimingLogger ( );
        private static readonly JsonSerializerOptions jsonSerializerOptions = new JsonSerializerOptions
        {
            AllowTrailingCommas = true,
            Converters =
            {
                new VersionValueConverter ( )
            },
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };

        private static void PrintUsage ( ) =>
            logger.LogInformation ( $"Usage: MultiMc.SolderUpdater [solder url] [modpack slug] [instance path]" );

        private static void DeleteLocalMod ( LocalModState localModState )
        {
            using ( logger.BeginScope ( $"Deleting mod {localModState.Name}" ) )
            {
                // Sorts directories last then longer paths first so that inner stuff is deleted before outer stuff.
                foreach ( var file in localModState.Files.OrderByDescending ( f => ( File.GetAttributes ( f ) & FileAttributes.Directory ) == 0 )
                                                         .ThenByDescending ( f => f.Length ) )
                {
                    try
                    {
                        if ( ( File.GetAttributes ( file ) & FileAttributes.Directory ) != 0 )
                        {
                            Directory.Delete ( file );
                        }
                        else
                        {
                            File.Delete ( file );
                        }
                        logger.LogDebug ( $"Deleted {file}" );
                    }
                    catch ( Exception ex )
                    {
                        logger.LogError ( $"Failed to delete {file} ({ex.Message})." );
                    }
                }
            }
        }

        private static async Task DownloadMod (
            SolderApiClient client,
            LocalState? localState,
            String instancePath,
            ModVersion mod,
            ImmutableDictionary<String, LocalModState>.Builder localMods )
        {
            if ( localState.HasValue )
            {
                if ( localState.Value.LocalMods.TryGetValue ( mod.Name, out LocalModState localModState ) )
                {
                    if ( localModState.Version.Equals ( mod.Version, StringComparison.OrdinalIgnoreCase ) )
                    {
                        localMods.Add ( localModState.Name, localModState );
                        return;
                    }
                    else
                    {
                        DeleteLocalMod ( localModState );
                    }
                }
            }

            ImmutableArray<String>.Builder files = ImmutableArray.CreateBuilder<String> ( );
            using ( Stream stream = await client.GetStreamAsync ( mod.Url ).ConfigureAwait ( false ) )
            using ( var archive = new ZipArchive ( stream ) )
            {
                foreach ( ZipArchiveEntry entry in archive.Entries )
                    files.Add ( entry.FullName );

                archive.ExtractToDirectory ( instancePath, true );
            }
            localMods.Add ( mod.Name, new LocalModState ( mod.Name, mod.Version, files.ToImmutable ( ) ) );
        }

        private static async Task<Int32> Main ( String[] args )
        {
            if ( args.Length == 0 )
            {
                PrintUsage ( );
                return 0;
            }
            else if ( args.Length != 3 )
            {
                logger.LogError ( "Not enough arguments!" );
                logger.WriteLine ( "" );
                PrintUsage ( );
                return 1;
            }

            var client = new SolderApiClient ( new Uri ( args[0] ) );

            var modpackSlug = args[1].Trim ( );
            if ( String.IsNullOrWhiteSpace ( modpackSlug ) )
            {
                logger.LogError ( "Invalid slug." );
                return 1;
            }

            var instancePath = args[2];
            var localStateFile = Path.Combine ( instancePath, "solder-modpack.lock" );
            if ( !Directory.Exists ( instancePath ) )
            {
                logger.LogError ( "The instance path provided does not exist." );
                return 1;
            }
            var cwd = Environment.CurrentDirectory;
            Environment.CurrentDirectory = instancePath;

            try
            {
                try
                {
                    ApiInfo info;
                    using ( logger.BeginOperation ( "Obtaining API info" ) )
                    {
                        info = await client.GetApiInfoAsync ( )
                                           .ConfigureAwait ( false );
                    }

                    if ( !info.ApiName.Equals ( "TechnicSolder", StringComparison.OrdinalIgnoreCase ) )
                    {
                        logger.LogError ( $"Unsupported API ({info.ApiName})" );
                        return 1;
                    }
                    else if ( info.Version.Length < 2
                              || !Version.TryParse ( info.Version[1..], out Version version )
                              || version.Major > 0
                              || version.Minor > 7 )
                    {
                        logger.LogError ( $"Unsupported API version ({info.Version})" );
                        return 1;
                    }
                }
                catch ( Exception ex )
                {
                    logger.LogError ( "Unable to obtain the Solder API information." );
                    logger.LogError ( ex.ToString ( ) );
                    return 1;
                }

                ModpackInfo modpackInfo;
                LocalState? localState = null;
                try
                {

                    using ( logger.BeginOperation ( "Obtaining modpack info" ) )
                    {
                        modpackInfo = await client.GetModpackInfoAsync ( modpackSlug )
                                                  .ConfigureAwait ( false );
                    }

                    if ( File.Exists ( localStateFile ) )
                    {
                        using ( logger.BeginOperation ( "Reading state info" ) )
                        using ( FileStream stream = File.OpenRead ( localStateFile ) )
                        {
                            try
                            {
                                localState = await JsonSerializer.DeserializeAsync<LocalState> ( stream, jsonSerializerOptions )
                                                                 .ConfigureAwait ( false );
                            }
                            catch ( JsonException )
                            {
                                stream.Seek ( 0, SeekOrigin.Begin );
                                var builder = new StringBuilder ( );
                                using ( var reader = new StreamReader ( stream ) )
                                {
                                    String line;
                                    while ( !( line = await reader.ReadLineAsync ( )
                                                                  .ConfigureAwait ( false ) )
                                                                  .StartsWith ( "}", StringComparison.Ordinal ) )
                                    {
                                        builder.AppendLine ( line );
                                    }
                                    builder.AppendLine ( "}" );
                                }

                                var json = builder.ToString ( );
                                localState = JsonSerializer.Deserialize<LocalState> ( json, jsonSerializerOptions );
                            }
                        }

                        // Both v1.0.0 and v1.1.0 had a flaw where a local file could be missing if the
                        // mod got deleted but a file with the same name got added in another mod. And
                        // v1.2.0 had another bug where it just deleted all mod and config files after
                        // migrating but didn't re-download them. And v1.3.0 had an issue where it
                        // didn't sort longer paths first and ended up not deleting old versions of files
                        // or non-existing mods.
                        if ( localState.Value.UpdaterVersion < new Version ( "1.4.0" ) )
                        {
                            // Redownload all mods
                            using ( logger.BeginScope ( "Deleting all mods because of updater version update", false ) )
                            {
                                foreach ( LocalModState mod in localState.Value.LocalMods.Values )
                                {
                                    DeleteLocalMod ( mod );
                                }
                                localState = null;
                            }
                        }

                        if ( localState?.ModpackVersion.Equals ( modpackInfo.LatestBuild, StringComparison.OrdinalIgnoreCase ) is true )
                        {
                            logger.LogInformation ( "Modpack is up to date." );
                            return 0;
                        }
                    }
                }
                catch ( Exception ex )
                {
                    logger.LogError ( "Unable to get modpack information:" );
                    logger.LogError ( ex.ToString ( ) );
                    return 1;
                }

                ModpackBuild modpackBuild;
                try
                {
                    using ( logger.BeginOperation ( "Obtaining modpack build info" ) )
                    {
                        modpackBuild = await client.GetModpackBuildAsync ( modpackSlug, modpackInfo.LatestBuild )
                                                   .ConfigureAwait ( false );
                    }
                }
                catch ( Exception ex )
                {
                    logger.LogError ( "Unable to get modpack build:" );
                    logger.LogError ( ex.ToString ( ) );
                    return 1;
                }

                if ( localState.HasValue )
                {
                    foreach ( LocalModState localMod in localState.Value.LocalMods.Values )
                    {
                        if ( !modpackBuild.Mods.Any ( m => m.Name.Equals ( localMod.Name, StringComparison.OrdinalIgnoreCase ) ) )
                        {
                            logger.LogInformation ( $"Mod {localMod.Name} no longer exists." );
                            DeleteLocalMod ( localMod );
                        }
                    }
                }

                ImmutableDictionary<String, LocalModState>.Builder localMods = ImmutableDictionary.CreateBuilder<String, LocalModState> ( );
                using ( logger.BeginOperation ( "Updating mods" ) )
                {
                    await Task.WhenAll ( modpackBuild.Mods.Select ( mod => DownloadMod ( client, localState, instancePath, mod, localMods ) ) )
                              .ConfigureAwait ( false );
                }

                localState = new LocalState ( UpdaterVersion, modpackInfo.LatestBuild, localMods.ToImmutable ( ) );

                using ( logger.BeginOperation ( "Saving local state to file" ) )
                using ( FileStream stream = File.Open ( localStateFile, FileMode.Truncate, FileAccess.Write, FileShare.None ) )
                {
                    await JsonSerializer.SerializeAsync ( stream, localState.Value, jsonSerializerOptions )
                                        .ConfigureAwait ( false );
                }

                return 0;
            }
            finally
            {
                Environment.CurrentDirectory = cwd;
            }
        }
    }
}