using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using MultiMc.SolderUpdater.Solder;
using MultiMc.SolderUpdater.Solder.Responses;
using Tsu.Timing;

namespace MultiMc.SolderUpdater
{
    internal static class Program
    {
        public static readonly Version UpdaterVersion = new Version ( "1.3.0" );
        private static readonly TimingLogger logger = new ConsoleTimingLogger ( );
#if LOG_IN_DOWNLOAD
        private static readonly Object _logLock = new Object ( );
#endif
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
                        if ( file.EndsWith ( '/' ) || file.EndsWith ( '\\' ) )
                            Directory.Delete ( file );
                        else
                            File.Delete ( file );
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
//            if ( mod.Name.Equals ( "_forge", StringComparison.OrdinalIgnoreCase ) )
//            {
//#if LOG_IN_DOWNLOAD
//                lock ( _logLock )
//                    logger.LogInformation ( "Skipping forge..." );
//#endif
//                return;
//            }

            if ( localState.HasValue )
            {
                if ( localState.Value.LocalMods.TryGetValue ( mod.Name, out LocalModState localModState ) )
                {
                    if ( localModState.Version.Equals ( mod.Version, StringComparison.OrdinalIgnoreCase ) )
                    {
                        localMods.Add ( localModState.Name, localModState );
#if LOG_IN_DOWNLOAD
                        lock ( _logLock )
                            logger.LogInformation ( $"Mod {mod.Name} is up to date." );
#endif
                        return;
                    }
                    else
                    {
#if LOG_IN_DOWNLOAD
                        lock ( _logLock )
                            logger.LogInformation ( $"Mod {mod.Name} is out of date." );
#endif
                        DeleteLocalMod ( localModState );
                    }
                }
            }

#if LOG_IN_DOWNLOAD
            lock ( _logLock )
                logger.LogInformation ( $"Obtaining {mod.Name} stream..." );
            var ts = Stopwatch.GetTimestamp ( );
#endif
            Stream stream = await client.GetStreamAsync ( mod.Url ).ConfigureAwait ( false );
#if LOG_IN_DOWNLOAD
            var dts = Stopwatch.GetTimestamp ( ) - ts;
            lock ( _logLock )
                logger.LogInformation ( $"Obtained {mod.Name} stream in {Duration.Format ( dts )}." );

            lock ( _logLock )
                logger.LogInformation ( $"Reading {mod.Name} zip file..." );
            ts = Stopwatch.GetTimestamp ( );
#endif
            var archive = new ZipArchive ( stream );
#if LOG_IN_DOWNLOAD
            dts = Stopwatch.GetTimestamp ( ) - ts;
            lock ( _logLock )
                logger.LogInformation ( $"Read {mod.Name} zip file in {Duration.Format ( dts )}." );
#endif

            ImmutableArray<String>.Builder files = ImmutableArray.CreateBuilder<String> ( );
            using ( archive )
            {
                foreach ( ZipArchiveEntry entry in archive.Entries )
                    files.Add ( entry.FullName );

#if LOG_IN_DOWNLOAD
                lock ( _logLock )
                    logger.LogInformation ( $"Extracting {mod.Name} zip file..." );
                ts = Stopwatch.GetTimestamp ( );
#endif
                archive.ExtractToDirectory ( instancePath, true );
#if LOG_IN_DOWNLOAD
                dts = Stopwatch.GetTimestamp ( ) - ts;
                lock ( _logLock )
                    logger.LogInformation ( $"Extracted {mod.Name} zip file in {Duration.Format ( dts )}." );
#endif
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
                        localState = await JsonSerializer.DeserializeAsync<LocalState> ( stream, jsonSerializerOptions )
                                                         .ConfigureAwait ( false );
                    }

                    // Both v1.0.0 and v1.1.0 had a flaw where a local file could be missing if the
                    // mod got deleted but a file with the same name got added in another mod. And
                    // v1.2.0 had another bug where it just deleted all mod and config files after
                    // migrating but didn't re-download them.
                    if ( localState.Value.UpdaterVersion < new Version ( "1.2.1" ) )
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
#if LOG_IN_DOWNLOAD
            using ( logger.BeginScope ( "Downloading mods" ) )
#else
            using ( logger.BeginOperation ( "Updating mods" ) )
#endif
            {
                await Task.WhenAll ( modpackBuild.Mods.Select ( mod => DownloadMod ( client, localState, instancePath, mod, localMods ) ) )
                          .ConfigureAwait ( false );
            }

            localState = new LocalState ( UpdaterVersion, modpackInfo.LatestBuild, localMods.ToImmutable ( ) );
            using ( logger.BeginOperation ( "Saving local state to file" ) )
            using ( FileStream stream = File.OpenWrite ( localStateFile ) )
            {
                await JsonSerializer.SerializeAsync ( stream, localState.Value, jsonSerializerOptions )
                                    .ConfigureAwait ( false );
            }

            return 0;
        }
    }
}