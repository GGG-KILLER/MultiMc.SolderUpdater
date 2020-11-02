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
using Tsu.Numerics;
using Tsu.Timing;

namespace MultiMc.SolderUpdater
{
    internal static class Program
    {
        private static readonly Version updaterVersion = new Version ( "1.2.1" );
        private static readonly TimingLogger logger = new ConsoleTimingLogger ( );
        private static readonly Object _logLock = new Object ( );
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
                foreach ( var file in localModState.Files.OrderBy ( f => f.EndsWith ( '/' ) || f.EndsWith ( '\\' ) ) )
                {
                    try
                    {
                        if ( file.EndsWith ( '/' ) || file.EndsWith ( '\\' ) )
                            Directory.Delete ( file );
                        else
                            File.Delete ( file );
                        lock ( _logLock )
                            logger.LogDebug ( $"Deleted {file}" );
                    }
                    catch ( Exception ex )
                    {
                        lock ( _logLock )
                            logger.LogError ( $"Failed to delete {file} ({ex.Message})" );
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
            if ( mod.Name.Equals ( "_forge", StringComparison.OrdinalIgnoreCase ) )
            {
                lock ( _logLock )
                    logger.LogInformation ( "Skipping forge..." );
                return;
            }

            if ( localState.HasValue )
            {
                if ( localState.Value.LocalMods.TryGetValue ( mod.Name, out LocalModState localModState ) )
                {
                    if ( localModState.Version.Equals ( mod.Version, StringComparison.OrdinalIgnoreCase ) )
                    {
                        localMods.Add ( localModState.Name, localModState );
                        lock ( _logLock )
                            logger.LogInformation ( $"Mod {mod.Name} is up to date." );
                        return;
                    }
                    else
                    {
                        lock ( _logLock )
                            logger.LogInformation ( $"Mod {mod.Name} is out of date." );
                        DeleteLocalMod ( localModState );
                    }
                }
            }

            lock ( _logLock )
                logger.LogInformation ( $"Obtaining {mod.Name} stream..." );
            var ts = Stopwatch.GetTimestamp ( );
            Stream stream = await client.GetStreamAsync ( mod.Url ).ConfigureAwait ( false );
            var dts = Stopwatch.GetTimestamp ( ) - ts;
            lock ( _logLock )
                logger.LogInformation ( $"Obtained {mod.Name} stream in {Duration.Format ( dts )}." );

            lock ( _logLock )
                logger.LogInformation ( $"Reading {mod.Name} zip file..." );
            ts = Stopwatch.GetTimestamp ( );
            var archive = new ZipArchive ( stream );
            dts = Stopwatch.GetTimestamp ( ) - ts;
            lock ( _logLock )
                logger.LogInformation ( $"Read {mod.Name} zip file in {Duration.Format ( dts )}." );

            ImmutableArray<String>.Builder files = ImmutableArray.CreateBuilder<String> ( );
            using ( archive )
            {
                foreach ( ZipArchiveEntry entry in archive.Entries )
                    files.Add ( entry.FullName );

                lock ( _logLock )
                    logger.LogInformation ( $"Extracting {mod.Name} zip file..." );
                ts = Stopwatch.GetTimestamp ( );
                archive.ExtractToDirectory ( instancePath, true );
                dts = Stopwatch.GetTimestamp ( ) - ts;
                lock ( _logLock )
                    logger.LogInformation ( $"Extracted {mod.Name} zip file in {Duration.Format ( dts )}." );
            }
            localMods.Add ( mod.Name, new LocalModState ( mod.Name, mod.Version, files.ToImmutable ( ) ) );
        }

        private static async Task<Int32> Main ( String[] args )
        {
            if ( args.Length == 0 )
            {
                lock ( _logLock )
                    PrintUsage ( );
                return 0;
            }
            else if ( args.Length != 3 )
            {
                lock ( _logLock )
                {
                    logger.LogError ( "Not enough arguments!" );
                    logger.WriteLine ( "" );
                    PrintUsage ( );
                }
                return 1;
            }

            var client = new SolderApiClient ( new Uri ( args[0] ) );

            var modpackSlug = args[1].Trim ( );
            if ( String.IsNullOrWhiteSpace ( modpackSlug ) )
            {
                lock ( _logLock )
                    logger.LogError ( "Invalid slug." );
                return 1;
            }

            var instancePath = args[2];
            var localStateFile = Path.Combine ( instancePath, "solder-modpack.lock" );
            if ( !Directory.Exists ( instancePath ) )
            {
                lock ( _logLock )
                    logger.LogError ( "The instance path provided does not exist." );
                return 1;
            }

            try
            {
                ApiInfo info = await client.GetApiInfoAsync ( ).ConfigureAwait ( false );

                if ( !info.ApiName.Equals ( "TechnicSolder", StringComparison.OrdinalIgnoreCase ) )
                {
                    lock ( _logLock )
                        logger.LogError ( $"Unsupported API ({info.ApiName})" );
                    return 1;
                }
                else if ( info.Version.Length < 2
                          || !Version.TryParse ( info.Version[1..], out Version version )
                          || version.Major > 0
                          || version.Minor > 7 )
                {
                    lock ( _logLock )
                        logger.LogError ( $"Unsupported API version ({info.Version})" );
                    return 1;
                }
            }
            catch ( Exception ex )
            {
                lock ( _logLock )
                {
                    logger.LogError ( "Unable to obtain the Solder API information." );
                    logger.LogError ( ex.ToString ( ) );
                }
                return 1;
            }

            ModpackInfo modpackInfo;
            LocalState? localState = null;
            try
            {
                modpackInfo = await client.GetModpackInfoAsync ( modpackSlug ).ConfigureAwait ( false );

                if ( File.Exists ( localStateFile ) )
                {
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
                        lock ( _logLock )
                            logger.LogInformation ( "Modpack is up to date." );
                        return 0;
                    }
                }
            }
            catch ( Exception ex )
            {
                lock ( _logLock )
                {
                    logger.LogError ( "Unable to get modpack information:" );
                    logger.LogError ( ex.ToString ( ) );
                }
                return 1;
            }

            ModpackBuild modpackBuild;
            try
            {
                modpackBuild = await client.GetModpackBuildAsync ( modpackSlug, modpackInfo.LatestBuild )
                                           .ConfigureAwait ( false );
            }
            catch ( Exception ex )
            {
                lock ( _logLock )
                {
                    logger.LogError ( "Unable to get modpack build:" );
                    logger.LogError ( ex.ToString ( ) );
                }
                return 1;
            }

            if ( localState.HasValue )
            {
                foreach ( LocalModState localMod in localState.Value.LocalMods.Values )
                {
                    if ( !modpackBuild.Mods.Any ( m => m.Name.Equals ( localMod.Name, StringComparison.OrdinalIgnoreCase ) ) )
                    {
                        lock ( _logLock )
                            logger.LogInformation ( $"Mod {localMod.Name} no longer exists." );
                        DeleteLocalMod ( localMod );
                    }
                }
            }

            ImmutableDictionary<String, LocalModState>.Builder localMods = ImmutableDictionary.CreateBuilder<String, LocalModState> ( );
            using ( logger.BeginScope ( "Downloading mods" ) )
            {
                await Task.WhenAll ( modpackBuild.Mods.Select ( mod => DownloadMod ( client, localState, instancePath, mod, localMods ) ) )
                          .ConfigureAwait ( false );
            }

            localState = new LocalState ( updaterVersion, modpackInfo.LatestBuild, localMods.ToImmutable ( ) );
            using ( logger.BeginOperation ( "Saving version to file" ) )
            using ( FileStream stream = File.OpenWrite ( localStateFile ) )
            {
                await JsonSerializer.SerializeAsync ( stream, localState.Value, jsonSerializerOptions )
                                    .ConfigureAwait ( false );
            }

            return 0;
        }
    }
}