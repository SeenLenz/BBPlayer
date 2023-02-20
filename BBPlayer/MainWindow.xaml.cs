using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using TagLib;
using System.Diagnostics;

namespace BBPlayer
{


    public partial class MainWindow : Window
    {
        public class Config
        {

        }
        public class Song
        {
            public Tag Tags { get; set; }

            public Song(String Path)
            {
                var temp = TagLib.File.Create($"{Path}");
                this.Tags = temp.Tag;

            }
        }

        public Config config;
        public String[] Folders = new string[] { };
        public List<String> Files = new List<String> { };
        public List<String> SupFormats = new List<String> { ".mp3", ".wav" };
        public List<Song> Songs { get; set; } = new List<Song>();
        public MainWindow()
        {

            InitializeComponent();
        }

        private void Add_Folder(object sender, RoutedEventArgs e)
        {
            var dlg = new CommonOpenFileDialog();
            dlg.IsFolderPicker = true;
            dlg.Multiselect = true;

            if (dlg.ShowDialog() == CommonFileDialogResult.Ok)
            {
                string[] selectedFolders = dlg.FileNames.ToArray();
                this.Folders = Folders.Concat(selectedFolders).ToArray();
                
                foreach (var folder in this.Folders)
                {
                    this.Files.AddRange(Directory.EnumerateFiles(folder, "*", SearchOption.AllDirectories)
                    .Where(item => SupFormats.Contains(Path.GetExtension(item).ToLower()))
                    .Select(item => Path.GetFullPath(item))
                    .ToList());
                }
                
                foreach (var file in this.Files)
                {
                    Songs.Add(new Song(file));
                }
                foreach (var song in this.Songs)
                {
                    Directories.Text = song.Tags.Title;
                }
            }
        }
    }
}