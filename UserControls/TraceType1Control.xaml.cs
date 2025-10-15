using Newtonsoft.Json;
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
        #region Inicialización y carga
        private SerialWriter writer;
        private SerialPortSession _session;
        ObservableCollection<ScanLogItem> logItems = new ObservableCollection<ScanLogItem>();
        DispatcherTimer cleanTimer;
        public event EventHandler ShowLoadOverlay;
        public event EventHandler HideLoadOverlay;
        public TraceType1Control()
        {
            InitializeComponent();
            SerialPortConfig _config;

            var json = System.IO.File.ReadAllText(SettingsManager.ConfigPortsFilePath);
            _config = JsonConvert.DeserializeObject<SerialPortConfig>(json);

            _session = new SerialPortSession(_config.Port, _config.BaudRate, _config.Parity, _config.DataBits, _config.StopBits);
            _session.AssignOwner(this, OnSerialData);
            _session.Open();

            SerialPortConfig _Wconfig;
            var Wjson = System.IO.File.ReadAllText(SettingsManager.ConfigWritePortsFilePath);
            _Wconfig = JsonConvert.DeserializeObject<SerialPortConfig>(Wjson);

            writer = new SerialWriter(_Wconfig.Port, _Wconfig.BaudRate);
            writer.OpenPort();
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
        #endregion

        #region Eventos del sistema
        private void BtnPlayVideo_Click(object sender, RoutedEventArgs e)
        {
            var ventana = Window.GetWindow(this) as MainWindow;
            ventana?.MostrarOverlay(true);

            var login = new VideoWindow();
            login.Owner = ventana;
            login.ShowDialog();

            ventana?.MostrarOverlay(false);
        }
        #endregion
        
        #region Eventos de comunicación
        private void OnSerialData(object sender, string data)
        {            
            Dispatcher.Invoke(() =>
            {
                string sLastData = "";
                sLastData = txtScanCode.Text;
                txtLastScan.Text = $@"Último Escaneo: {sLastData.Replace("Escaneado:", "")}";
                txtScanCode.Text = $"Escaneado: {data}";
                CheckSerialNumber(data);
            });
        }
        #endregion
        
        #region Funciones de negocio / lógica principal
        private async void CheckSerialNumber(string serial)
        {
            try
            {
                ShowLoadOverlay?.Invoke(this, EventArgs.Empty);
                var result = await ApiCalls.GetFromApiAsync($@"{SettingsManager.APIUrlCheckSerial}{serial.Trim()}");
                HideLoadOverlay?.Invoke(this, EventArgs.Empty);
                string Res = "NO_RESPONSE";
                if (result.content != null)
                {                    
                    switch (Convert.ToString(result.content)?.Trim().Replace("\"", ""))
                    {
                        case "0":
                            Res = $"NO_OK";
                            break;

                        case "1":
                            Res = $"OK";
                            break;

                        default:
                            Res = $"NO_RESPONSE";
                            break;
                    }
                   
                    Dispatcher.Invoke(() => AddLog($"Serie Validada {serial} / {Res}{Environment.NewLine} / {result.statusCode}", "OK"));
                    writer.WriteData($"{Res}{Environment.NewLine}");
                }
                else
                {
                    Dispatcher.Invoke(() => AddLog($@"Error {serial} / {Res}{Environment.NewLine} / {result.statusCode}", "Error"));
                    writer.WriteData($"{Res}{Environment.NewLine}");
                }
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() => AddLog($@"Serie Validada {serial} / {ex.Message}", "Error"));
            }
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
        
        #region Liberación de recursos
        private void TraceType1_Control_Unloaded(object sender, RoutedEventArgs e)
        {
            _session.Dispose();
            writer.ClosePort();
        }
        #endregion
        
        #region Logging y diagnóstico
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
        #endregion
    }
}
