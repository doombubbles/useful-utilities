using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BTD_Mod_Helper;
using BTD_Mod_Helper.Api;
using BTD_Mod_Helper.Api.Audio;
using BTD_Mod_Helper.Api.Enums;
using BTD_Mod_Helper.Api.Internal;
using BTD_Mod_Helper.Api.ModOptions;
using Il2CppNinjaKiwi.Localization;
using MelonLoader.Utils;
using UnityEngine;
using TaskScheduler = BTD_Mod_Helper.Api.TaskScheduler;

// ReSharper disable IteratorMethodResultIsIgnored

namespace UsefulUtilities.Utilities;

public class JukeboxFolder : UsefulUtility
{
    public static readonly ModSettingFolder FolderPath =
        new(Path.Combine(MelonEnvironment.GameRootDirectory, "Jukebox"))
        {
            displayName = "Jukebox Folder",
            icon = VanillaSprites.JukeboxIcon,
            category = UsefulUtilitiesMod.Jukebox,
            description =
                "Audio files you put in this folder will be automatically loaded into the BTD6 Jukebox. " +
                "Can add new tracks from files without restarting the game, but can't delete them.",
            onSave = newPath =>
            {
                if (Path.GetFullPath(newPath) == Path.GetFullPath(watcher!.Path)) return;
                watcher.Path = newPath;
                TaskScheduler.ScheduleTask(() =>
                {
                    var tracks = CreateTracks(newPath);
                    AddTracks(tracks);
                    LoadTracks(tracks);
                    RegisterTracks(tracks);
                });
            }
        };

    private static FileSystemWatcher watcher = null!;

    public static readonly ModSettingBool LoadAsynchronously = new(true)
    {
        description = "Whether to load in tracks asynchronously on a separate thread or directly on the main thread",
        icon = VanillaSprites.LoadingWheel,
        category = UsefulUtilitiesMod.Jukebox,
    };

    public static readonly ModSettingBool NormalizeVolume = new(true)
    {
        description =
            "Normalizes the volume of jukebox tracks to be as load as they can be without peaking, as normal BTD6 music tends to be",
        icon = VanillaSprites.VolumeIcon,
        category = UsefulUtilitiesMod.Jukebox,
    };

    public static Task? LoadTask { get; private set; }

    public override IEnumerable<ModContent> Load()
    {
        var result = base.Load();

        if (!Directory.Exists(FolderPath)) Directory.CreateDirectory(FolderPath);

        watcher = new FileSystemWatcher(FolderPath);
        foreach (var extension in ResourceHandler.AudioExtensions)
        {
            watcher.Filters.Add("*" + extension);
        }
        watcher.IncludeSubdirectories = true;
        watcher.Created += (_, args) => TaskScheduler.ScheduleTask(() =>
        {
            var track = new FileJukeboxTrack(args.FullPath);
            AddTracks(track);
            LoadTracks(track);
            RegisterTracks(track);
        }, ScheduleType.WaitForSeconds, 1);

        var tracks = CreateTracks(FolderPath);

        if (LoadAsynchronously)
        {
            LoadTask = Task.Run(() =>
            {
                LoadTracks(tracks);
            });
        }

        return result.Concat(tracks);
    }

    public static IEnumerable<string> GetFiles(string path) => ResourceHandler.AudioExtensions
        .SelectMany(extension => Directory.EnumerateFiles(path, "*" + extension, SearchOption.AllDirectories));

    public static FileJukeboxTrack[] CreateTracks(string folderPath) =>
        GetFiles(folderPath).Select(file => new FileJukeboxTrack(file)).ToArray();

    public static void AddTracks(params IEnumerable<FileJukeboxTrack> tracks)
    {
        GetInstance<JukeboxFolder>().mod.AddContent(tracks);
    }

    public static void LoadTracks(params IEnumerable<FileJukeboxTrack> tracks)
    {
        foreach (var fileJukeboxTrack in tracks)
        {
            fileJukeboxTrack.LoadTrack();
        }
    }

    public static void RegisterTracks(params IEnumerable<FileJukeboxTrack> tracks)
    {
        foreach (var track in tracks.Where(track => !track.Registered))
        {
            track.Register();
            track.RegisterText(LocalizationManager.Instance.textTable);
        }
    }

    public class LoadJukeboxTracks : ModLoadTask
    {
        public override bool ShouldRun => LoadTask is { IsCompleted: false };

        public override bool ShowProgressBar => true;

        public override string DisplayName => "Loading Jukebox Tracks...";

        public override IEnumerator Coroutine()
        {
            var tracks = GetContent<FileJukeboxTrack>();

            while (ShouldRun)
            {
                yield return null;

                Progress = tracks.Count(track => track.Complete) / (float) tracks.Count;
            }

            RegisterTracks(tracks);
        }
    }

    public class FileJukeboxTrack : ModJukeboxTrack
    {
        public sealed override string Name { get; }
        public override AudioClip? AudioClip => audioClip;

        public override string DisplayName => Name;

        private AudioClip? audioClip;

        public override int RegisterPerFrame => 1;

        public string FilePath { get; }
        public bool Complete { get; private set; }
        public bool Registered { get; private set; }

        public FileJukeboxTrack(string filePath)
        {
            Name = Path.GetFileNameWithoutExtension(filePath);
            FilePath = filePath;
            mod = GetInstance<UsefulUtilitiesMod>();

            ModHelper.Msg<UsefulUtilitiesMod>($"Adding track \"{Name}\" from {FilePath}");
        }

        public override void Register()
        {
            if (!Complete && !LoadAsynchronously)
            {
                LoadTrack();
            }

            if (Complete && audioClip != null)
            {
                base.Register();
                Registered = true;
            }
        }

        public void LoadTrack()
        {
            if (Complete) return;

            try
            {
                var start = DateTime.Now;
                using var waveStream = ResourceHandler.GetWaveStream(FilePath);
                if (NormalizeVolume) BloonsMod.NormalizeAudioVolume.Add(Id);
                audioClip = ResourceHandler.CreateAudioClip(waveStream, Id);
                var end = DateTime.Now;

                if (audioClip != null)
                {
                    ModHelper.Msg<UsefulUtilitiesMod>(
                        $"Successfully processed track {Name} duration {TimeSpan.FromSeconds(audioClip.length):g} in {(end - start).TotalSeconds:N1}s");
                    return;
                }
            }
            catch (Exception e)
            {
                ModHelper.Error<UsefulUtilitiesMod>(e);
            }
            finally
            {
                Complete = true;
            }

            ModHelper.Error<UsefulUtilitiesMod>($"Unable to parse potential jukebox track file {FilePath}");
        }
    }
}