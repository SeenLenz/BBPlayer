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
using BBPlayer;

namespace BBPlayer
{
    /// <summary>
    /// Interaction logic for Playlists.xaml
    /// </summary>
    public partial class Playlists : Window
    {
        public Playlists()
        {
            InitializeComponent();
        }

        private void switchToLocalFiles(object sender, RoutedEventArgs e)
        {
            localfiles localfilespage= new localfiles();
            Application.Current.MainWindow.Content = localfilespage.Content;
        }

        private void SwitchToMainWindow(object sender, RoutedEventArgs e)
        {
            MainWindow mainWindow = new MainWindow();
            Application.Current.MainWindow.Content = mainWindow.Content;
        }
    }
}
