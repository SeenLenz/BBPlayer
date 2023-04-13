﻿using System;
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
    /// Interaction logic for localfiles.xaml
    /// </summary>
    public partial class localfiles : Window
    {
        public localfiles()
        {
            InitializeComponent();
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
            if(clickcounter % 2 == 0)
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

        private void switchToPlaylists(object sender, RoutedEventArgs e)
        {
            Playlists playlistpage = new();
            Application.Current.MainWindow.Content = playlistpage.Content;
        }
    }
}