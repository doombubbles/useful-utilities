using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BTD_Mod_Helper;
using BTD_Mod_Helper.Api.Audio;
using BTD_Mod_Helper.Api.Enums;
using BTD_Mod_Helper.Api.Internal;
using BTD_Mod_Helper.Api.ModOptions;
using Il2CppNinjaKiwi.Common;
using MelonLoader.Utils;
using NAudio.Wave;
using UnityEngine;
using TaskScheduler = BTD_Mod_Helper.Api.TaskScheduler;

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
                "Any .mp3 or .wav files you put in this folder will be automatically loaded into the BTD6 Jukebox. " +
                "Can add new tracks from files without restarting the game, but can't delete them.",
            onSave = newPath =>
            {
                watcher!.Path = newPath;
                TaskRun(() => LoadAllTracks(newPath));
            }
        };

    private static FileSystemWatcher watcher = null!;

    public static readonly ModSettingBool LoadAsynchronously = new(true)
    {
        description = "Whether to load in tracks asynchronously on a separate thread or directly on the main thread",
        icon = VanillaSprites.JukeboxIcon,
        category = UsefulUtilitiesMod.Jukebox,
    };

    public override void OnLoad()
    {
        if (!Directory.Exists(FolderPath)) Directory.CreateDirectory(FolderPath);
    }

    private static void TaskRun(Action action)
    {
        if (LoadAsynchronously)
        {
            Task.Run(action);
        }
        else
        {
            action();
        }
    }

    private static void TaskSchedule(Action action)
    {
        if (LoadAsynchronously)
        {
            TaskScheduler.ScheduleTask(action);
        }
        else
        {
            action();
        }
    }

    public override void OnRegister()
    {
        if (!Directory.Exists(FolderPath)) Directory.CreateDirectory(FolderPath);
        
        watcher = new FileSystemWatcher(FolderPath);
        watcher.Filters.Add("*.mp3");
        watcher.Filters.Add("*.wav");
        watcher.IncludeSubdirectories = true;
        watcher.Created += (_, args) => TaskScheduler.ScheduleTask(() => TaskRun(() => LoadTrack(args.FullPath)),
            ScheduleType.WaitForSeconds, 1);
        watcher.EnableRaisingEvents = true;
        
        TaskRun(() => LoadAllTracks(FolderPath));
    }

    public static void LoadAllTracks(string path)
    {
        if (!Directory.Exists(FolderPath)) Directory.CreateDirectory(FolderPath);
        var files = Directory.EnumerateFiles(path, "*.mp3", SearchOption.AllDirectories)
            .Concat(Directory.EnumerateFiles(path, "*.wav", SearchOption.AllDirectories));

        foreach (var file in files)
        {
            LoadTrack(file);
        }
    }

    private static void LoadTrack(string filePath)
    {
        var name = Path.GetFileNameWithoutExtension(filePath);
        var id = GetInstance<UsefulUtilitiesMod>().IDPrefix + name;

        if (ResourceHandler.AudioClips.ContainsKey(id)) return;

        ModHelper.Msg<UsefulUtilitiesMod>($"Adding track \"{name}\" from {filePath}");
        var start = DateTime.Now;
        var extension = Path.GetExtension(filePath);
        try
        {
            AudioClip? audioClip = null;

            switch (extension)
            {
                case ".mp3":
                    using (var mp3Reader = new Mp3FileReader(filePath))
                    {
                        audioClip = ResourceHandler.CreateAudioClip(mp3Reader, id);
                    }

                    break;
                case ".wav":
                    using (var wavReader = new WaveFileReader(filePath))
                    {
                        audioClip = ResourceHandler.CreateAudioClip(wavReader, id);
                    }

                    break;
            }

            if (audioClip is null) return;

            TaskSchedule(() =>
            {
                var track = new FileJukeboxTrack(name, audioClip);
                track.Register();
                track.RegisterText(LocalizationManager.Instance.defaultTable);
                var end = DateTime.Now;
                ModHelper.Msg<UsefulUtilitiesMod>(
                    $"Successfully processed track {name} duration {audioClip.length} in {(end - start).TotalSeconds:N1}s");
            });
        }
        catch (Exception e)
        {
            ModHelper.Error<UsefulUtilitiesMod>(e);
            ModHelper.Error<UsefulUtilitiesMod>($"Unable to parse potential jukebox track file {filePath}");
        }
    }

    private class FileJukeboxTrack : ModJukeboxTrack
    {
        public override string Name { get; }
        public override AudioClip AudioClip { get; }

        public override string DisplayName => Name;

        public FileJukeboxTrack(string name, AudioClip audioClip)
        {
            mod = GetInstance<UsefulUtilitiesMod>();
            Name = name;
            AudioClip = audioClip;
        }
    }
}