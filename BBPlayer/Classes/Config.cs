using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BBPlayer.Classes
{
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

        public Config()
        {
            this.currentTheme = Themes.Dark;
            this.isDebug = false;
            this.isExperimental = false;
        }
    }
}
