using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Management;
using System.Windows;
using System.Windows.Controls;
using wpfGMTraceability.Helpers;

namespace wpfGMTraceability.Views
{
    /// <summary>
    /// Interaction logic for ConfigWindow.xaml
    /// </summary>
    public partial class ConfigWindow : Window
    {
        public ObservableCollection<string> COMList { get; set; } = new ObservableCollection<string>();
        public ConfigWindow()
        {
            InitializeComponent();
        }
        private void Config_Window_Loaded(object sender, RoutedEventArgs e)
        {
            LoadConfig();
        }
        private void btnSaveAndReconnect_Click(object sender, RoutedEventArgs e)
        {
            var itTypeTrace = cbTraceType.SelectedItem as ComboBoxItem;
            var valTypeTrace = "";
            if (itTypeTrace != null)
            {
                valTypeTrace = itTypeTrace.Content.ToString();
            }
            else
            {
                valTypeTrace = "Tipo 1";
            }

            string readPort = (cbInPort.SelectedItem == null) ? "" : cbInPort.SelectedItem.ToString();
            string writePort = (cbOutPort.SelectedItem == null) ? "" : cbOutPort.SelectedItem.ToString();

            if(readPort == writePort) { 
                MessageBox.Show("Los puertos de lectura y escritura no pueden ser iguales.", "Error de configuración", MessageBoxButton.OK, MessageBoxImage.Warning);
                cbOutPort.SelectedIndex = -1;
                writePort = "";
                return;
            }

            var configPorts = new SerialPortConfig
            {
                Port = readPort,
                BaudRate = 9600,
                Parity = Parity.None,
                DataBits = 8,
                StopBits = StopBits.One
            };

            var configWritePorts = new SerialPortConfig
            {
                Port = writePort,
                BaudRate = 9600,
                Parity = Parity.None,
                DataBits = 8,
                StopBits = StopBits.One
            };

            string sTargetVideoPath = Path.Combine(SettingsManager.AppFolderPath, txtVideoPath.Text);
            var configSettings = new SettingsConfig
            {
                APIUrl = (txtAPIUrl.Text == null) ? "http://localhost/" : txtAPIUrl.Text.Trim().ToString(),
                APILoadBOMUrl = (txtAPIBOMUrl.Text == null) ? "http://localhost/" : txtAPIBOMUrl.Text.Trim().ToString(),
                APIBoxRequestUrl = (txtAPIRequestBoxUrl.Text == null) ? "http://localhost/" : txtAPIRequestBoxUrl.Text.Trim().ToString(),
                APISerialConsumeUrl = (txtAPIConsumeUrl.Text == null) ? "http://localhost/" : txtAPIConsumeUrl.Text.Trim().ToString(),
                TraceType = valTypeTrace.ToString(),
                VideoURL = (sTargetVideoPath == null) ? @"C:\" : sTargetVideoPath.Trim().ToString(),
                APIInsert = (txtApiInsert.Text == null) ? @"C:\" : txtApiInsert.Text.Trim().ToString()
            };

            var jsonSettings = JsonConvert.SerializeObject(configSettings, Formatting.Indented);
            System.IO.File.WriteAllText(SettingsManager.ConfigSettingsFilePath, jsonSettings);

            var jsonPorts = JsonConvert.SerializeObject(configPorts, Formatting.Indented);
            System.IO.File.WriteAllText(SettingsManager.ConfigPortsFilePath, jsonPorts);

            var jsonWritePort = JsonConvert.SerializeObject(configWritePorts, Formatting.Indented);
            System.IO.File.WriteAllText(SettingsManager.ConfigWritePortsFilePath, jsonWritePort);

            try
            {
                DialogResult = true;

                System.Diagnostics.Process.Start(Application.ResourceAssembly.Location);
                Application.Current.Shutdown();
            }catch (System.IO.IOException IOEx){ 
                if(IOEx.ToString() == @"Acceso al puerto denegado.")
                {
                    MessageBox.Show("No se pudo conectar al puerto serie. Esta en uso.", "Error de conexión", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            
            }catch (Exception){
                MessageBox.Show("No se pudo conectar al puerto serie. Verifique la configuración e intente nuevamente.", "Error de conexión", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            Close();
        }
        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
        #region LocalFunctions
        private void LoadConfig()
        {
            List<string> ports = SerialPort.GetPortNames().ToList();
            // Cargar puertos disponibles
            cbInPort.ItemsSource = new List<string>(ports);
            cbOutPort.ItemsSource = new List<string>(ports);

            // Ejemplo de carga
            LoadCOMs();

            DataContext = this;

            // Cargar configuración actual
            try
            {
                SettingsConfig _config;
                var json = System.IO.File.ReadAllText(SettingsManager.ConfigSettingsFilePath);
                _config = JsonConvert.DeserializeObject<SettingsConfig>(json);
                txtAPIUrl.Text = _config.APIUrl;
                cbTraceType.Text = _config.TraceType;
                txtAPIBOMUrl.Text = _config.APILoadBOMUrl;
                txtAPIRequestBoxUrl.Text = _config.APIBoxRequestUrl;
                txtAPIConsumeUrl.Text = _config.APISerialConsumeUrl;
                txtVideoPath.Text = _config.VideoURL;
                txtApiInsert.Text = _config.APIInsert;
                

                SerialPortConfig _configPorts;
                var jsonPorts = System.IO.File.ReadAllText(SettingsManager.ConfigPortsFilePath);
                _configPorts = JsonConvert.DeserializeObject<SerialPortConfig>(jsonPorts);
                cbInPort.SelectedItem = _configPorts.Port;

                SerialPortConfig _configWritePort;
                var jsonWritePort = System.IO.File.ReadAllText(SettingsManager.ConfigWritePortsFilePath);
                _configWritePort = JsonConvert.DeserializeObject<SerialPortConfig>(jsonWritePort);
                cbOutPort.SelectedItem = _configWritePort.Port;
              
            }
            catch (Exception)
            {

            }            
        }
        private void LoadCOMs() {
            var lista = new List<string>();
            using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_PnPEntity WHERE Name LIKE '%(COM%'"))
            {
                foreach (ManagementObject obj in searcher.Get())
                {
                    string nombre = obj["Name"]?.ToString();         // Ej: "USB Serial Device (COM4)"
                    string fabricante = obj["Manufacturer"]?.ToString(); // Ej: "FTDI"
                    string id = obj["DeviceID"]?.ToString();         // Ej: "USB\\VID_0403&PID_6001\\A50285BI"

                    COMList.Add($"{nombre} | {fabricante} | {id}");
                }
            }
        }
        #endregion
        private void btnLoadFile_Click(object sender, RoutedEventArgs e)
        {
            var wDialog = new OpenFileDialog
            {
                Title = "Selecciona tu archivo",
                Filter = "Todos los archivos (*.*)|*.*"
            };

            if (wDialog.ShowDialog() == true)
            {
                string sFilePath = wDialog.FileName;
                string sFileName = Path.GetFileName(sFilePath);
                txtVideoPath.Text = sFileName;

                string sTarget = Path.Combine(SettingsManager.AppFolderPath, sFileName);

                try
                {
                    File.Copy(sFilePath, sTarget, overwrite: true);
                }
                catch (IOException ex)
                {
                    MessageBox.Show($"Error al copiar el archivo:\n{ex.Message}", "Error");
                    txtVideoPath.Text = ex.Message;
                }

            }
        }
    }
}
