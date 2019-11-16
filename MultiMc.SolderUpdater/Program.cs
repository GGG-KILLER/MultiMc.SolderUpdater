using MultiMc.SolderUpdater.Solder;
using MultiMc.SolderUpdater.Solder.Responses;
using GUtils.Timing;
using Newtonsoft.Json;
using System;
using System.Collections.Immutable;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;

namespace MultiMc.SolderUpdater
{
    internal static class Program
    {
        private static readonly Version updaterVersion = new Version(1, 1, 0);
        private static readonly TimingLogger logger;

        static Program()
        {
            logger = new ConsoleTimingLogger();
        }

        private static void PrintUsage() =>
            logger.LogInformation($"Usage: {Path.GetFileName(typeof(Program).Assembly.Location)} [solder url] [modpack slug] [instance path]");

        private static void DeleteLocalMod(LocalModState localModState)
        {
            using (logger.BeginScope($"Deleting mod {localModState.Name}"))
            {
                foreach (var file in localModState.Files)
                {
                    try
                    {
                        File.Delete(file);
                        logger.LogDebug($"Deleted {file}");
                    }
                    catch
                    {
                        logger.LogError($"Failed to delete {file}");
                    }
                }
            }
        }

        private static async Task<Int32> Main(String[] args)
        {
            if (args.Length == 0)
            {
                PrintUsage();
                return 0;
            }
            else if (args.Length != 3)
            {
                logger.LogError("Not enough arguments!");
                logger.WriteLine("");
                PrintUsage();
                return 1;
            }

            var client = new SolderApiClient(new Uri(args[0]));

            var modpackSlug = args[1].Trim();
            if (String.IsNullOrWhiteSpace(modpackSlug))
            {
                logger.LogError("Invalid slug.");
                return 1;
            }

            var instancePath = args[2];
            var localCopyStateFile = Path.Combine(instancePath, "solder-modpack.lock");
            if (!Directory.Exists(instancePath))
            {
                logger.LogError("The instance path provided does not exist.");
                return 1;
            }

            try
            {
                ApiInfo info = await client.GetApiInfoAsync().ConfigureAwait(false);

                if (!info.Name.Equals("TechnicSolder", StringComparison.OrdinalIgnoreCase))
                {
                    logger.LogError($"Unsupported API ({info.Name})");
                    return 1;
                }
                else if (info.Version.Length < 2 || !Version.TryParse(info.Version.Substring(1), out Version version) || version.Major > 0 || version.Minor > 7)
                {
                    logger.LogError($"Unsupported API version ({info.Version})");
                    return 1;
                }
            }
            catch (Exception ex)
            {
                logger.LogError("Unable to obtain the Solder API information.");
                logger.LogError(ex.ToString());
                return 1;
            }

            ModpackInfo modpackInfo;
            LocalState? localState = null;
            try
            {
                modpackInfo = await client.GetModpackInfoAsync(modpackSlug).ConfigureAwait(false);

                if (File.Exists(localCopyStateFile))
                {
                    using (var reader = new StreamReader(localCopyStateFile))
                    using (var jreader = new JsonTextReader(reader))
                        localState = new JsonSerializer().Deserialize<LocalState>(jreader);

                    if (localState.Value.UpdaterVersion >= updaterVersion && localState.Value.ModpackVersion.Equals(modpackInfo.LatestBuild, StringComparison.OrdinalIgnoreCase))
                    {
                        logger.LogInformation("Modpack is up to date.");
                        return 0;
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError("Unable to get modpack information:");
                logger.LogError(ex.ToString());
                return 1;
            }

            ModpackBuild modpackBuild;
            try
            {
                modpackBuild = await client.GetModpackBuildAsync(modpackSlug, modpackInfo.LatestBuild).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logger.LogError("Unable to get modpack build:");
                logger.LogError(ex.ToString());
                return 1;
            }

            ImmutableDictionary<String, LocalModState>.Builder localMods = ImmutableDictionary.CreateBuilder<String, LocalModState>();
            foreach (ModVersion mod in modpackBuild.Mods)
            {
                if (mod.Name.Equals("_forge", StringComparison.OrdinalIgnoreCase))
                {
                    logger.LogInformation("Skipping forge...");
                }

                if (localState.HasValue)
                {
                    if (localState.Value.LocalMods.TryGetValue(mod.Name, out LocalModState localModState))
                    {
                        if (localModState.Version.Equals(mod.Version, StringComparison.OrdinalIgnoreCase))
                        {
                            localMods.Add(localModState.Name, localModState);
                            logger.LogInformation($"Mod {mod.Name} is up to date.");
                            continue;
                        }
                        else
                        {
                            logger.LogInformation($"Mod {mod.Name} is out of date.");
                            DeleteLocalMod(localModState);
                        }
                    }
                }

                ImmutableArray<String>.Builder files = ImmutableArray.CreateBuilder<String>();
                using (logger.BeginScope($"Downloading {mod.Name}"))
                {
                    Stream stream;
                    using (logger.BeginOperation("Obtaining stream"))
                        stream = await client.GetStreamAsync(mod.Url);

                    ZipArchive archive;
                    using (logger.BeginOperation("Reading zip file"))
                        archive = new ZipArchive(stream);

                    using (archive)
                    {
                        foreach (ZipArchiveEntry entry in archive.Entries)
                            files.Add(entry.FullName);

                        using (logger.BeginOperation("Extracting zip file"))
                            archive.ExtractToDirectory(instancePath, true);
                    }
                }

                localMods.Add(mod.Name, new LocalModState(mod.Name, mod.Version, files.ToImmutable()));
            }

            if (localState.HasValue)
            {
                foreach (LocalModState localMod in localState.Value.LocalMods.Values)
                {
                    if (!localMods.ContainsKey(localMod.Name))
                    {
                        logger.LogInformation($"Mod {localMod.Name} no longer exists.");
                        DeleteLocalMod(localMod);
                    }
                }
            }

            localState = new LocalState(updaterVersion, modpackInfo.LatestBuild, localMods.ToImmutable());
            using (logger.BeginOperation("Saving version to file"))
                File.WriteAllText(localCopyStateFile, JsonConvert.SerializeObject(localState, Formatting.Indented));

            return 0;
        }
    }
}