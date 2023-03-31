using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BBPlayer.Classes
{
    [Serializable]
    public class Playlist
    {
        public string Title { get; set; }
        public string ImagePath { get; set; }
        public string Description { get; set; }
        public int ID { get; set; }
        public List<string> SongIdList { get; set; }

        public Playlist(int ID)
        {
            this.ID = ID;
            this.Title = $"My Playlist #{ID}";
            this.ImagePath = @"/Path/To/Image";
            this.Description = @"This is a playlist";
            this.SongIdList = new List<string>();
        }
    }
}
