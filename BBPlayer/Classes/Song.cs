using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BBPlayer.Classes
{
    public class Song
    {
        public TimeSpan Duration { get; set; }
        public TagLib.File Raw { get; set; }
        public string Title { get; set; }
        public string Path { get; set; }
        public string Artist { get; set; }
        public string Album { get; set; }
        public string Year { get; set; }
        public string Genre { get; set; }
        public string Track { get; set; }
        public string Disc { get; set; }
        public int ID { get; set; }

        public Song(string Path)
        {
            this.Raw = TagLib.File.Create(Path);
            if (this.Raw.Tag != null)
            {
                this.Path = Path;
                this.Duration = this.Raw.Properties.Duration;
                this.Title = this.Raw.Tag.Title ?? Path.Split(@"\").Last().Split(".").First();
                this.Artist = this.Raw.Tag.FirstPerformer ?? "Unknown Artis";
                this.Album = this.Raw.Tag.Album ?? "Unknown Album";
                this.Year = Convert.ToString(this.Raw.Tag.Year);
                this.Genre = this.Raw.Tag.FirstGenre ?? "Unknown Genre";
                this.Track = Convert.ToString(this.Raw.Tag.Track);
                this.Disc = Convert.ToString(this.Raw.Tag.Disc);
                this.ID = ID;
            }
        }
    }
}
