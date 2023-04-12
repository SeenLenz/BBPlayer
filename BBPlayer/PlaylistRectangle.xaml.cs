using System;
using System.Collections.Generic;
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
    /// Interaction logic for PlaylistRectangle.xaml
    /// </summary>
    public partial class PlaylistRectangle : UserControl
    {
        public PlaylistRectangle()
        {
            InitializeComponent();
        }
        void rectangle_MouseLeftButtonDown(object sender, RoutedEventArgs e)
        {
            Playlist playlistpage = new();
            Application.Current.MainWindow.Content = playlistpage.Content;
        }
    }
}
