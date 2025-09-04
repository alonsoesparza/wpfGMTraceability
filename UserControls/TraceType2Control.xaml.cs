using MaterialDesignThemes.Wpf;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using wpfGMTraceability.Helpers;
using wpfGMTraceability.Managers;
using wpfGMTraceability.Models;
using static MaterialDesignThemes.Wpf.Theme.ToolBar;

namespace wpfGMTraceability.UserControls
{
    /// <summary>
    /// Interaction logic for TraceType2Control.xaml
    /// </summary>
    public partial class TraceType2Control : UserControl, IOverlayAware
    {
        StationData BOMInventoryData;

        private readonly DualSerialManager _serialManager;
        ObservableCollection<ScanLogItem> logItems = new ObservableCollection<ScanLogItem>();
        DispatcherTimer cleanTimer;

        public event EventHandler ShowLoadOverlay;
        public event EventHandler HideLoadOverlay;
        public TraceType2Control(DualSerialManager _dualManager)
        {
            InitializeComponent();
            _serialManager = _dualManager;
            _serialManager.DataReceived += SerialManager_DataReceived;
        }
        private void TraceType2_Control_Loaded(object sender, RoutedEventArgs e)
        {
            var window = Window.GetWindow(this) as IMainWindowHost;
            window?.SetWindowTitle("Nuevo título desde el UserControl");
            /**Se necesita esperar el resultado**/
            //LoadBOMDataAsync().GetAwaiter().GetResult();
            _ = LoadBOMDataAsync();
        }
        private void TraceType2_Control_Unloaded(object sender, RoutedEventArgs e)
        {
            
        }
        private void BtnPlayVideo_Click(object sender, RoutedEventArgs e)
        {

        }
        private void SerialManager_DataReceived(object sender, string data)
        {
            Dispatcher.Invoke(() =>
            {
                string sLastData = "";
                sLastData = txtScanCode.Text;
                txtLastScan.Text = $@"Último Escaneo: {sLastData.Replace("Escaneado:", "")}";
                txtScanCode.Text = $"Escaneado: {data}";
                DoConsume(data);
            });
        }
        private void dgParts_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {


            var dep = (DependencyObject)e.OriginalSource;

            if (dgParts.SelectedItem == null)
                return;

            // Subir en el árbol visual hasta encontrar la fila
            while (dep != null && !(dep is DataGridRow))
                dep = VisualTreeHelper.GetParent(dep);

            if (dep is DataGridRow row)
            {
                // Si ya está seleccionada y expandida, colapsa
                if (row.IsSelected && row.DetailsVisibility == Visibility.Visible)
                {
                    row.DetailsVisibility = Visibility.Collapsed;
                    dgParts.SelectedItem = null; // deselecciona para permitir reexpansión
                }
                else
                {
                    row.DetailsVisibility = Visibility.Visible;
                }
            }
        }
        #region Local Methods
        private async Task LoadBOMDataAsync()
        {
            BOMInventoryData = await ApiCalls.GetStationDataAsync();
            try
            {
                dgParts.ItemsSource = BOMInventoryData.Parts;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        private async void DoConsume(string serial) {
            var LastSerialByPart = BOMInventoryData.Parts
                .Where(p => p.Boxes != null && p.Boxes.Count > 0)
                .Select(p => new
                {
                    BomPart = p.BomPart,
                    BomQty = p.bom_quantity_per_piece,
                    OldBox = p.Boxes.Last()
                })
                .ToList();

            var jsonList = new List<object>();
            foreach (var item in LastSerialByPart)
            {
                var jsonEntry = new
                {
                    boxnumber = item.OldBox.BoxNumber,
                    serialtestnumber = serial.Trim(),
                    qty = item.BomQty
                };
                jsonList.Add(jsonEntry);
            }

            var payload = new StationConnectorPayload
            {
                station_name = "TycoStationConnector",
                items = jsonList
            };

            string jsonFinal = JsonConvert.SerializeObject(payload, Formatting.Indented);

            ShowLoadOverlay?.Invoke(this, EventArgs.Empty);
            var result = await ApiCalls.PostAPIConsumeAsync(jsonFinal);
            HideLoadOverlay?.Invoke(this, EventArgs.Empty);

            string Res = "NO_RESPONSE";

            if (result.content != null)
            {
                _serialManager.Send(Res);
                Dispatcher.Invoke(() => AddLog($"Serie hgdhgdghdValidada {serial} / {Res} / {result.statusCode}", "OK"));

                _ = LoadBOMDataAsync();
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
        #endregion
    }
}
