using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using TagLib;
using System.Threading.Tasks;

namespace BBPlayer.Classes
{
    [Serializable]
    public class Song : ObservableCollection<Song>
    {
        public TimeSpan Duration { get; set; }
        public string Title { get; set; }
        public string Path { get; set; }
        public string Artist { get; set; }
        public string Album { get; set; }
        public string Year { get; set; }
        public string Genre { get; set; }
        public string Track { get; set; }
        public string Disc { get; set; }
        public int ID { get; set; }
        public string FileName { get; set; }
        public int Clicks { get; set; }
        public Song(string Path, int ID)
        {
            TagLib.File Raw = TagLib.File.Create(Path);
            if (Raw.Tag != null)
            {
                this.Path = Path;
                this.Duration = Raw.Properties.Duration;
                this.Title = Raw.Tag.Title ?? Path.Split(@"\").Last().Split(".").First();
                this.FileName = Path.Split(@"\").Last();
                this.Artist = Raw.Tag.FirstPerformer ?? "Unknown Artis";
                this.Album = Raw.Tag.Album ?? "Unknown Album";
                this.Year = Convert.ToString(Raw.Tag.Year);
                this.Genre = Raw.Tag.FirstGenre ?? "Unknown Genre";
                this.Track = Convert.ToString(Raw.Tag.Track);
                this.Disc = Convert.ToString(Raw.Tag.Disc);
                this.Clicks = 0;
                this.ID = ID;
            }
        }
    }
}
