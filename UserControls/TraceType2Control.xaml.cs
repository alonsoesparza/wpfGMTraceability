using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using wpfGMTraceability.Helpers;
using wpfGMTraceability.Managers;
using wpfGMTraceability.Models;
using wpfGMTraceability.Views;

namespace wpfGMTraceability.UserControls
{
    /// <summary>
    /// Interaction logic for TraceType2Control.xaml
    /// </summary>
    public partial class TraceType2Control : UserControl, IOverlayAware
    {
        #region Inicialización y carga

        StationData BOMInventoryData;

        private SerialPortSession _session;
        ObservableCollection<ScanLogItem> logItems = new ObservableCollection<ScanLogItem>();

        public event EventHandler ShowLoadOverlay;
        public event EventHandler HideLoadOverlay;
        public TraceType2Control()
        {
            InitializeComponent();

            SerialPortConfig _config;

            var json = System.IO.File.ReadAllText(SettingsManager.ConfigPortsFilePath);
            _config = JsonConvert.DeserializeObject<SerialPortConfig>(json);

            _session = new SerialPortSession(_config.Port, _config.BaudRate, _config.Parity, _config.DataBits, _config.StopBits);
            _session.AssignOwner(this, OnSerialData);
            _session.Open();

        }
        private void TraceType2_Control_Loaded(object sender, RoutedEventArgs e)
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
                ProcessSerialNumber(data);
            });

        }
        #endregion

        #region Funciones de negocio / lógica principal
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
        private void ProcessSerialNumber(string serial)
        {
            var InsufficientParts = CheckForSufficientStock();
            if (InsufficientParts.Count > 0)
            {
                try
                {
                    _session.ReleaseOwner(this);
                    var modal = new RequestBoxWindow(_session, InsufficientParts, BOMInventoryData, serial);
                    modal.ShowDialog();
                    _session.AssignOwner(this, OnSerialData);
                    _ = LoadBOMDataAsync();
                }
                catch (Exception ex)
                {
                    Console.Write(ex.Message);
                }
                return;
            }
            else
            {
                DoConsume(serial);
            }
        }
        private List<object> CheckForSufficientStock()
        {
            var SufficientParts = BOMInventoryData.Parts
                .Where(p => !p.Sufficient)
                .Select(p => new
                {
                    p.BomPart,
                    p.bom_quantity_per_piece,
                    p.total_available
                });

            return SufficientParts.Cast<object>().ToList();
        }
        private async void DoConsume(string serial)
        {
            //** Cajas Ordenadas para recorrerlas en orden
            var orderedBoxesByPart = BOMInventoryData.Parts
                                    .Where(p => p.Boxes != null && p.Boxes.Count > 0)
                                    .Select(p => new
                                    {
                                        BomPart = p.BomPart,
                                        BomQty = p.bom_quantity_per_piece,
                                        Boxes = p.Boxes.OrderBy(b => b.BoxNumber).ToList()
                                    })
                                    .ToList();

            //** Crear la lista de consumo
            var consumptionItems = new List<object>();
            foreach (var part in orderedBoxesByPart)
            {
                int remainingQty = part.BomQty;

                foreach (var box in part.Boxes)
                {
                    if (remainingQty <= 0)
                        break;

                    int qtyToTake = Math.Min(box.BoxQt, remainingQty);

                    consumptionItems.Add(new
                    {
                        boxnumber = box.BoxNumber,
                        serialtestnumber = serial,
                        qty = qtyToTake
                    });

                    remainingQty -= qtyToTake;
                }
            }

            var finalJson = new
            {
                station_name = BOMInventoryData.Station,
                items = consumptionItems
            };

            string jsonFinal = JsonConvert.SerializeObject(finalJson, Formatting.Indented);

            ShowLoadOverlay?.Invoke(this, EventArgs.Empty);
            var result = await ApiCalls.PostAPIConsumeAsync(jsonFinal);
            HideLoadOverlay?.Invoke(this, EventArgs.Empty);

            string ResContent = result.content;
            int StatusCode = result.statusCode;

            string StatusMessage = HttpStatusHelper.GetStatusMessage(StatusCode);

            if (ResContent != null)
            {
                Dispatcher.Invoke(() => AddLog($"{serial} / {ResContent} / {StatusMessage}", "OK"));
                _ = LoadBOMDataAsync();
            }
            else
            {
                //**** Mensaje de error, API no responde
                Dispatcher.Invoke(() => AddLog($"{serial} / {ResContent} / {StatusMessage}", "ERROR"));
            }
        }
        #endregion

        #region Liberación de recursos
        private void TraceType2_Control_Unloaded(object sender, RoutedEventArgs e)
        {
            _session.Dispose();
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
