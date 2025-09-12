using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Ports;
using System.Linq;
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
    /// Interaction logic for RequestBoxWindow.xaml
    /// </summary>
    public partial class RequestBoxWindow : Window
    {
        #region Inicialización y carga
        StationData BOMInventoryData;
        List<object> MissingPart;
        string FGSerial;
        private readonly SerialPortSession _session;
        public event EventHandler ShowLoadOverlay;
        public event EventHandler HideLoadOverlay;
        ObservableCollection<ScanLogItem> logItems = new ObservableCollection<ScanLogItem>();
        public RequestBoxWindow(SerialPortSession session, List<object> MissingPartData, StationData bOMInventoryData, string fGSerial)
        {
            InitializeComponent();

            MissingPart = MissingPartData;
            BOMInventoryData = bOMInventoryData;
            _session = session;
            _session.AssignOwner(this, OnModalData);
            FGSerial = fGSerial;
        }
        private void RequestBox_Window_Loaded(object sender, RoutedEventArgs e)
        {
            lbMissingParts.ItemsSource = MissingPart.ToList();
            lbLog.ItemsSource = logItems;
        }
        #endregion

        #region Eventos del sistema
        private void BtnClose_Click(object sender, RoutedEventArgs e)
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
                ProcessRMSerialNumber(data);
            });
        }
        #endregion

        #region Funciones de negocio / lógica principal
        private async void ProcessRMSerialNumber(string RMserial)
        {
            int PipeStringPosition = RMserial.IndexOf('|');
            if (PipeStringPosition >= 0)
            {
                string[] SerialArr = RMserial.Split('|');
                bool IsPartInTheBOM = BOMInventoryData.Parts.Any(p => p.BomPart == SerialArr[2].ToString().Trim());
                if (IsPartInTheBOM)
                {
                    var jsonEntry = new
                    {
                        station_name = "TycoStationConnector",
                        payload = RMserial.Trim(),
                    };
                    string jsonFinal = JsonConvert.SerializeObject(jsonEntry, Formatting.None);

                    ShowLoadOverlay?.Invoke(this, EventArgs.Empty);
                    var result = await ApiCalls.PostAPIRequestBoxAsync(jsonFinal);
                    HideLoadOverlay?.Invoke(this, EventArgs.Empty);

                    string ResContent = result.content;
                    int StatusCode = result.statusCode;

                    string StatusMessage = HttpStatusHelper.GetStatusMessage(StatusCode);

                    if (result.content != null)
                    {
                        Dispatcher.Invoke(() => AddLog($"{RMserial} / {ResContent} / {StatusMessage}", "OK"));

                        var TypedList = MissingPart.OfType<MissingPartToBoxRequest>().ToList();

                        var leftPartsToRequestBox = TypedList
                            .Where(p => p.BomPart != SerialArr[2].ToString().Trim())
                            .ToList();

                        lbMissingParts.ItemsSource = leftPartsToRequestBox;
                    }
                    else {
                        //**** Mensaje de error, API no responde
                        Dispatcher.Invoke(() => AddLog($"{RMserial} / {ResContent} / {StatusMessage}", "ERROR"));
                    }
                }
                else {
                    //**** Mensaje de error, NP no esta dentro del BOM
                    Dispatcher.Invoke(() => AddLog($"El código escaneado no corresponde a ninguna parte faltante en el BOM, por favor intente de nuevo.", "ERROR"));
                }
            }
            else {
                //**** Mensaje de error, no es un serial válido
                Dispatcher.Invoke(() => AddLog($"El código escaneado no es válido, por favor intente de nuevo.", "ERROR"));
            }
        }
        #endregion

        #region Liberación de recursos

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
