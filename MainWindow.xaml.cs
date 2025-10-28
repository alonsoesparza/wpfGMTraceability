using Newtonsoft.Json;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using wpfGMTraceability.Helpers;
using wpfGMTraceability.Managers;
using wpfGMTraceability.UserControls;
using wpfGMTraceability.Views;

namespace wpfGMTraceability
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }
        private void Main_Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (!File.Exists(SettingsManager.ConfigPortsFilePath))
            {
                var configWindow = new ConfigWindow(); // Tu ventana de configuración
                bool? result = configWindow.ShowDialog();

                if (result != true)
                {
                    MessageBox.Show("Se requiere configuración para continuar.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    Application.Current.Shutdown();
                    return;
                }
            }

            //*******Load Settings Config********
            SettingsConfig _config;
            var json = System.IO.File.ReadAllText(SettingsManager.ConfigSettingsFilePath);
            _config = JsonConvert.DeserializeObject<SettingsConfig>(json);
            SettingsManager.APIUrlCheckSerial = _config.APIUrl;
            SettingsManager.TraceType = _config.TraceType;
            SettingsManager.APILoadBOMUrl = _config.APILoadBOMUrl;
            SettingsManager.APIRequestBoxUrl = _config.APIBoxRequestUrl;
            SettingsManager.APIConsumeSerialUrl = _config.APISerialConsumeUrl;
            SettingsManager.APIPASSInsertUrl = _config.APIInsert;
            SettingsManager.VideoFileName = _config.VideoURL;

            //*******Load Ports Config********
            RenderPages.Children.Clear();

            try
            {
                UserControl myUsrCtrl = null;

                switch (SettingsManager.TraceType)
                {
                    case "Tipo 1":
                        //Title = "GM Traceability - Tipo 1";
                        myUsrCtrl = new TraceType1Control();
                        break;
                    case "Tipo 2":
                        //Title = "GM Traceability - Tipo 1";
                        myUsrCtrl = new TraceType2Control();
                        break;

                    default:
                        break;
                }

                if (myUsrCtrl != null)
                {
                    if (myUsrCtrl is IOverlayAware overlayAware)
                    {
                        overlayAware.ShowLoadOverlay += (s, ee) => LoadingOverlay.Visibility = Visibility.Visible;
                        overlayAware.HideLoadOverlay += (s, ee) => LoadingOverlay.Visibility = Visibility.Collapsed;
                    }

                    RenderPages.Children.Add(myUsrCtrl);
                }

            }
            catch (System.IO.IOException exIO)
            {
                MessageBox.Show($"Error al iniciar: {exIO.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Error al iniciar: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }            
        }
        #region LocalFunctions
        private void OpenOptions_Click(object sender, RoutedEventArgs e)
        {
            var ventana = Window.GetWindow(this) as MainWindow;
            ventana?.MostrarOverlay(true);

            var login = new OptionsMenuWindow();
            login.Owner = ventana;
            login.ShowDialog();

            ventana?.MostrarOverlay(false);
        }
        public void MostrarOverlay(bool mostrar)
        {
            OverlayOscuro.Visibility = mostrar ? Visibility.Visible : Visibility.Collapsed;
        }
        #endregion
        private void Main_Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            
        }
    }
}
