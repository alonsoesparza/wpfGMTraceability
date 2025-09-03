using System;
using System.Collections.ObjectModel;
using System.IO.Ports;
using System.Linq;
using System.Net;
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
    public partial class TraceType1Control : UserControl, IOverlayAware
    {
        private readonly DualSerialManager _serialManager;
        ObservableCollection<ScanLogItem> logItems = new ObservableCollection<ScanLogItem>();
        DispatcherTimer cleanTimer;

        public event EventHandler ShowLoadOverlay;
        public event EventHandler HideLoadOverlay;

        public TraceType1Control(DualSerialManager _dualManager)
        {
            InitializeComponent();
            _serialManager = _dualManager;
            _serialManager.DataReceived += SerialManager_DataReceived;
        }
        private void TraceType1_Control_Loaded(object sender, RoutedEventArgs e)
        {
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
            try
            {
                ShowLoadOverlay?.Invoke(this, EventArgs.Empty);
                var result = await ApiCheckSerialService.GetFromApiAsync($@"{SettingsManager.APIUrlCheckSerial}{serial.Trim()}");
                HideLoadOverlay?.Invoke(this, EventArgs.Empty);

                Console.WriteLine($"Status Code: {result.statusCode}");
                Console.WriteLine($"Content: {result.content}");

                string Res = "NO_RESPONSE";
                if (result.content != null)
                {                    
                    switch (Convert.ToString(result.content))
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
                    Dispatcher.Invoke(() => AddLog($"Serie Validada {serial} / {Res} / {result.statusCode}", "OK"));

                }
                else
                {
                    Dispatcher.Invoke(() => AddLog($@"Error {serial} / {Res} / {result.statusCode}", "Error"));
                }

            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() => AddLog($@"Serie Validada {serial} / {ex.Message}", "Error"));
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
