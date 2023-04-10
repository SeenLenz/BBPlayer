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
using System.Windows.Shapes;

namespace BBPlayer
{
    /// <summary>
    /// Interaction logic for Playlist.xaml
    /// </summary>
    public partial class Playlist : Window
    {
        public Playlist()
        {
            InitializeComponent();
        }

        private void switchToLocalFiles(object sender, RoutedEventArgs e)
        {
            localfiles ablak = new localfiles();
            Application.Current.MainWindow.Content = ablak.Content;
        }
        private void switchToMainMenu(object sender, RoutedEventArgs e)
        {
            MainWindow mainWindow = new MainWindow();
            Application.Current.MainWindow.Content = mainWindow.Content;
        }
        int clickcounter = 1;
        private void bt_PlaySong_Click(object sender, RoutedEventArgs e)
        {
            clickcounter++;
            if (clickcounter % 2 == 0)
            {
                bt_PlaySong.Content = "||";
            }
            else
            {
                bt_PlaySong.Content = ">";
            }
        }
        private void closeWindow1(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}
