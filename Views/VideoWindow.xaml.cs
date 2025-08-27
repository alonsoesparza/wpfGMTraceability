using Microsoft.Web.WebView2.Core;
using System.Threading.Tasks;
using System.Windows;

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

            webView.CoreWebView2.Navigate(@"http://10.13.0.41:8080/slid/site");
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
