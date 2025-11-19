using MaterialDesignThemes.Wpf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Policy;
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
using wpfGMTraceability.Helpers;
using static System.Net.Mime.MediaTypeNames;

namespace wpfGMTraceability.Views
{
    /// <summary>
    /// Interaction logic for APICheckWindow.xaml
    /// </summary>
    public partial class APICheckWindow : Window
    {
        private static readonly HttpClient client = new HttpClient();
        private static bool DoContinue = true;
        public APICheckWindow()
        {
            InitializeComponent();
        }
        private void APICheck_Window_Loaded(object sender, RoutedEventArgs e)
        {
            txtAPISerialCheck.Text = SettingsManager.APIUrlCheckSerial;
            txtAPIInsert.Text = SettingsManager.APIPASSInsertUrl;
            txtAPILoadBOM.Text = SettingsManager.APILoadBOMUrl;
            txtAPISerialBoxRequest.Text = SettingsManager.APIRequestBoxUrl;
            txtAPISerialConsume.Text = SettingsManager.APIConsumeSerialUrl;
            txtAPIMultiInsert.Text = SettingsManager.APISerialMultiInsertUrl;
            _ = DoCheckAsync();
        }
        private async Task DoCheckAsync()
        {
            var APIurls = new[]
            {
                SettingsManager.APIUrlCheckSerial,
                SettingsManager.APIPASSInsertUrl,
                SettingsManager.APILoadBOMUrl,
                SettingsManager.APIRequestBoxUrl,
                SettingsManager.APIConsumeSerialUrl,
                SettingsManager.APISerialMultiInsertUrl
            };

            var icons = new[] { icnAPISerialCheck, icnAPIInsert, icnAPILoadBOM, icnAPISerialBoxRequest, icnAPISerialConsume, icnAPIMultiInsert };

            var tasks = APIurls.Select(CheckApiAsync);
            var results = await Task.WhenAll(tasks);

            for (int i = 0; i < APIurls.Length; i++)
            {
                bool result = await CheckApiAsync(APIurls[i]);
                if (!result){ DoContinue = false; }
                Dispatcher.Invoke(() =>
                {
                    icons[i].Visibility = Visibility.Visible;
                    icons[i].Kind = result ? MaterialDesignThemes.Wpf.PackIconKind.CloudCheck
                                           : MaterialDesignThemes.Wpf.PackIconKind.CloudOff;
                    icons[i].Foreground = result ? Brushes.Green : Brushes.Red;
                });
            }

            if (DoContinue)
            {
                this.Close();
            }
        }
        private static async Task<bool> CheckApiAsync(string url)
        {
            string sType = "";
            try
            {
                if(url == SettingsManager.APIUrlCheckSerial) { sType = "GETWITHCONTENT"; }
                if (url == SettingsManager.APIPASSInsertUrl) { sType = "POST"; }
                if (url == SettingsManager.APILoadBOMUrl) { sType = "GET"; }
                if (url == SettingsManager.APIRequestBoxUrl) { sType = "POST"; }
                if (url == SettingsManager.APIConsumeSerialUrl) { sType = "POST"; }
                if (url == SettingsManager.APISerialMultiInsertUrl) { sType = "POST"; }

                if (sType == "GET")
                {
                    var response = await client.GetAsync(url);
                    return response.IsSuccessStatusCode;
                }
                else if (sType == "POST")
                {
                    var response = await client.PostAsync(url, new StringContent(""));
                    bool isUp = response.StatusCode != HttpStatusCode.NotFound;
                    return isUp;
                }else if (sType == "GETWITHCONTENT") {
                    var request = new HttpRequestMessage(HttpMethod.Head, url);
                    var response = await client.SendAsync(request);
                    return response.IsSuccessStatusCode || (int)response.StatusCode < 500;
                } else {
                    return false;
                }
            }
            catch
            {
                return false;
            }
        }
        private void btnSaveAndReconnect_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
