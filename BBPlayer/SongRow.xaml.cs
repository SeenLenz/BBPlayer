using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
namespace BBPlayer
{
    /// <summary>
    /// Interaction logic for SongRow.xaml
    /// </summary>
    public partial class SongRow : UserControl, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        private string _SongTitle;

        public string SongTitle
        {
            get { return _SongTitle; }
            set
            {
                _SongTitle = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("SongTitle"));
            }
        }

        private string _IsGridVisible;

        public string IsGridVisible
        {
            get { return _IsGridVisible; }
            set
            {
                _IsGridVisible = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("IsGridVisible"));
            }
        }

        private string _SongArtist;

        public string SongArtist
        {
            get { return _SongArtist; }
            set
            {
                _SongArtist = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("SongArtist"));
            }
        }
        private string _SongYear;

        public string SongYear
        {
            get { return _SongYear; }
            set
            {
                _SongYear = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("SongYear"));
            }
        }
        private string _SongGenre;

        public string SongGenre
        {
            get { return _SongGenre; }
            set
            {
                _SongGenre = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("SongGenre"));
            }
        }
        private string _SongDuration;

        public string SongDuration
        {
            get { return _SongDuration; }
            set
            {
                _SongDuration = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("SongDuration"));
            }

        }

        public SongRow()
        {
            this.DataContext = this;
            InitializeComponent();
        }
    }
}
