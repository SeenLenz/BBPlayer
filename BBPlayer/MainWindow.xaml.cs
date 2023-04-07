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
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Data;
using WForms = System.Windows.Forms;
using System.Windows.Forms.Integration;
using System.Windows.Controls.Primitives;
using System.Xml.Linq;
using System.ComponentModel;
using System.Text.Json;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows.Data;
using System.Reflection;

namespace BBPlayer
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        #region Properties

        public enum SortTypes
        {
            DateAdded,
            Duration,
            Title,
            Artist,
            Album,
        }

        private SortTypes _CurrentSort;

        public SortTypes CurrentSort
        {
            get { return _CurrentSort; }
            set
            {
                this.Config.CurrentSort = value;
                _CurrentSort = value;
            }
        }


        public event PropertyChangedEventHandler? PropertyChanged;

        public int StartIndex = 0;
        public int PageSize = 30;
        public int CurrentCount = 0;
        public int TotalCount = 0;
        public double ScrollDifference = 0;

        private WaveOutEvent outputDevice = new WaveOutEvent();
        private AudioFileReader audioFile;

        private Song SongInFocus;
        private ObservableCollection<Song> SongList = new ObservableCollection<Song> { };


        private int SongIndex = 0;

        public BinaryFormatter formatter = new BinaryFormatter();
        public List<String> SupportedFormats = new List<String> { ".mp3", ".wav", ".aiff", ".ogg" };

        //This is a temporary property used to store the file paths froma parse Directory
        public List<String> Files = new List<String> { };


        public Dictionary<string, Playlist> Playlists;
        public Dictionary<string, Album> Albums;

        public bool isShuffle = false;
        public bool isReplay = false;
        public bool isReplayInfinite = false;

        private Config _config;
        public Config Config
        {
            get { return _config; }
            set { _config = value; }
        }


        private int _id;
        public int ID
        {
            get
            {
                this.Config.uid = _id;
                return ++_id;
            }
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

        private WForms.ListView SongListView = new WForms.ListView();

        //DO NOT TOUCH!!! This section contains the properties for MainWindow required for BackgroundTask
        public BlockingCollection<Song> Playback_MessageQueue = new BlockingCollection<Song>();
        private CancellationTokenSource CancellationToken = new CancellationTokenSource();
        public ConcurrentQueue<string[]> MessageQueue = new ConcurrentQueue<string[]>();
        public Dictionary<string, Song> MediaLibrary;
        public ConcurrentDictionary<string, FileSystemWatcher> Watchers = new ConcurrentDictionary<string, FileSystemWatcher>();
        public bool FileThreadRunning = true;
        private Task PlaybackTask;
        private Task FileTask;

        string SongListPath = @"./SongList.json";
        string ConfigPath = @"./Config.json";
        string FoldersPath = @"./Folders.json";
        string MediaLibraryPath = @"./MediaLibrary.json";
        string AlbumsPath = @"./Albums.Json";
        string PlaylistsPath = @"./Playlists.json";

        ICollectionView view;

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
                        this.ID = this.Config.uid;
                        this.CurrentSort = this.Config.CurrentSort;
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
                        this.MediaLibrary = (Dictionary<string, Song>)formatter.Deserialize(stream);
                    }
                    catch (System.Runtime.Serialization.SerializationException)
                    {

                        this.MediaLibrary = new Dictionary<string, Song>();
                    }

                }
            }
            catch (System.IO.FileNotFoundException)
            {
                using (FileStream fileStream = System.IO.File.Create("./MediaLibrary.bin")) { }
                this.MediaLibrary = new Dictionary<string, Song>();
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

            try
            {
                using (Stream stream = System.IO.File.Open("./SongList.bin", FileMode.Open))
                {
                    try
                    {
                        this.SongList = (ObservableCollection<Song>)formatter.Deserialize(stream);
                    }
                    catch (System.Runtime.Serialization.SerializationException)
                    {

                        this.SongList = new ObservableCollection<Song>();
                    }

                }
            }
            catch (System.IO.FileNotFoundException)
            {
                using (FileStream fileStream = System.IO.File.Create("./SongList.bin")) { }
                this.Playlists = new Dictionary<string, Playlist>();
            }


            InitializeComponent();
            SongPanel.ItemsSource = SongList;
            this.view = CollectionViewSource.GetDefaultView(SongPanel.ItemsSource);
            SongPanel.SelectionMode = SelectionMode.Single;
            this.outputDevice.PlaybackStopped += OnPlaybackStopped;
            this.PlaybackTask = Task.Run(() => MediaTask());
            this.FileTask = Task.Run(() => BackgroundTask());
            Closing += WindowEventClose;
        }

        private void MediaTask()
        {
            while (!this.CancellationToken.Token.IsCancellationRequested)
            {
                try
                {
                    Song song = this.Playback_MessageQueue.Take(this.CancellationToken.Token);
                    this.outputDevice.Init(new AudioFileReader(song.Path));
                    outputDevice.Play();
                }
                catch (OperationCanceledException)
                {

                }
            }
        }

        private void BackgroundTask()
        {
            if (this.SongList != null && this.SongList.Count != 0)
            {
                this.SongInFocus = this.SongList[SongIndex];
            }
            //This section gets called only once and parses all the folders for any changes
            while (!this.CancellationToken.Token.IsCancellationRequested)
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
                    if (this.SongIndex == null)
                    {
                        this.SongInFocus = this.SongList[SongIndex];
                    }

                    this.Files = new List<String> { };

                }

                Thread.Sleep(5000);
            }
        }

        #endregion

        #region Filesystem Interactions

        private void ParseFiles()
        {
            foreach (var file in this.Files)
            {
                string name = file.Split(@"\").Last();
                if (!this.MediaLibrary.ContainsKey(name))
                {
                    this.MediaLibrary.TryAdd(name, new Song(file, ID));
                    Song temp = new Song(file, ID);

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
        }
        private void ParseFolder(string path)
        {
            this.Files.AddRange(Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories)
                    .Where(item => SupportedFormats.Contains(Path.GetExtension(item).ToLower()))
                    .Select(item => Path.GetFullPath(item))
                    .ToList());
        }
        private void LoadSong(string path) { }
        #endregion

        #region Gui Event Handlers
    
        private void SongFocusChanged(object sender, SelectionChangedEventArgs e)
        {
            this.SongInFocus = this.SongList[this.SongList.IndexOf((Song)SongPanel.Items[SongPanel.SelectedIndex])];
        }

        private void SortChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox comboBox = sender as ComboBox;

            ComboBoxItem selectedItem = comboBox.SelectedItem as ComboBoxItem;
            string selectedContent = selectedItem.Content as string;

            TestLabel.Content = selectedContent;
            view.SortDescriptions.Clear();
            switch (selectedContent)
            {
                case "Album":
                    this.view.SortDescriptions.Add(new SortDescription("Album", ListSortDirection.Descending));
                    view.Refresh();
                    break;
                case "Date Added":
                    this.view.SortDescriptions.Add(new SortDescription("ID", ListSortDirection.Descending));
                    view.Refresh();
                    break;
                case "Artist":
                    this.view.SortDescriptions.Add(new SortDescription("Artist", ListSortDirection.Descending));
                    view.Refresh();
                    break;
                case "Title":
                    this.view.SortDescriptions.Add(new SortDescription("Title", ListSortDirection.Descending));
                    view.Refresh();
                    break;
                case "Duration":
                    this.view.SortDescriptions.Add(new SortDescription("Duration", ListSortDirection.Descending));
                    view.Refresh();
                    break;
                case "Most Played":
                    this.view.SortDescriptions.Add(new SortDescription("Clicks", ListSortDirection.Descending));
                    view.Refresh();
                    break;
                default:
                    break;
            }
        }

        private void WindowEventClose(object sender, System.ComponentModel.CancelEventArgs e)
        {

            CancellationToken.Cancel();
            outputDevice.Stop();
            outputDevice.Dispose();
            Playback_MessageQueue.CompleteAdding();

            using (Stream stream = System.IO.File.Open("./MediaLibrary.bin", FileMode.Create))
            {
                this.formatter.Serialize(stream, this.MediaLibrary);
            }

            using (Stream stream = System.IO.File.Open("./SongList.bin", FileMode.Create))
            {
                this.formatter.Serialize(stream, this.SongList);
            }

            using (Stream stream = System.IO.File.Open("./Albums.bin", FileMode.Create))
            {
                this.formatter.Serialize(stream, this.Albums);
            }

            using (Stream stream = System.IO.File.Open("./Playlists.bin", FileMode.Create))
            {
                this.formatter.Serialize(stream, this.Playlists);
            }

            using (Stream stream = System.IO.File.Open("./Folders.bin", FileMode.Create))
            {
                this.formatter.Serialize(stream, this.Folders);
            }

            using (Stream stream = System.IO.File.Open("./Config.bin", FileMode.Create))
            {
                this.formatter.Serialize(stream, this.Config);
            }

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

            //Directories.Text += $"\n\nKey: {this.SongInFocus.Key}\nName: {this.SongInFocus.Value.Title}\nTrack: {this.SongInFocus.Value.Track}\nYear: {this.SongInFocus.Value.Year}\nGenre: {this.SongInFocus.Value.Genre}\nAlbum: {this.SongInFocus.Value.Album}\nArtist: {this.SongInFocus.Value.Artist}\nDisc: {this.SongInFocus.Value.Disc}\nDuration: {this.SongInFocus.Value.Duration}\nPath: {this.SongInFocus.Value.Path}\n";

        }
        private void bt_Play(object sender, RoutedEventArgs e) { PlaySong(); }
        private void bt_Next(object sender, RoutedEventArgs e) { NextSong(); }
        private void bt_Stop(object sender, RoutedEventArgs e) { StopSong(); }
        private void bt_Previous(object sender, RoutedEventArgs e) { PreviousSong(); }
        #endregion

        #region Event Handlers
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
            MediaLibrary.TryAdd(e.Name, new Song(e.FullPath, ID));
        }
        private void DirectoryEventDeleted(object source, FileSystemEventArgs e)
        {
            MediaLibrary.Remove(e.Name, out _);
        }
        private void DirectoryEventChanged(object source, FileSystemEventArgs e)
        {
            //This is unhandled as of yet, since im not shure how this event works and stuff
        }
        public void OnPlaybackStopped(object sender, StoppedEventArgs e)
        {
        }
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

        #region Playback Actions
        private void Replay() { }
        private void Shuffle() { }

        private void PreviousSong() { this.SongInFocus = this.SongList[this.SongList.IndexOf((Song)SongPanel.Items[--SongPanel.SelectedIndex])]; }
        private void NextSong() { this.SongInFocus = this.SongList[this.SongList.IndexOf((Song)SongPanel.Items[SongPanel.SelectedIndex])]; }

        private void PlaySong()
        {
            Playback_MessageQueue.Add(this.SongInFocus);
        }
        private void PauseSong() { }
        private void StopSong() { this.outputDevice.Stop(); }
        #endregion


    }
}