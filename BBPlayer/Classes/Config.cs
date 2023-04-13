using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace BBPlayer.Classes
{
    [Serializable]
    public class Config
    {
        public enum Themes
        {
            Dark,
            Light,
            Burgundy,
            Red,
            Green,
            Blue,
            Yellow,
            Orange,
        }

   
        public Themes currentTheme { get; set; }
        public bool isDebug { get; set; }
        public bool isExperimental { get; set; }

        public int uid { get; set; }
        public MainWindow.SortTypes CurrentSort;

        public Config()
        {
            this.currentTheme = Themes.Dark;
            this.isDebug = false;
            this.isExperimental = false;
            this.uid = 0;
            this.CurrentSort = BBPlayer.MainWindow.SortTypes.DateAdded;
        }
    }
}
