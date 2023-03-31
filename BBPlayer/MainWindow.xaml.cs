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
using TagLib;

namespace BBPlayer
{
    public partial class MainWindow : Window
    {
        #region Properties

        private WaveOutEvent outputDevice = new WaveOutEvent();
        private AudioFileReader audioFile;

        private KeyValuePair<string, Song> SongInFocus;
        private List<KeyValuePair<string, Song>> SongList = new List<KeyValuePair<string, Song>> { };
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
        public int PlaybackState = 0;
        public bool Pause = false;
        public int státusz = 0;
        public int jelenlegislidermax = 0;
        public string savedfilename;


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
        public BlockingCollection<Song> Playback_MessageQueue = new BlockingCollection<Song>();
        private CancellationTokenSource CancellationToken = new CancellationTokenSource();
        public ConcurrentQueue<string[]> MessageQueue = new ConcurrentQueue<string[]>();
        public ConcurrentDictionary<string, Song> MediaLibrary;
        public ConcurrentDictionary<string, FileSystemWatcher> Watchers = new ConcurrentDictionary<string, FileSystemWatcher>();
        public bool FileThreadRunning = true;
        private Task PlaybackTask;
        private Task FileTask;
        private Task PlaybackStateTask;
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

            this.outputDevice.PlaybackStopped += OnPlaybackStopped;
            this.PlaybackTask = Task.Run(() => MediaTask());
            this.FileTask = Task.Run(() => BackgroundTask());
            

            InitializeComponent();
            if(status != null)
            {
                Slider.IsEnabled = false;
                status.IsEnabled = false;
            }
            Closing += WindowEventClose;
        }

        private void MediaTask()
        {
            while (!this.CancellationToken.Token.IsCancellationRequested)
            {
                try
                {
                    Song song = this.Playback_MessageQueue.Take(this.CancellationToken.Token);
                    this.outputDevice.Init(this.audioFile = new AudioFileReader(song.Path));
                    outputDevice.Play();
                }
                catch (OperationCanceledException)
                {

                }
            }
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

            this.SongList = this.MediaLibrary.OrderBy(e => e.Value.ID).ToList();

            if (this.SongList.Count != 0)
            {
                this.SongInFocus = this.SongList[SongIndex];
            }

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

       private void PlayingStateSeconds()
        {
            while ((!this.CancellationToken.Token.IsCancellationRequested))
            {

                if (status != null)
                {
                       
                    
                    while (SongInFocus.Key != null && this.PlaybackState != (int)SongInFocus.Value.Duration.TotalSeconds && this.Pause == false)
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            Slider.IsEnabled = true;
                            status.IsEnabled = true;
                        });
                        

                        if (jelenlegislidermax != (int)SongInFocus.Value.Duration.TotalSeconds)
                        {
                            int slidernum = (int)SongInFocus.Value.Duration.TotalSeconds;
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                Slider.Maximum = slidernum;
                            });
                            jelenlegislidermax = slidernum;
                        }
                        this.PlaybackState += 1;
                        string csere;
                        decimal minute = Math.Floor((decimal)this.PlaybackState / 60);
                        if(this.PlaybackState - minute * 60 < 10)
                        {
                            csere = $"{minute}:0{this.PlaybackState - minute * 60}";
                        }
                        else
                        {
                            csere = $"{minute}:{this.PlaybackState - minute * 60}";
                        }
                        
                        
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            
                            status.Content = csere;
                            Slider.Value = this.PlaybackState;
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
                this.MediaLibrary.TryAdd(name, new Song(file, ID));
                this.SongList.Add(new KeyValuePair<string, Song>(name, new Song(file, ID)));
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
        private void LoadSong(string path) { }
        #endregion

        #region Gui Event Handlers
        private void WindowEventClose(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Closing -= WindowEventClose;
            outputDevice.Stop();
            outputDevice.Dispose();
            Playback_MessageQueue.CompleteAdding();
            CancellationToken.Cancel();
            PlaybackTask.Wait();
            FileTask.Wait();
            PlaybackStateTask.Wait();
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
        private void bt_Play(object sender, RoutedEventArgs e) 
        {
            if (bt_play.Content.ToString() == "Play")
            {
                PlaySong();
                bt_play.Content = "Pause";
            }
            else
            {
                PauseSong();
                bt_play.Content = "Play";
            }

        }
        private void bt_Next(object sender, RoutedEventArgs e) { NextSong(); }
        private void bt_Previous(object sender, RoutedEventArgs e) { PreviousSong(); }
        private void bt_Replay(object sender, RoutedEventArgs e){ Replay(); }
        private void DragCompleted(object sender, RoutedEventArgs e) { }
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
            MediaLibrary.TryRemove(e.Name, out _);
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
        private void Replay() {
            if(this.isReplay == false && isReplayInfinite == false)
            {
                this.isReplay = true;

                replay.Content = "Replay on";
                
            }
            else if(this.isReplay == true)
            {
                this.isReplay = false;
                this.isReplayInfinite = true;

                replay.Content = "Inf Replay";

            }
            else
            {
                this.isReplayInfinite = false;

                replay.Content = "Replay off";
                
            }
        }
        private void Replay_OnSongEnd()
        {
            if(this.isReplay == true)
            {
                if (this.SongInFocus.Value == this.SongList[SongList.Count].Value) // megvizsgálni hogy a lejátszási lista végén vagyunk-e
                {
                    PauseSong();
                    this.SongInFocus = this.SongList[0];
                    this.outputDevice.Dispose();
                    PlaySong();
                }
                else
                {
                    PauseSong();
                    NextSong();
                    this.outputDevice.Dispose();
                    PlaySong();
                }
            }
            else if(this.isReplayInfinite == true)
            {
                PauseSong();
                this.PlaybackState = 0;
                this.státusz = 0;
                this.audioFile.Position = 0;

                PlaySong();
            }
            else
            {
                PauseSong();
            }
        }
        private void Shuffle() { }
        private void PreviousSong() {
            if (SongIndex -1 != -1)
            {
                this.SongInFocus = this.SongList[--SongIndex];

                Application.Current.Dispatcher.Invoke(() =>
                {
                    this.státusz = 0;
                    this.PlaybackState = 0;
                    Slider.Value = 0;
                    this.outputDevice.Stop();
                    this.outputDevice.Dispose();
                });
                PlaySong();
            }

        }
        private void NextSong() {
            if (SongIndex + 1 != SongList.Count)
            {
                this.SongInFocus = this.SongList[++SongIndex];

                Application.Current.Dispatcher.Invoke(() =>
                {
                    this.státusz = 0;
                    this.PlaybackState = 0;
                    Slider.Value = 0;
                    this.outputDevice.Stop();
                    this.outputDevice.Dispose();
                });
                this.PlaySong();
            }
            

        }
        private void PlaySong()
        {
            if (this.PlaybackStateTask == null || this.PlaybackStateTask.Status != TaskStatus.Running)
            {
            this.Playback_MessageQueue.Add(this.SongInFocus.Value);
            this.PlaybackStateTask = Task.Run(() => PlayingStateSeconds());
            }
            else
            {
                if (SongInFocus.Value.FileName == savedfilename)
                {
                    this.Pause = false;
                    int sampleRate = audioFile.WaveFormat.SampleRate;
                    int bitsPerSample = audioFile.WaveFormat.BitsPerSample;
                    int channels = audioFile.WaveFormat.Channels;
                    this.audioFile.Position = SecondsToBytes(státusz, sampleRate, channels, bitsPerSample);

                    this.outputDevice.Init(audioFile);
                    this.outputDevice.Play();
                }
                else
                {
                    this.Playback_MessageQueue.Add(this.SongInFocus.Value);

                    this.Pause = false;
                    int sampleRate = audioFile.WaveFormat.SampleRate;
                    int bitsPerSample = audioFile.WaveFormat.BitsPerSample;
                    int channels = audioFile.WaveFormat.Channels;
                    this.audioFile.Position = SecondsToBytes(státusz, sampleRate, channels, bitsPerSample);

                    this.outputDevice.Init(audioFile);
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
                    int time = int.Parse(min) * 60 + int.Parse(sec);
                    this.státusz = time;
                    this.outputDevice.Stop();
                });
                savedfilename = this.SongInFocus.Value.FileName;
                
            }
            //else if(this.Pause == true)
            //{
            //    this.Pause = false;
            //    int sampleRate = audioFile.WaveFormat.SampleRate;
            //    int bitsPerSample = audioFile.WaveFormat.BitsPerSample;
            //    int channels = audioFile.WaveFormat.Channels;
            //    this.audioFile.Position = SecondsToBytes(státusz, sampleRate, channels, bitsPerSample);
            //    this.outputDevice.Init(audioFile);
            //    this.outputDevice.Play();
            //}
        }


        #endregion

        #region Conversions
        public static long SecondsToBytes(double durationInSeconds, int sampleRate, int channels, int bitsPerSample)
        {
            long bytes = (long)(durationInSeconds * sampleRate * channels * bitsPerSample / 8);
            return bytes;
        }
        #endregion

    }
}