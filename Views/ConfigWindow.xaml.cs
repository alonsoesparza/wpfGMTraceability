using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
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
        //private SerialPortManager _serialManager = new SerialPortManager();
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
                ReadPort = readPort,
                BaudRate = 9600,
                Parity = Parity.None,
                DataBits = 8,
                StopBits = StopBits.One
            };

            var configSettings = new SettingsConfig
            {
                APIUrl = (txtAPIUrl.Text == null) ? "http://localhost/" : txtAPIUrl.Text.Trim().ToString(),
                APILoadBOMUrl = (txtAPIBOMUrl.Text == null) ? "http://localhost/" : txtAPIBOMUrl.Text.Trim().ToString(),
                APIBoxRequestUrl = (txtAPIRequestBoxUrl.Text == null) ? "http://localhost/" : txtAPIRequestBoxUrl.Text.Trim().ToString(),
                APISerialConsumeUrl = (txtAPIConsumeUrl.Text == null) ? "http://localhost/" : txtAPIConsumeUrl.Text.Trim().ToString(),
                TraceType = valTypeTrace.ToString()
            };

            var jsonSettings = JsonConvert.SerializeObject(configSettings, Formatting.Indented);
            System.IO.File.WriteAllText(SettingsManager.ConfigSettingsFilePath, jsonSettings);

            var jsonPorts = JsonConvert.SerializeObject(configPorts, Formatting.Indented);
            System.IO.File.WriteAllText(SettingsManager.ConfigPortsFilePath, jsonPorts);

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

                SerialPortConfig _configPorts;
                var jsonPorts = System.IO.File.ReadAllText(SettingsManager.ConfigPortsFilePath);
                _configPorts = JsonConvert.DeserializeObject<SerialPortConfig>(jsonPorts);
                cbInPort.SelectedItem = _configPorts.ReadPort;
               // cbOutPort.SelectedItem = _configPorts.WritePort;                
            }
            catch (Exception)
            {

            }            
        }
        #endregion
    }
}
