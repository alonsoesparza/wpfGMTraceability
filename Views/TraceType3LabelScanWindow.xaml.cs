using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO.Ports;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
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
using wpfGMTraceability.Managers;
using wpfGMTraceability.Models;

namespace wpfGMTraceability.Views
{
    /// <summary>
    /// Interaction logic for TraceType3LabelScanWindow.xaml
    /// </summary>
    public partial class TraceType3LabelScanWindow : Window
    {
        #region Inicialización y carga
        public string LabelScanCode { get; private set; }
        private readonly SerialPortSession _session;
        public event EventHandler ShowLoadOverlay;
        public event EventHandler HideLoadOverlay;
        ObservableCollection<ScanLogItem> logItems = new ObservableCollection<ScanLogItem>();
        public TraceType3LabelScanWindow(SerialPortSession session)
        {
            InitializeComponent();

            SerialPortConfig _configScannerPort;
            var jsonScannerPort = System.IO.File.ReadAllText(SettingsManager.ConfigPortsFilePath);
            _configScannerPort = JsonConvert.DeserializeObject<SerialPortConfig>(jsonScannerPort);

            SerialPortConfig _configLabelPort;
            var jsonLabelPort = System.IO.File.ReadAllText(SettingsManager.ConfigLabelPortsFilePath);
            _configLabelPort = JsonConvert.DeserializeObject<SerialPortConfig>(jsonLabelPort);

            if(_configScannerPort.Port == _configLabelPort.Port)
            {
                _session = session;
                _session.AssignOwner(this, OnModalData);
            }
            else
            {
                _session = new SerialPortSession(_configLabelPort.Port, _configLabelPort.BaudRate, _configLabelPort.Parity, _configLabelPort.DataBits, _configLabelPort.StopBits);
                _session.AssignOwner(this, OnModalData);
                _session.Open();
            }
        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            LabelScanCode = "";
        }
        #endregion
        #region Eventos del sistema
        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            _session.ReleaseOwner(this);
            this.Close();
        }
        #endregion
        #region Eventos de comunicación
        public void OnModalData(object sender, string data)
        {
            //Console.WriteLine($"🪟 [Modal] RXX: {data}");
            Dispatcher.Invoke(() =>
            {
                string sLastData = "";
                sLastData = txtScanCode.Text;
                txtScanCode.Text = $"{data}";
            });
            FinishAction();
        }
        #endregion
        private async void FinishAction()
        {
            txtError.Visibility = Visibility.Collapsed;
            txtErrorMsg.Visibility = Visibility.Collapsed;
            LabelScanCode = txtScanCode.Text.Replace("\r", "").Replace("\n", "").Trim();       

            byte evalSerialAPI = await CheckSerialNumberAsync(LabelScanCode);
            if (evalSerialAPI == 1)
            {
                this.DialogResult = true;
                return;
            }
            else
            {
                txtError.Visibility = Visibility.Visible;
                txtErrorMsg.Visibility = Visibility.Visible;
            }
        }
        private async Task<byte> CheckSerialNumberAsync(string serial)
        {
            try
            {
                var result = await ApiCalls.GetFromApiAsync($@"{SettingsManager.APIUrlCheckSerial}{serial.Trim()}");
                string Res = "NO_RESPONSE";

                var content = result.content ?? string.Empty;
                if (!string.IsNullOrWhiteSpace(content))
                {
                    switch (content.Trim().Replace("\"", ""))
                    {
                        case "0":
                            Res = "NO_OK";
                            break;
                        case "1":
                            Res = "OK";
                            break;
                        default:
                            Res = "NO_RESPONSE";
                            break;
                    }

                    Dispatcher.Invoke(() => AddLog(result.content.ToString()));

                    if (Res == "OK")
                    {
                        return (byte)1;
                    }
                    else if (Res == "NO_OK")
                    {
                        Dispatcher.Invoke(() => AddLog(result.content.ToString()));
                        return (byte)0;
                    }
                    else
                    {
                        Dispatcher.Invoke(() => AddLog(result.content.ToString()));
                        return (byte)0;
                    }
                }
                else
                {
                    return (byte)0;
                }
            }
            catch (Exception Ex)
            {
                Dispatcher.Invoke(() => AddLog(Ex.ToString()));
                return (byte)0;
            }
        }
        public void AddLog(string msj)
        {
            txtErrorMsg.Text = msj;
        }
    }
}
