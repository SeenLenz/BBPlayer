using BBPlayer.Classes;
using Microsoft.WindowsAPICodePack.Dialogs;
using NAudio.Wave;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using System.Windows;
using TagLib;

namespace BBPlayer
{

    public partial class MainWindow : Window
    {
        #region Properties
        BinaryFormatter formatter = new BinaryFormatter();
        public List<String> SupportedFormats = new List<String> { ".mp3", ".wav", ".aiff", ".ogg" };

        //This is a temporary property used to store the file paths froma parse Directory
        public List<String> Files = new List<String> { };


        Dictionary<string, Playlist> Playlists;
        Dictionary<string, Album> Albums;


        private Config _config;
        public Config Config
        {
            get { return _config; }
            set { _config = value; }
        }


        private int _id;
        public int ID
        {
            get { return ++_id; }
            set { _id = value; }
        }


        private string[] _folders = new string[] { };
        public string[] Folders
        {
            get
            {
                return _folders;
            }
            set
            {
                _folders = value;
                this.MessageQueue.Enqueue(value);
            }
        }

        //DO NOT TOUCH!!! This section contains the properties for MainWindow required for BackgroundTask
        ConcurrentQueue<string[]> MessageQueue = new ConcurrentQueue<string[]>();
        ConcurrentDictionary<string, Song> MediaLibrary;
        ConcurrentDictionary<string, FileSystemWatcher> Watchers = new ConcurrentDictionary<string, FileSystemWatcher>();
        bool FileThreadRunning = true;
        #endregion

        #region Threads
        public MainWindow()
        {
            //Here Every bin File gets deserialized (file beolvasas) 
            //In the first try block we look if the file exists 
            //in the second try block we handle the file empty exceptio
            //we do this for every .bin file (Config, MusicLibrary, Playlists, Albums, Folders)

            try
            {
                using (Stream stream = System.IO.File.Open("./Config.bin", FileMode.Open))
                {
                    try
                    {
                        this.Config = (Config)formatter.Deserialize(stream);
                    }
                    catch (System.Runtime.Serialization.SerializationException)
                    {

                        this.Config = new Config();
                    }

                }
            }
            catch (System.IO.FileNotFoundException)
            {
                using (FileStream fileStream = System.IO.File.Create("./Config.bin")) { }
                this.Config = new Config();
            }

            try
            {
                using (Stream stream = System.IO.File.Open("./Folders.bin", FileMode.Open))
                {
                    try
                    {
                        this.Folders = (string[])formatter.Deserialize(stream);
                    }
                    catch (System.Runtime.Serialization.SerializationException)
                    {

                        this.Folders = new string[] { };
                    }

                }
            }
            catch (System.IO.FileNotFoundException)
            {

                using (FileStream fileStream = System.IO.File.Create("./Folders.bin")) { }
                this.Folders = new string[] { };
            }

            try
            {
                using (Stream stream = System.IO.File.Open("./MediaLibrary.bin", FileMode.Open))
                {
                    try
                    {
                        this.MediaLibrary = (ConcurrentDictionary<string, Song>)formatter.Deserialize(stream);
                    }
                    catch (System.Runtime.Serialization.SerializationException)
                    {

                        this.MediaLibrary = new ConcurrentDictionary<string, Song>();
                    }

                }
            }
            catch (System.IO.FileNotFoundException)
            {
                using (FileStream fileStream = System.IO.File.Create("./MediaLibrary.bin")) { }
                this.MediaLibrary = new ConcurrentDictionary<string, Song>();
            }

            try
            {
                using (Stream stream = System.IO.File.Open("./Albums.bin", FileMode.Open))
                {
                    try
                    {
                        this.Albums = (Dictionary<string, Album>)formatter.Deserialize(stream);
                    }
                    catch (System.Runtime.Serialization.SerializationException)
                    {

                        this.Albums = new Dictionary<string, Album>();
                    }

                }
            }
            catch (System.IO.FileNotFoundException)
            {
                using (FileStream fileStream = System.IO.File.Create("./Albums.bin")) { }
                this.Albums = new Dictionary<string, Album>();
            }

            try
            {
                using (Stream stream = System.IO.File.Open("./Playlists.bin", FileMode.Open))
                {
                    try
                    {
                        this.Playlists = (Dictionary<string, Playlist>)formatter.Deserialize(stream);
                    }
                    catch (System.Runtime.Serialization.SerializationException)
                    {

                        this.Playlists = new Dictionary<string, Playlist>();
                    }

                }
            }
            catch (System.IO.FileNotFoundException)
            {
                using (FileStream fileStream = System.IO.File.Create("./Playlists.bin")) { }
                this.Playlists = new Dictionary<string, Playlist>();
            }

            Thread FileThread = new Thread(BackgroundTask);
            FileThread.Start();
            InitializeComponent();
            Closing += WindowEventClose;
        }

        private void MediaTask()
        {

        }

        private void BackgroundTask()
        {

            //This section gets called only once and parses all the folders for any changes
            foreach (var folder in this.Folders)
            {
                this.ParseFolder(folder);
                this.DirectoryEventSub(folder);
            }
            this.ParseFiles();
            this.Files = new List<String> { };

            while (this.FileThreadRunning)
            {

                //this section gets called every 5 seconds and checks for new folders, if there are any it parses them
                string[] value;
                if (MessageQueue.TryDequeue(out value))
                {
                    foreach (var folder in value)
                    {
                        this.ParseFolder(folder);
                        this.DirectoryEventSub(folder);
                    }
                    this.ParseFiles();
                    this.Files = new List<String> { };

                }

                Thread.Sleep(5000);
            }
        }

        #endregion

        #region Filesystem Interactions
        private void DirectoryEventSub(string path)
        {
            FileSystemWatcher watcher = new FileSystemWatcher(path);

            watcher.EnableRaisingEvents = true;

            watcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName;

            watcher.Created += DirectoryEventCreated;
            watcher.Deleted += DirectoryEventDeleted;
            watcher.Changed += DirectoryEventChanged;

            this.Watchers.TryAdd(path, watcher);
        }
        private void DirectoryEventCreated(object source, FileSystemEventArgs e)
        {
            MediaLibrary.TryAdd(e.Name, new Song(e.FullPath));
        }
        private void DirectoryEventDeleted(object source, FileSystemEventArgs e)
        {
            MediaLibrary.TryRemove(e.Name, out _);
        }
        private void DirectoryEventChanged(object source, FileSystemEventArgs e)
        {
            //This is unhandled as of yet, since im not shure how this event works and stuff
        }
        private void ParseFiles()
        {
            foreach (var file in this.Files)
            {
                string name = file.Split(@"\").Last();
                this.MediaLibrary.TryAdd(name, new Song(file));
                if (this.Albums.ContainsKey(this.MediaLibrary[name].Album))
                {
                    AddSongToAlbum(name);
                }
                else
                {
                    AddAlbum(this.MediaLibrary[name].Album);
                }
            }
        }
        private void ParseFolder(string path)
        {
            this.Files.AddRange(Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories)
                    .Where(item => SupportedFormats.Contains(Path.GetExtension(item).ToLower()))
                    .Select(item => Path.GetFullPath(item))
                    .ToList());
        }
        #endregion

        #region Gui Event Handlers
        private void WindowEventClose(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Closing -= WindowEventClose;
            this.FileThreadRunning = false;
        }
        private void bt_AddFolder(object sender, RoutedEventArgs e)
        {
            var dlg = new CommonOpenFileDialog();
            dlg.IsFolderPicker = true;
            dlg.Multiselect = true;
            dlg.ShowPlacesList = true;

            if (dlg.ShowDialog() == CommonFileDialogResult.Ok)
            {
                string[] selectedFolders = dlg.FileNames.ToArray();

                foreach (var item in selectedFolders)
                {
                    if (this.Folders.Contains(item))
                    {
                        MessageBox.Show("You have already added this folder", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                }

                this.Folders = Folders.Concat(selectedFolders).ToArray();

                using (Stream stream = System.IO.File.Open("./Folders.bin", FileMode.Create))
                {
                    formatter.Serialize(stream, this.Folders);
                }
            }
        }
        private void bt_ListDirectories(object sender, RoutedEventArgs e)
        {
            Directories.Text = "";

            foreach (var entry in this.MediaLibrary)
            {
                Directories.Text += $"\n\nKey: {entry.Key}\nName: {entry.Value.Title}\nTrack: {entry.Value.Track}\nYear: {entry.Value.Year}\nGenre: {entry.Value.Genre}\nAlbum: {entry.Value.Album}\nArtist: {entry.Value.Artist}\nDisc: {entry.Value.Disc}\nDuration: {entry.Value.Duration}\nPath: {entry.Value.Path}\n";
            }
        }

        private void bt_Play(object sender, RoutedEventArgs e) { PlaySong(); }
        private void bt_Next(object sender, RoutedEventArgs e) { NextSong(); }
        private void bt_Previous(object sender, RoutedEventArgs e) { PreviousSong(); }
        #endregion

        #region Album Actions
        private void RemoveAlbum(string key)
        {
            this.Albums.Remove(key);
        }
        private void AddAlbum(string key)
        {
            this.Albums.Add(key, new Album(ID));
        }
        private void AddSongToAlbum(string key)
        {
            this.Albums[this.MediaLibrary[key].Album].SongIdList.Add(key);
        }
        private void RemoveSongFromAlbum(string AlbumKey, string SongKey)
        {
            this.Albums[AlbumKey].SongIdList.Remove(SongKey);
        }
        #endregion

        #region Playlist Actions
        private void RemovePlaylist(string key)
        {
            this.Playlists.Remove(key);
        }
        private void AddPlaylist(string key)
        {
            this.Albums.Add(key, new Album(ID));
        }
        private void AddSongToPlaylist(string key)
        {
            this.Albums[this.MediaLibrary[key].Album].SongIdList.Add(key);
        }
        private void RemoveSongFromPlaylist(string PlaylistKey, string SongKey)
        {
            this.Albums[PlaylistKey].SongIdList.Remove(SongKey);
        }
        #endregion

        #region Playback Functions
        private void Replay() { }
        private void Shuffle() { }
        private void PreviousSong() { }
        private void NextSong() { }
        private void PlaySong()
        {
            using (var audioFile = new AudioFileReader(this.MediaLibrary["Nightcore - Bad boy (128 kbps).mp3"].Path))
            using (var outputDevice = new WaveOutEvent())
            {
                outputDevice.Init(audioFile);
                outputDevice.Play();
                while (outputDevice.PlaybackState == PlaybackState.Playing)
                {
                    Thread.Sleep(1000);
                }
            }
        }
        private void PauseSong() { }
        private void StopSong() { }
        #endregion

    }
}