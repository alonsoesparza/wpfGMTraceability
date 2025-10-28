using HtmlAgilityPack;
using LibVLCSharp.Shared;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using wpfGMTraceability.Helpers;

namespace wpfGMTraceability.Views
{
    public partial class VideoWindow : Window
    {
        public VideoWindow()
        {
            InitializeComponent();
        }

        private void VideoWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (File.Exists(SettingsManager.VideoFileName))
            {
                VidSlide.Source = new Uri(SettingsManager.VideoFileName, UriKind.Absolute);
                VidSlide.Position = TimeSpan.Zero;

                VidSlide.Play();

                VidSlide.MediaFailed += (s, ee) => MessageBox.Show("Error: " + ee.ErrorException.Message);
            }
            else
            {
                MessageBox.Show("Archivo no encontrado:\n" + SettingsManager.VideoFileName);
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            try { VidSlide.Stop(); } catch { }
            Close();
        }
    }
}
