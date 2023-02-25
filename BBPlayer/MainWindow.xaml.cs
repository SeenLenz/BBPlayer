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
        //This section contains the normal properties of the MainWindow class
        BinaryFormatter formatter = new BinaryFormatter();
        public List<String> SupFormats = new List<String> { ".mp3", ".wav", ".aiff", ".ogg" };
        public List<String> Files = new List<String> { };
        private string[] _folders = new string[] { };
        Dictionary<int, Playlist> Playlists;
        Dictionary<int, Album> Albums;

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

        //This section contains the properties for MainWindow required for BackgroundTask
        ConcurrentQueue<string[]> MessageQueue = new ConcurrentQueue<string[]>();
        ConcurrentDictionary<string, Song> MediaLibrary;
        ConcurrentDictionary<string, FileSystemWatcher> Watchers = new ConcurrentDictionary<string, FileSystemWatcher>();
        bool FileThreadRunning = true;

        public MainWindow()
        {

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
                        this.Albums = (Dictionary<int, Album>)formatter.Deserialize(stream);
                    }
                    catch (System.Runtime.Serialization.SerializationException)
                    {

                        this.Albums = new Dictionary<int, Album>();
                    }

                }
            }
            catch (System.IO.FileNotFoundException)
            {
                using (FileStream fileStream = System.IO.File.Create("./Albums.bin")) { }
                this.Albums = new Dictionary<int, Album>();
            }

            try
            {
                using (Stream stream = System.IO.File.Open("./Playlists.bin", FileMode.Open))
                {
                    try
                    {
                        this.Playlists = (Dictionary<int, Playlist>)formatter.Deserialize(stream);
                    }
                    catch (System.Runtime.Serialization.SerializationException)
                    {

                        this.Playlists = new Dictionary<int, Playlist>();
                    }

                }
            }
            catch (System.IO.FileNotFoundException)
            {
                using (FileStream fileStream = System.IO.File.Create("./Playlists.bin")) { }
                this.Playlists = new Dictionary<int, Playlist>();
            }

            Thread FileThread = new Thread(BackgroundTask);
            FileThread.Start();
            InitializeComponent();
            Closing += WindowEventClose;
        }

        private void WindowEventClose(object sender, System.ComponentModel.CancelEventArgs e)
        {

            Closing -= WindowEventClose;
            this.FileThreadRunning = false;
        }

        private void BackgroundTask()
        {


            foreach (var folder in this.Folders)
            {
                this.ParseFolder(folder);
                this.DirectoryEventSub(folder);
            }
            this.ParseFiles();
            this.Files = new List<String> { };

            while (this.FileThreadRunning)
            {

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


                Thread.Sleep(200);
            }
        }

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

        }

        private void ParseFiles()
        {
            foreach (var file in this.Files)
            {
                MediaLibrary.TryAdd(file.Split(@"\").Last(), new Song(file));
            }
        }

        private void ParseFolder(string path)
        {
            this.Files.AddRange(Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories)
                    .Where(item => SupFormats.Contains(Path.GetExtension(item).ToLower()))
                    .Select(item => Path.GetFullPath(item))
                    .ToList());
        }
        private void Add_Folder(object sender, RoutedEventArgs e)
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

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Directories.Text = "";

            foreach (var entry in this.MediaLibrary)
            {
                Directories.Text += $"\n\nKey: {entry.Key}\nName: {entry.Value.Title}\nTrack: {entry.Value.Track}\nYear: {entry.Value.Year}\nGenre: {entry.Value.Genre}\nAlbum: {entry.Value.Album}\nArtist: {entry.Value.Artist}\nDisc: {entry.Value.Disc}\nDuration: {entry.Value.Duration}\nPath: {entry.Value.Path}\n";
            }
        }
    }
}