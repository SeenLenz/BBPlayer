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
        public List<String> SupFormats = new List<String> { ".mp3", ".wav", ".aiff", ".ogg" };

        private int _id = 0;

        public int ID
        {
            get { return _id; }
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
        public List<String> Files = new List<String> { };
        private List<Song> Songs = new List<Song>();

        //This section contains the properties for MainWindow required for BackgroundTask
        ConcurrentQueue<string[]> MessageQueue = new ConcurrentQueue<string[]>();
        ConcurrentDictionary<int, Song> MediaLibrary = new ConcurrentDictionary<int, Song>();
        ConcurrentDictionary<string, FileSystemWatcher> Watchers = new ConcurrentDictionary<string, FileSystemWatcher>();
        bool FileThreadRunning = true;

        public MainWindow()
        {
            BinaryFormatter formatter = new BinaryFormatter();
            using (Stream stream = System.IO.File.Open("./Data/Folders.bin", FileMode.Open))
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

                BinaryFormatter formatter = new BinaryFormatter();
                using (Stream stream = System.IO.File.Open("./Data/Folders.bin", FileMode.Create))
                {
                    formatter.Serialize(stream, this.Folders);
                }

                Thread.Sleep(200);
            }
        }

        private void DirectoryEventSub(string path)
        {
            FileSystemWatcher watcher = new FileSystemWatcher(path);

            watcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName;

            watcher.Created += DirectoryEventCreated;
            watcher.Deleted += DirectoryEventDeleted;

            this.Watchers.TryAdd(path, watcher);
        }

        private void DirectoryEventCreated(object source, FileSystemEventArgs e)
        {
            MediaLibrary.TryAdd(this.ID, new Song(e.FullPath));
        }

        private void DirectoryEventDeleted(object source, FileSystemEventArgs e)
        {
            foreach (var entry in MediaLibrary)
            {
                if (entry.Value.Path == e.FullPath)
                {
                    MediaLibrary.TryRemove(entry.Key, out _);
                }
            }
        }

        private void DirectoryEventModify(string path)
        {

        }

        private void ParseFiles()
        {
            foreach (var file in this.Files)
            {
                MediaLibrary.TryAdd(this.ID, new Song(file));
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
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            foreach (var entry in this.MediaLibrary)
            {
                Directories.Text += $"Name: {entry.Value.Title}\nTrack: {entry.Value.Track}\nYear: {entry.Value.Year}\nGenre: {entry.Value.Genre}\nAlbum: {entry.Value.Album}\nArtist: {entry.Value.Artist}\nDisc: {entry.Value.Disc}\nDuration: {entry.Value.Duration}\nPath: {entry.Value.Path}\n";
            }
        }
    }
}