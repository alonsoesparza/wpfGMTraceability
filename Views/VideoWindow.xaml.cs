using Microsoft.Web.WebView2.Core;
using Newtonsoft.Json;
using System.Threading.Tasks;
using System.Windows;
using wpfGMTraceability.Helpers;

namespace wpfGMTraceability.Views
{
    /// <summary>
    /// Interaction logic for VideoWindow.xaml
    /// </summary>
    public partial class VideoWindow : Window
    {
        public VideoWindow()
        {
            InitializeComponent();
        }
        private async void Video_Window_Loaded(object sender, RoutedEventArgs e)
        {
            await webView.EnsureCoreWebView2Async();
            webView.CoreWebView2.Settings.IsWebMessageEnabled = true;
            webView.AllowExternalDrop = true;

            SettingsConfig _config;
            var json = System.IO.File.ReadAllText(SettingsManager.ConfigSettingsFilePath);
            _config = JsonConvert.DeserializeObject<SettingsConfig>(json);
            string urlVideo = _config.VideoURL.ToString();

            webView.CoreWebView2.Navigate(urlVideo);
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
