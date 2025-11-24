using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO.Ports;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using wpfGMTraceability.Helpers;
using wpfGMTraceability.Managers;
using wpfGMTraceability.Models;
using wpfGMTraceability.Views;

namespace wpfGMTraceability.UserControls
{
    /// <summary>
    /// Interaction logic for TraceType3Control.xaml
    /// </summary>
    public partial class TraceType3Control : UserControl
    {
        #region Inicialización y carga
        private SerialWriterReader writer;
        public event Action<string> StationTitle;
        StationData BOMInventoryData;
        private SerialPortSession _session;
        ObservableCollection<ScanLogItem> logItems = new ObservableCollection<ScanLogItem>();
        DispatcherTimer cleanTimer;
        private readonly SemaphoreSlim _scanLock = new SemaphoreSlim(1, 1);
        public event EventHandler ShowLoadOverlay;
        public event EventHandler HideLoadOverlay;
        private List<string> scanList = new List<string>();
        private int CompCount = 0;
        private int Comp = 0;
        string sLastData = "";
        private bool reloadInit = false;
        public TraceType3Control()
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
        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            cleanTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(60)
            };
            cleanTimer.Tick += (s, eE) => CleanLogs();
            cleanTimer.Start();
            LoadInit();
        }
        private void LoadInit()
        {
            var window = Window.GetWindow(this) as IMainWindowHost;
            window?.SetWindowTitle("Nuevo título desde el UserControl");
            lbLog.ItemsSource = logItems;
            _ = LoadBOMDataAsync();
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
            if (reloadInit) { LoadInit(); reloadInit = false; }
            Dispatcher.Invoke(() =>
            {
                if (txtScanCode.Text == "") { DrawCardWithSerial(); }
                txtScanCode.Text = $"Escaneado: {data}";
                txtLastScan.Text = $@"Último Escaneo: {sLastData.Replace("Escaneado:", "")}";
                txtCompCount.Text = $@"{CompCount}/{Comp}";
            });
            DoProcess(data);
        }
        #endregion

        #region Funciones de negocio / lógica principal
        private async void DoProcess(string serial)
        {
            var ventana = Window.GetWindow(this) as MainWindow;
            try
            {
                sLastData = serial;
                ShowLoadOverlay?.Invoke(this, EventArgs.Empty);
                string serialclean = serial.Replace("\r", "").Replace("\n", "");
                if (!scanList.Contains(serialclean))
                {
                    byte evalSerialAPI = await CheckSerialNumberAsync(serialclean);
                    if (evalSerialAPI == 1)
                    {
                        scanList.Add(serialclean);
                        CompCount++;
                        txtCompCount.Text = $@"{CompCount}/{Comp}";
                        DrawCardWithSerial(CompCount, serialclean);
                        ventana?.MostrarOverlay(false);
                    }
                    else
                    {
                        if (CompCount == 0) { txtCompCount.Text = ""; }
                        reloadInit = true;
                        ventana?.MostrarOverlay(false);
                        return;
                    }
                }
                else
                {
                    Dispatcher.Invoke(() => AddLog("[SERIAL CHECK]", serial, "-", "-", "Serie ya escaneada!", "Warning"));
                    ventana?.MostrarOverlay(false);
                    return;
                }

                if (CompCount == Comp)
                {
                    ventana?.MostrarOverlay(true);
                    var respuesta = await writer.WriteAndWaitForPassOrResetAsync("OK", overallTimeoutMs: null);
                    if (string.Equals(respuesta, "PASS", StringComparison.OrdinalIgnoreCase))
                    {
                        //*****Escaneo de etiqueta despues de la respuesta del Dynalab
                        _session.ReleaseOwner(this);
                        var modal = new TraceType3LabelScanWindow(_session);
                        modal.Owner = Window.GetWindow(this);
                        bool? resultado = modal.ShowDialog();
                        _session.AssignOwner(this, OnSerialData);
                        //**Valor retornado, habria qeu validarlo en la ventana del escaneo de la etiqueta
                        string valor = modal.LabelScanCode;
                        if (resultado == true)
                        {
                            //***Hacer el insert
                            var dict = new Dictionary<string, object>();
                            string idx = "";
                            for (int i = 1; i <= 10; i++)
                            {
                                if (i == 1) { idx = ""; } else { idx = i.ToString(); }
                                dict[$"SN{idx}"] = scanList.ElementAtOrDefault(i - 1);
                            }
                            dict["SN10"] = valor;
                            dict["Status"] = "PASS";

                            string jsonFinal = JsonConvert.SerializeObject(dict, Formatting.None);
                            var resInsert = await ApiCalls.PostAPISerialMultiInsert(jsonFinal);

                            if (resInsert.statusCode == (int)HttpStatusCode.OK)
                            {
                                Dispatcher.Invoke(() => AddLog("[API INSERT]", "", "MULTI INSERT OK", resInsert.statusCode.ToString().Trim(), null, "OK"));
                            }
                            else
                            {
                                Dispatcher.Invoke(() => AddLog("[API INSERT]", "", "INSERT FALLÓ", resInsert.statusCode.ToString().Trim(), null, "Error"));
                            }
                            scanList.Clear();
                            txtScanCode.Text = "";
                            txtLastScan.Text = $@"Último Escaneo: {sLastData.Replace("Escaneado:", "")}";
                            txtCompCount.Text = "";
                            CompCount = 0;
                        }
                        ventana?.MostrarOverlay(false);
                        //**** Si no pasa (que el Dynalab no arroje el PASS)
                    }
                    else if (string.Equals(respuesta, "RESET", StringComparison.OrdinalIgnoreCase))
                    {
                        Dispatcher.Invoke(() => AddLog("[PLC RESET]", serial, "RESET RECIBIDO", "-", null, "SystemInfo", true));
                        RestartApp();
                    }
                    else
                    {
                        //***Si Dynalab no manda señal ** validar
                    }
                }
            }
            catch (Exception Ex)
            {
                Dispatcher.Invoke(() => AddLog("[SYSTEM ERROR]", serial, "-", "-", Ex.Message, "SystemError"));
                ventana?.MostrarOverlay(false);
            }
        }
        private async Task<byte> CheckSerialNumberAsync(string serial)
        {
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
                        AddLog("[SERIAL CHECK]", serial, Res, result.statusCode.ToString().Trim(), null, ResLogType)
                    );

                    if (Res == "OK")
                    {
                        return (byte)1;
                    }
                    else if (Res == "NO_OK")
                    {
                        Dispatcher.Invoke(() => AddLog("[SERIAL CHECK]", serial, "NO_OK", result.statusCode.ToString().Trim(), null, "Error"));
                        return (byte)0;
                    }
                    else
                    {
                        Dispatcher.Invoke(() => AddLog("[SERIAL CHECK]", serial, "NO_RESPONSE", result.statusCode.ToString().Trim(), $" {result.content.ToString().Substring(0, 64)}", "SystemError"));
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
                Dispatcher.Invoke(() => AddLog("[SERIAL CHECK]", serial, "NO_RESPONSE", "", $" {Ex.ToString()}", "SystemError"));
                return (byte)0;
            }
        }
        public async Task LoadBOMDataAsync()
        {
            BOMInventoryData = await ApiCalls.GetStationDataAsync();
            try
            {
                StationTitle?.Invoke(BOMInventoryData.Station.ToString());
                dgBOM.ItemsSource = BOMInventoryData.Parts;
                Comp = BOMInventoryData.Comp;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }        
        #endregion

        #region Liberación de recursos
        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            _session.Dispose();
            writer.ClosePort();
        }
        #endregion

        #region Logging y diagnóstico
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
        private void DrawCardWithSerial(int numItem = -1, string serial = "")
        {
            switch (numItem)
            {
                case 1:
                    crdComp1.Visibility = Visibility.Visible;
                    txtComp1.Text = serial;
                    break;
                case 2:
                    crdComp2.Visibility = Visibility.Visible;
                    txtComp2.Text = serial;
                    break;
                case 3:
                    crdComp3.Visibility = Visibility.Visible;
                    txtComp3.Text = serial;
                    break;
                case 4:
                    crdComp4.Visibility = Visibility.Visible;
                    txtComp4.Text = serial;
                    break;
                case 5:
                    crdComp5.Visibility = Visibility.Visible;
                    txtComp5.Text = serial;
                    break;
                case 6:
                    crdComp6.Visibility = Visibility.Visible;
                    txtComp6.Text = serial;
                    break;
                case 7:
                    crdComp7.Visibility = Visibility.Visible;
                    txtComp7.Text = serial;
                    break;
                case 8:
                    crdComp8.Visibility = Visibility.Visible;
                    txtComp8.Text = serial;
                    break;
                case 9:
                    crdComp9.Visibility = Visibility.Visible;
                    txtComp9.Text = serial;
                    break;
                case 10:
                    crdComp1.Visibility = Visibility.Visible;
                    txtComp10.Text = serial;
                    break;
                default:
                    crdComp1.Visibility = Visibility.Hidden;
                    crdComp2.Visibility = Visibility.Hidden;
                    crdComp3.Visibility = Visibility.Hidden;
                    crdComp4.Visibility = Visibility.Hidden;
                    crdComp5.Visibility = Visibility.Hidden;
                    crdComp6.Visibility = Visibility.Hidden;
                    crdComp7.Visibility = Visibility.Hidden;
                    crdComp8.Visibility = Visibility.Hidden;
                    crdComp9.Visibility = Visibility.Hidden;
                    crdComp10.Visibility = Visibility.Hidden;

                    txtComp1.Text = "";
                    txtComp2.Text = "";
                    txtComp3.Text = "";
                    txtComp4.Text = "";
                    txtComp5.Text = "";
                    txtComp6.Text = "";
                    txtComp7.Text = "";
                    txtComp8.Text = "";
                    txtComp9.Text = "";
                    txtComp10.Text = "";
                    break;
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
    }
}
