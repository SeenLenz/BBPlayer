using BBPlayer.Classes;
using Microsoft.WindowsAPICodePack.Dialogs;
using NAudio.Wave;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
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
using Microsoft.VisualBasic.ApplicationServices;
using Newtonsoft.Json.Linq;
using static Microsoft.WindowsAPICodePack.Shell.PropertySystem.SystemProperties.System;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Globalization;
using Microsoft.WindowsAPICodePack.Shell.PropertySystem;

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

        private Song _songInFocus;

        public Song SongInFocus
        {
            get { return _songInFocus; }
            set
            {
                if (_songInFocus != value)
                {
                    _songInFocus = value;
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        lb_songName.Content = Convert.ToString(value.Title);
                    });
                }
            }
        }

    
        private ObservableCollection<Song> SongList = new ObservableCollection<Song> { };


        private int SongIndex = 0;

        public BinaryFormatter formatter = new BinaryFormatter();
        public List<String> SupportedFormats = new List<String> { ".mp3", ".wav", ".aiff", ".ogg" };

        //This is a temporary property used to store the file paths froma parse Directory
        public List<String> Files = new List<String> { };


        public Dictionary<string, Playlist> Playlists;
        public Dictionary<string, Album> Albums;
        public bool play = false;
        public bool isShuffle = false;
        public bool isReplay = false;
        public bool isReplayInfinite = false;
        public int PlaybackState = 0;
        public bool Pause = false;
        public int státusz = 0;
        public int jelenlegislidermax = 0;
        public string savedfilename;
        public bool isdragged = false;
        public Random random = new Random();


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
        private System.Threading.Tasks.Task PlaybackTask;
        private System.Threading.Tasks.Task FileTask;
        private System.Threading.Tasks.Task PlaybackStateTask;


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

            List<Song> songsToRemove = new List<Song>();
            foreach (var song in this.SongList)
            {
                if (!File.Exists(song.Path))
                {
                    this.MediaLibrary.Remove(song.Path.Split(@"\").Last());
                    songsToRemove.Add(song);
                }
            }

            foreach (var songToRemove in songsToRemove)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    SongList.Remove(songToRemove);
                });
            }

            InitializeComponent();
            SongPanel.ItemsSource = SongList;

            this.view = CollectionViewSource.GetDefaultView(SongPanel.ItemsSource);
            SongPanel.SelectionMode = SelectionMode.Single;
            this.outputDevice.PlaybackStopped += OnPlaybackStopped;
            this.PlaybackTask = System.Threading.Tasks.Task.Run(() => MediaTask());
            this.FileTask = System.Threading.Tasks.Task.Run(() => BackgroundTask());
            if (status != null)
            {
                Slider.IsEnabled = false;
                status.IsEnabled = false;
            }
            Closing += WindowEventClose;

            CollectionView sortview = (CollectionView)CollectionViewSource.GetDefaultView(SongPanel.ItemsSource); ;
            sortview.Filter = SongFilter;
        }

        private void MediaTask()
        {
            while (!this.CancellationToken.Token.IsCancellationRequested)
            {
                try
                {
                    Song song = this.Playback_MessageQueue.Take(this.CancellationToken.Token);
                    this.audioFile = new AudioFileReader(song.Path);
                    this.outputDevice.Init(this.audioFile);
                    outputDevice.Play();
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        outputDevice.Volume = Convert.ToSingle(volume.Value) / 100;
                    });

                }
                catch (OperationCanceledException)
                {
                }
                catch (InvalidOperationException)
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

        private void DirectoryEventSub(string path)
        {
            FileSystemWatcher watcher = new FileSystemWatcher(path);

            watcher.EnableRaisingEvents = true;

            watcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName;

            watcher.Created += DirectoryEventCreated;
            watcher.Deleted += DirectoryEventDeleted;
            watcher.Renamed += DirectoryEventRenamed;

            this.Watchers.TryAdd(path, watcher);
        }

        private void PlayingStateSeconds()
        {
            while ((!this.CancellationToken.Token.IsCancellationRequested))
            {

                if (status != null)
                {


                    while (SongInFocus != null && this.PlaybackState != (int)SongInFocus.Duration.TotalSeconds && this.Pause == false)
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            Slider.IsEnabled = true;
                            status.IsEnabled = true;
                        });


                        if (jelenlegislidermax != (int)SongInFocus.Duration.TotalSeconds)
                        {
                            int slidernum = (int)SongInFocus.Duration.TotalSeconds;
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                Slider.Maximum = slidernum;
                            });
                            jelenlegislidermax = slidernum;
                        }
                        this.PlaybackState += 1;
                        string csere;
                        decimal minute = Math.Floor((decimal)this.PlaybackState / 60);
                        if (this.PlaybackState - minute * 60 < 10)
                        {
                            csere = $"{minute}:0{this.PlaybackState - minute * 60}";
                        }
                        else
                        {
                            csere = $"{minute}:{this.PlaybackState - minute * 60}";
                        }

                        if (isdragged == false)
                        {
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                Slider.Value = this.PlaybackState;
                            });
                        }
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            status.Content = csere;
                        });

                        if (PlaybackState == jelenlegislidermax)
                        {
                            Replay_OnSongEnd();
                        }
                        Thread.Sleep(1000);
                    }
                }
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


                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        SongList.Add(temp);
                    });

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

        private void txtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            CollectionViewSource.GetDefaultView(SongPanel.ItemsSource).Refresh();
        }

        private bool SongFilter(object item)
        {
            if (String.IsNullOrEmpty(txtSearch.Text))
                return true;
            else
                return ((item as Song).Title.IndexOf(txtSearch.Text, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private void SongFocusChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                this.SongInFocus = this.SongList[this.SongList.IndexOf((Song)SongPanel.Items[SongPanel.SelectedIndex])];
                if (play == true)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        status.Content = "0:00";
                        this.státusz = 0;
                        this.PlaybackState = 0;
                        Slider.Value = 0;
                        this.outputDevice.Stop();
                    });
                    PlaySong();
                }
            }
            catch (ArgumentOutOfRangeException)
            {
                this.SongInFocus = this.SongList[0];
            }

        }

        private void SortChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox comboBox = sender as ComboBox;

            ComboBoxItem selectedItem = comboBox.SelectedItem as ComboBoxItem;
            string selectedContent = selectedItem.Content as string;

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
            foreach (var item in this.Folders)
            {
                tb_addedFolders.Text += item;
            }
        }
        //private void bt_ListDirectories(object sender, RoutedEventArgs e)
        //{
        //    Directories.Text = "";

        //    foreach (var entry in this.MediaLibrary)
        //    {
        //        Directories.Text += $"\n\nKey: {entry.Key}\nName: {entry.Value.Title}\nTrack: {entry.Value.Track}\nYear: {entry.Value.Year}\nGenre: {entry.Value.Genre}\nAlbum: {entry.Value.Album}\nArtist: {entry.Value.Artist}\nDisc: {entry.Value.Disc}\nDuration: {entry.Value.Duration}\nPath: {entry.Value.Path}\n";
        //    }

        //    //Directories.Text += $"\n\nKey: {this.SongInFocus.Key}\nName: {this.SongInFocus.Value.Title}\nTrack: {this.SongInFocus.Value.Track}\nYear: {this.SongInFocus.Value.Year}\nGenre: {this.SongInFocus.Value.Genre}\nAlbum: {this.SongInFocus.Value.Album}\nArtist: {this.SongInFocus.Value.Artist}\nDisc: {this.SongInFocus.Value.Disc}\nDuration: {this.SongInFocus.Value.Duration}\nPath: {this.SongInFocus.Value.Path}\n";

        //}
        private void bt_Play(object sender, RoutedEventArgs e)
        {
            if (play == false)
            {
                PlaySong();
                play = true;
                BitmapImage bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri("./img/pause.png", UriKind.Relative);
                bitmap.EndInit();
                play_pic.Source = bitmap;
            }
            else
            {
                PauseSong();
                play = false;
                BitmapImage bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri("./img/play.png", UriKind.Relative);
                bitmap.EndInit();
                play_pic.Source = bitmap;
            }

        }
        private void bt_Next(object sender, RoutedEventArgs e) {
                NextSong();
        }
        private void bt_Previous(object sender, RoutedEventArgs e) { PreviousSong(); }
        private void bt_Replay(object sender, RoutedEventArgs e) { Replay(); }
        private void DragStarted(object sender, DragStartedEventArgs e) { onDragStarted(); }

        private void bt_shuffle1(object sender, RoutedEventArgs e) { Shuffle(); }

        private void DragCompleted(object sender, RoutedEventArgs e) { Drag(); }
        private void volume_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) { vol(e.NewValue); }

        private void vol(double e)
        {
            float x = Convert.ToSingle(e) / 100;
            outputDevice.Volume = x;
        }
        #endregion

        #region Event Handlers

        private void DirectoryEventCreated(object source, FileSystemEventArgs e)
        {
            System.Threading.Thread.Sleep(1000);
            Song temp = new Song(e.FullPath, ID);
            if (!this.MediaLibrary.ContainsKey(e.Name))
            {
                this.MediaLibrary.TryAdd(e.Name, temp);
                Application.Current.Dispatcher.Invoke(() =>
                {
                    SongList.Add(temp);
                });
            }
        }
        private void DirectoryEventDeleted(object source, FileSystemEventArgs e)
        {
            this.MediaLibrary.Remove(e.Name, out _);
            Song songToRemove = SongList.FirstOrDefault(s => s.Path == e.FullPath);
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (songToRemove != null)
                {
                    SongList.Remove(songToRemove);
                }
            });

        }
        private void DirectoryEventRenamed(object source, RenamedEventArgs e)
        {

            //var songToUpdate = SongList.FirstOrDefault(s => s.Path == e.OldFullPath);
            //TagLib.File Raw = TagLib.File.Create(e.FullPath);
            //if (songToUpdate != null)
            //{
            //    songToUpdate.Path = e.FullPath;
            //    songToUpdate.FileName = e.FullPath.Split(@"\").Last(); ;
            //    songToUpdate.Title = Raw.Tag.Title ?? e.FullPath.Split(@"\").Last().Split(".").First();

            //    MediaLibrary[songToUpdate.FileName] = songToUpdate;
            //    CollectionViewSource.GetDefaultView(SongList).Refresh();
            //}

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
        private void Replay()
        {
            if (this.isReplay == false && isReplayInfinite == false)
            {
                this.isReplay = true;
                BitmapImage bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri("./img/repeat_on.png", UriKind.Relative);
                bitmap.EndInit();
                replay_pic.Source = bitmap;
               

            }
            else if (this.isReplay == true)
            {
                this.isReplay = false;
                this.isReplayInfinite = true;

                BitmapImage bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri("./img/infreplay.png", UriKind.Relative);
                bitmap.EndInit();
                replay_pic.Source = bitmap;

            }
            else
            {
                this.isReplayInfinite = false;

                BitmapImage bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri("./img/repeat.png", UriKind.Relative);
                bitmap.EndInit();
                replay_pic.Source = bitmap;

            }
        }
        private void Replay_OnSongEnd()
        {
            if (this.isShuffle == true)
            {
                if (this.isReplayInfinite == true)
                {
                    PauseSong();
                    this.PlaybackState = 0;
                    this.státusz = 0;
                    this.audioFile.Position = 0;

                    PlaySong();
                }
                else
                {
                        int x = random.Next(0, SongList.Count - 1);
                        PauseSong();
                        this.PlaybackState = 0;
                        this.státusz = 0;
                        this.audioFile.Position = 0;
                        this.SongInFocus = SongList[x];
                        PlaySong();
                }
                Application.Current.Dispatcher.Invoke(() =>
                {
                    BitmapImage bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri("./img/pause.png", UriKind.Relative);
                    bitmap.EndInit();
                    play_pic.Source = bitmap;
                });
            }
            else if (this.isReplay == true)
            {
                if (this.SongInFocus == this.SongList[SongList.Count - 1]) // megvizsgálni hogy a lejátszási lista végén vagyunk-e
                {
                    PauseSong();
                    this.SongInFocus = this.SongList[0];
                    SongIndex = 0;
                    this.outputDevice.Stop();
                    this.outputDevice.Dispose();
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        status.Content = "0:00";
                        this.státusz = 0;
                        this.PlaybackState = 0;
                        Slider.Value = 0;
                    });
                    PlaySong();
                }
                else
                {
                    PauseSong();
                    if (SongIndex + 1 != SongList.Count)
                    {
                        this.SongInFocus = this.SongList[++SongIndex];

                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            status.Content = "0:00";
                            this.státusz = 0;
                            this.PlaybackState = 0;
                            Slider.Value = 0;
                        });
                    }
                    this.outputDevice.Stop();
                    this.outputDevice.Dispose();
                    PlaySong();
                }
                Application.Current.Dispatcher.Invoke(() =>
                {
                    BitmapImage bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri("./img/pause.png", UriKind.Relative);
                    bitmap.EndInit();
                    play_pic.Source = bitmap;
                });
               
            }
            else if (this.isReplayInfinite == true)
            {
                PauseSong();
                this.PlaybackState = 0;
                this.státusz = 0;
                this.audioFile.Position = 0;

                PlaySong();
                Application.Current.Dispatcher.Invoke(() =>
                {
                    BitmapImage bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri("./img/pause.png", UriKind.Relative);
                    bitmap.EndInit();
                    play_pic.Source = bitmap;
                });
            }
            else
            {
                PauseSong();

                Application.Current.Dispatcher.Invoke(() =>
                {
                    BitmapImage bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri("./img/play.png", UriKind.Relative);
                    bitmap.EndInit();
                    play_pic.Source = bitmap;
                });
            }
        }
        private void Shuffle()
        {
            if (this.isShuffle == false)
            {
                this.isShuffle = true;

                BitmapImage bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri("./img/shuffle_on.png", UriKind.Relative);
                bitmap.EndInit();
                shuffle_pic.Source = bitmap;

            }
            else
            {
                this.isShuffle = false;

                BitmapImage bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri("./img/shuffle.png", UriKind.Relative);
                bitmap.EndInit();
                shuffle_pic.Source = bitmap;

            }
        }
        private void PreviousSong()
        {
            if (PlaybackState < 20) // lejátszás nem a szám első 20 mp-jében van
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    status.Content = "0:00";
                    this.státusz = 0;
                    this.PlaybackState = 0;
                    Slider.Value = 0;
                    this.outputDevice.Stop();
                });
                PlaySong();
            }
            else if (SongIndex - 1 != -1)
            {
                this.SongInFocus = this.SongList[this.SongList.IndexOf((Song)SongPanel.Items[--SongPanel.SelectedIndex])];

                Application.Current.Dispatcher.Invoke(() =>
                {
                    status.Content = "0:00";
                    this.státusz = 0;
                    this.PlaybackState = 0;
                    Slider.Value = 0;
                    this.outputDevice.Stop();
                    this.outputDevice.Dispose();
                });
                PlaySong();
            }
            else
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    status.Content = "0:00";
                    this.státusz = 0;
                    this.PlaybackState = 0;
                    Slider.Value = 0;
                    this.outputDevice.Stop();
                });
                PlaySong();
            }
            Application.Current.Dispatcher.Invoke(() =>
            {
                BitmapImage bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri("./img/pause.png", UriKind.Relative);
                bitmap.EndInit();
                play_pic.Source = bitmap;
            });

        }
        private void NextSong()
        {
            if (SongIndex + 1 != SongList.Count)
            {
                if (this.isShuffle == true)
                {
                    if (this.isReplayInfinite == true)
                    {
                        PauseSong();
                        this.PlaybackState = 0;
                        this.státusz = 0;
                        this.audioFile.Position = 0;

                        PlaySong();
                    }
                    else
                    {
                        int x = random.Next(0, SongList.Count - 1);
                        PauseSong();
                        this.PlaybackState = 0;
                        this.státusz = 0;
                        this.audioFile.Position = 0;
                        this.SongInFocus = SongList[x];
                        PlaySong();
                    }
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        BitmapImage bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.UriSource = new Uri("./img/pause.png", UriKind.Relative);
                        bitmap.EndInit();
                        play_pic.Source = bitmap;
                    });
                }
                else
                {
                    this.SongInFocus = this.SongList[this.SongList.IndexOf((Song)SongPanel.Items[++SongPanel.SelectedIndex])];

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        status.Content = "0:00";
                        this.státusz = 0;
                        this.PlaybackState = 0;
                        Slider.Value = 0;
                        this.outputDevice.Stop();
                        this.outputDevice.Dispose();
                    });
                    this.PlaySong();
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        BitmapImage bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.UriSource = new Uri("./img/pause.png", UriKind.Relative);
                        bitmap.EndInit();
                        play_pic.Source = bitmap;
                    });
                }
            }
        }
        //private void Replay() { }
        //private void Shuffle() { }

        //private void PreviousSong() { this.SongInFocus = this.SongList[this.SongList.IndexOf((Song)SongPanel.Items[--SongPanel.SelectedIndex])]; }
        //private void NextSong() { this.SongInFocus = this.SongList[this.SongList.IndexOf((Song)SongPanel.Items[SongPanel.SelectedIndex])]; }

        private void PlaySong()
        {
            if (this.PlaybackStateTask == null || this.PlaybackStateTask.Status != TaskStatus.Running)
            {
                this.Playback_MessageQueue.Add(this.SongInFocus);
                this.PlaybackStateTask = System.Threading.Tasks.Task.Run(() => PlayingStateSeconds());
            }
            else
            {
                if (SongInFocus.FileName == savedfilename)
                {
                    this.Pause = false;
                    int sampleRate = audioFile.WaveFormat.SampleRate;
                    int bitsPerSample = audioFile.WaveFormat.BitsPerSample;
                    int channels = audioFile.WaveFormat.Channels;
                    this.audioFile.Position = SecondsToBytes(státusz, sampleRate, channels, bitsPerSample);

                    this.outputDevice.Init(audioFile);
                    this.outputDevice.Play();
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        outputDevice.Volume = Convert.ToSingle(volume.Value) / 100;
                    });
                }
                else
                {
                    this.Playback_MessageQueue.Add(this.SongInFocus);

                    this.Pause = false;

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        status.Content = "0:00";
                        this.státusz = 0;
                        this.PlaybackState = 0;
                        Slider.Value = 0;
                        this.outputDevice.Stop();
                        this.outputDevice.Dispose();
                    });


                    //this.outputDevice.Init(audioFile);
                }

            }
        }
        private void PauseSong()
        {

            if (this.Pause == false)
            {
                this.Pause = true;
                decimal minute = Math.Floor((decimal)PlaybackState / 60);
                Application.Current.Dispatcher.Invoke(() =>
                {
                    string temp = status.Content.ToString();
                    string min = temp.Split(':')[0];
                    string sec = temp.Split(':')[1];
                    int time = (int.Parse(min) * 60) + int.Parse(sec);
                    this.státusz = time;
                    this.outputDevice.Stop();
                });
                savedfilename = this.SongInFocus.FileName;

            }
        }
        private void onDragStarted()
        {
            isdragged = true;
        }

        private void Drag()
        {
            this.outputDevice.Stop();
            isdragged = false;
            int érték = Convert.ToInt32(Slider.Value);
            int sampleRate = audioFile.WaveFormat.SampleRate;
            int bitsPerSample = audioFile.WaveFormat.BitsPerSample;
            int channels = audioFile.WaveFormat.Channels;
            this.audioFile.Position = SecondsToBytes(érték, sampleRate, channels, bitsPerSample);
            this.outputDevice.Init(audioFile);
            PlaybackState = érték;
            Slider.Value = érték;
            string csere;
            decimal minute = Math.Floor((decimal)this.PlaybackState / 60);
            if (this.PlaybackState - minute * 60 < 10)
            {
                csere = $"{minute}:0{this.PlaybackState - minute * 60}";
            }
            else
            {
                csere = $"{minute}:{this.PlaybackState - minute * 60}";
            }
            status.Content = csere;
            this.outputDevice.Play();
            Application.Current.Dispatcher.Invoke(() =>
            {
                outputDevice.Volume = Convert.ToSingle(volume.Value) / 100;
            });
            Application.Current.Dispatcher.Invoke(() =>
            {
                BitmapImage bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri("./img/pause.png", UriKind.Relative);
                bitmap.EndInit();
                play_pic.Source = bitmap;
            });
        }

        #endregion

        #region Conversions
        public static long SecondsToBytes(double durationInSeconds, int sampleRate, int channels, int bitsPerSample)
        {
            long bytes = (long)(durationInSeconds * sampleRate * channels * bitsPerSample / 8);
            Debug.WriteLine(bytes);
            return bytes;
        }





        #endregion

        private void closeWindow1(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

       

        private void ShowPopup(object sender, RoutedEventArgs e)
        {
            foreach (var item in this.Folders)
            {
                tb_addedFolders.Text += item;
            }
            Popup1.IsOpen = true;
        }
        private void ClosePopup(object sender, RoutedEventArgs e)
        {
            tb_addedFolders.Text = "";
            Popup1.IsOpen = false;
        }
    }
}