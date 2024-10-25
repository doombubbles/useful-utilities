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
            description =
                "Any .mp3 or .wav files you put in this folder will be automatically loaded into the BTD6 Jukebox. " +
                "Can add new tracks from files without restarting the game, but can't delete them.",
            onSave = newPath =>
            {
                watcher!.Path = newPath;
                LoadAllTracks(newPath);
            }
        };

    private static FileSystemWatcher watcher = null!;

    public override void OnRegister()
    {
        LoadAllTracks(FolderPath);

        watcher = new FileSystemWatcher(FolderPath);
        watcher.Filters.Add("*.mp3");
        watcher.Filters.Add("*.wav");
        watcher.IncludeSubdirectories = true;
        watcher.Created += (_, args) => LoadTrack(args.FullPath);
        watcher.EnableRaisingEvents = true;
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
        var extension = Path.GetExtension(filePath);

        if (ResourceHandler.AudioClips.ContainsKey(id)) return;

#if DEBUG
        var start = DateTime.Now;
#endif
        ModHelper.Msg<UsefulUtilitiesMod>($"Adding track \"{name}\" from {filePath}");
        Task.Run(() =>
        {
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

                if (audioClip is not null)
                {
                    TaskScheduler.ScheduleTask(() =>
                    {
                        var track = new FileJukeboxTrack(name, audioClip);
                        track.Register();
                        track.RegisterText(LocalizationManager.Instance.defaultTable);
#if DEBUG
                        var end = DateTime.Now;
                        ModHelper.Msg<UsefulUtilitiesMod>(
                            $"Successfully processed track {name} duration  in {(end - start).TotalSeconds:N1}s");
#endif
                    });
                    return;
                }
            }
            catch (Exception e)
            {
                ModHelper.Error<UsefulUtilitiesMod>(e);
            }

            ModHelper.Error<UsefulUtilitiesMod>($"Unable to parse potential jukebox track file {filePath}");
        });
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