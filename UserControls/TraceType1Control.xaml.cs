using System;
using System.Collections.ObjectModel;
using System.IO.Ports;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using wpfGMTraceability.Helpers;
using wpfGMTraceability.Managers;
using wpfGMTraceability.Models;
using wpfGMTraceability.Views;

namespace wpfGMTraceability.UserControls
{
    /// <summary>
    /// Interaction logic for TraceType1Control.xaml
    /// </summary>
    public partial class TraceType1Control : UserControl
    {
        private readonly DualSerialManager _serialManager;
        ObservableCollection<ScanLogItem> logItems = new ObservableCollection<ScanLogItem>();
        DispatcherTimer cleanTimer;
        public TraceType1Control(DualSerialManager _dualManager)
        {
            InitializeComponent();
            _serialManager = _dualManager;
            _serialManager.DataReceived += SerialManager_DataReceived;
        }
        private void TraceType1_Control_Loaded(object sender, RoutedEventArgs e)
        {
            //txtStatusCard.Text = App.SerialPortStatusMessage;
            lbLog.ItemsSource = logItems;

            cleanTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(60)
            };
            cleanTimer.Tick += (s, eE) => CleanLogs();
            cleanTimer.Start();
        }
        private void TraceType1_Control_Unloaded(object sender, RoutedEventArgs e)
        {
            _serialManager.DataReceived -= SerialManager_DataReceived;
        }
        private void SerialManager_DataReceived(object sender, string data)
        {
            Dispatcher.Invoke(() =>
            {
                string sLastData = "";
                sLastData = txtScanCode.Text;
                txtLastScan.Text = $@"Último Escaneo: {sLastData.Replace("Escaneado:","")}";
                txtScanCode.Text = $"Escaneado: {data}";
                CheckSerialNumber(data);
            });
        }
        private void BtnPlayVideo_Click(object sender, RoutedEventArgs e)
        {
            var ventana = Window.GetWindow(this) as MainWindow;
            ventana?.MostrarOverlay(true);

            var login = new VideoWindow();
            login.Owner = ventana;
            login.ShowDialog();

            ventana?.MostrarOverlay(false);
        }
        #region Local Methods
        private async void CheckSerialNumber(string serial)
        {
            var APIresponse = "";
            try
            {
                APIresponse = await ApiCheckSerialService.GetFromApiAsync<string>($@"{App.APIUrlCheckSerial}{serial}");
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() => AddLog($"Serie Validada", $"{serial} / {ex.Message}"));
            }
            if (APIresponse != null)
            {
                string Res = "NO_RESPONSE";
                switch (Convert.ToString(APIresponse))
                {
                    case "0":
                        Res = $"NO_OK{Environment.NewLine}";
                        break;

                    case "1":
                        Res = $"OK{Environment.NewLine}";
                        break;

                    default:
                        Res = $"NO_RESPONSE{Environment.NewLine}";
                        break;
                }
                _serialManager.Send(Res);
                Dispatcher.Invoke(() => AddLog($"Serie Validada", $"{serial} / {Res}"));

            }
            else {
                Dispatcher.Invoke(() => AddLog($@"Error ", $"{serial} Error: "));
            }
        }
        public void AddLog(string mensaje, string tipo, bool persistente = false)
        {
            var nuevoLog = new ScanLogItem
            {
                Msj = mensaje,
                MsjType = tipo,
                Timestamp = DateTime.Now,
                Persistent = persistente
            };

            logItems.Add(nuevoLog);
            lbLog.ScrollIntoView(nuevoLog);
        }
        private void CleanLogs()
        {
            var haceUnMinuto = DateTime.Now.AddMinutes(-1);
            var recientes = logItems.Where(log => log.Persistent || log.Timestamp >= haceUnMinuto).ToList();

            logItems.Clear();
            foreach (var log in recientes)
                logItems.Add(log);
        }
        #endregion
    }
}
