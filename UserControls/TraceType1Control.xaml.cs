using LibVLCSharp.Shared;
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
                txtLastScan.Text = $@"Último Escaneo: {sLastData.Replace("Escaneado:", "").Trim()}";
                txtScanCode.Text = $"Escaneado: {data.Trim()}";
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
                        AddLog("[SERIAL CHECK]",serial,Res,result.statusCode.ToString().Trim(),null,ResLogType)
                    );

                    string serialclean = serial.Replace("\r", "").Replace("\n", "");

                    if (Res == "OK")
                    {
                        ShowLoadOverlay?.Invoke(this, EventArgs.Empty);
                        var respuesta = await writer.WriteAndWaitForPassOrResetAsync(
                            "OK",
                            overallTimeoutMs: null
                        );
                        HideLoadOverlay?.Invoke(this, EventArgs.Empty);

                        if (string.Equals(respuesta, "PASS", StringComparison.OrdinalIgnoreCase))
                        {
                            var jsonEntry = new
                            {
                                SN = serialclean,
                                Status = "PASS",
                            };

                            string jsonFinal = JsonConvert.SerializeObject(jsonEntry, Formatting.None);
                            var resInsert = await ApiCalls.PostAPIPASSInsert(jsonFinal);

                            if (resInsert.statusCode == (int)HttpStatusCode.OK)
                            {
                                Dispatcher.Invoke(() =>
                                    AddLog("[API INSERT]",serialclean,"INSERT OK",resInsert.statusCode.ToString().Trim(), null, "OK")
                                );
                            }
                            else
                            {
                                Dispatcher.Invoke(() =>
                                    AddLog("[API INSERT]", serialclean, "INSERT FALLÓ", resInsert.statusCode.ToString().Trim(), null, "Error")
                                );
                            }
                        }
                        else if (string.Equals(respuesta, "RESET", StringComparison.OrdinalIgnoreCase))
                        {
                            Dispatcher.Invoke(() =>
                                AddLog("[PLC RESET]", serialclean, "RESET RECIBIDO", "-", null, "SystemInfo", true)
                            );

                            RestartApp();
                        }
                        else
                        {
                            Dispatcher.Invoke(() =>
                                AddLog("[API INSERT]", serialclean, "-", "-", "Sin respuesta válida (PASS/RESET) desde el equipo", "Error")
                            );
                        }
                    }
                    else if (Res == "NO_OK")
                    {
                        await writer.WriteAsync($"{Res}\n");
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
                        AddLog("[SERIAL CHECK]", serial,"NO_RESPONSE",result.statusCode.ToString().Trim(), null, "SystemError"));

                    await writer.WriteAsync("NO_RESPONSE\n");
                }
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() => AddLog("[SYSTEM ERROR]", serial, "-", "-",ex.Message, "SystemError"));
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
        public void AddLog(string titleItem, string serial, string apiResponse, string apiStatus, string mensaje, string tipo, bool persistente = false)
        {
            var nuevoLog = new ScanLogItem
            {
                Title = titleItem,
                Serial = serial,
                APIResponse = apiResponse,
                APIStatus = apiStatus,
                Msj = mensaje,
                MsjType = tipo,
                Timestamp = DateTime.Now,
                Persistent = persistente
            };

            logItems.Add(nuevoLog);
            lbLog.ScrollIntoView(nuevoLog);
        }
        #endregion

        #region Utilidades
        private void RestartApp()
        {
            try
            {
                var exePath = System.Reflection.Assembly.GetEntryAssembly()?.Location
                              ?? Application.ResourceAssembly.Location;

                System.Diagnostics.Process.Start(exePath);
                Application.Current.Shutdown();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al reiniciar la aplicación: {ex.Message}", "Restart Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        #endregion
    }
}