using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BBPlayer.Classes
{
    [Serializable]
    public class Album
    {
        public string ImagePath { get; set; }
        public string Title { get; set; }
        public string Artist { get; set; }
        public string Genre { get; set; }
        public int Length { get; set; }
        public int ID { get; set; }
        public List<string> SongIdList { get; set; }

        public Album(int ID)
        {
            this.ID = ID;
            this.Title = $"Album #{ID}";
            this.Length = 0;
            this.ImagePath = @"/Path/To/Image";
            this.Genre = @"Unknown Genre";
            this.Artist = @"Unknown Artist";
            this.SongIdList = new List<string>();
        }
        public void AddSong() { }
        public void RemoveSong() { }
    }
}
