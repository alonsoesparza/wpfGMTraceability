using Newtonsoft.Json;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using wpfGMTraceability.Helpers;
using wpfGMTraceability.Managers;
using wpfGMTraceability.Models;
using wpfGMTraceability.Views;

namespace wpfGMTraceability.UserControls
{
    public partial class TraceType1Control : UserControl, IOverlayAware
    {
        #region Inicialización y carga
        private SerialWriterReader writer;
        private SerialPortSession _session;
        ObservableCollection<ScanLogItem> logItems = new ObservableCollection<ScanLogItem>();
        DispatcherTimer cleanTimer;
        private readonly SemaphoreSlim _scanLock = new SemaphoreSlim(1, 1);
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

            writer = new SerialWriterReader(_Wconfig.Port, _Wconfig.BaudRate);
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

        #region Eventos UI
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

        #region Recepción Serial
        private void OnSerialData(object sender, string data)
        {
            Dispatcher.Invoke(() =>
            {
                string sLastData = txtScanCode.Text;
                txtLastScan.Text = $@"Último Escaneo: {sLastData.Replace("Escaneado:", "")}";
                txtScanCode.Text = $"Escaneado: {data}";
            });

            _ = CheckSerialNumberAsync(data); // ejecuta sin bloquear UI
        }
        #endregion

        #region Lógica principal
        private async Task CheckSerialNumberAsync(string serial)
        {
            await _scanLock.WaitAsync();
            try
            {
                var result = await ApiCalls.GetFromApiAsync($@"{SettingsManager.APIUrlCheckSerial}{serial.Trim()}");

                string Res = "NO_RESPONSE";
                string ResLogType = "Error";

                var content = result.content ?? string.Empty;
                if (!string.IsNullOrWhiteSpace(content))
                {
                    switch (content.Trim().Replace("\"", ""))
                    {
                        case "0":
                            Res = "NO_OK";
                            ResLogType = "Error";
                            break;
                        case "1":
                            Res = "OK";
                            ResLogType = "OK";
                            break;
                        default:
                            Res = "NO_RESPONSE";
                            ResLogType = "Error";
                            break;
                    }

                    Dispatcher.Invoke(() =>
                        AddLog("[SERIAL CHECK]",$"[Serie]: {serial}[API Response]: {Res}{Environment.NewLine}[API Status]: {result.statusCode}", ResLogType));

                    string serialclean = serial.Replace("\r", "").Replace("\n", "");

                    if (Res == "OK")
                    {
                        ShowLoadOverlay?.Invoke(this, EventArgs.Empty);

                        bool isPass = await writer.WriteAndWaitForAsync($"{Res}\n", "PASS", overallTimeoutMs: null);

                        HideLoadOverlay?.Invoke(this, EventArgs.Empty);

                        if (isPass)
                        {
                            var jsonEntry = new
                            {
                                SerialNumber = serialclean,
                                State = "OK",
                                Day = $@"{DateTime.Now:yyyy-MM-dd}",
                                Hour = DateTime.Now.ToString("HH:mm"),
                                C1 = "1",
                                C2 = "2",
                                C3 = "3",
                                C4 = "4",
                                C5 = "5",
                                C6 = "6",
                                C7 = "7",
                                C8 = "8",
                                C9 = "9",
                                C10 = "0"
                            };

                            string jsonFinal = JsonConvert.SerializeObject(jsonEntry, Formatting.None);
                            var resInsert = await ApiCalls.PostAPIPASSInsert(jsonFinal);

                            if (resInsert.statusCode == (int)HttpStatusCode.OK)
                            {
                                Dispatcher.Invoke(() =>
                                    AddLog("[API INSERT]",$"[Serie] {serialclean}{Environment.NewLine}[API Response]: INSERT OK{Environment.NewLine}[API Status]: {resInsert.statusCode}", "OK"));
                            }
                            else
                            {
                                Dispatcher.Invoke(() =>
                                    AddLog("[API INSERT]", $"Serie {serialclean}{Environment.NewLine}[API Response]: INSERT FALLÓ{Environment.NewLine}[API Status]:{resInsert.statusCode}", "Error"));
                            }
                        }
                        else
                        {
                            Dispatcher.Invoke(() =>
                                AddLog("[API INSERT]", $"[Serie]: {serialclean} / Esperando PASS del Arduino...", "Error"));
                        }
                    }
                    else if (Res == "NO_OK")
                    {
                        // NO_OK → no mostrar mensaje adicional
                        await writer.WriteAsync($"{Res}\n"); // opcional; quítalo si no deseas enviar nada al Arduino
                    }
                    else
                    {
                        // NO_RESPONSE u otros
                        await writer.WriteAsync($"{Res}\n");
                    }
                }
                else
                {
                    Dispatcher.Invoke(() =>
                        AddLog("[SERIAL CHECK]", $@"Error {serial} / NO_RESPONSE{Environment.NewLine} / {result.statusCode}", "SystemError"));

                    await writer.WriteAsync("NO_RESPONSE\n");
                }
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() => AddLog("[SYSTEM ERROR]", $@"[Serie Validada]: {serial} / {ex.Message}", "SystemError"));
            }
            finally
            {
                try { HideLoadOverlay?.Invoke(this, EventArgs.Empty); } catch { }
                _scanLock.Release();
            }
        }

        private void CleanLogs()
        {
            var haceUnMinuto = DateTime.Now.AddMinutes(-5);
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

        #region Logging
        public void AddLog(string titleItem,string mensaje, string tipo, bool persistente = false)
        {
            var nuevoLog = new ScanLogItem
            {
                Title = titleItem,
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