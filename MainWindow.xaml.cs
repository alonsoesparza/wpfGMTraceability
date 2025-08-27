using Newtonsoft.Json;
using System.IO;
using System.Windows;
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
        private DualSerialManager _dual;
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Main_Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (!File.Exists(App.ConfigPortsFilePath))
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
            var json = System.IO.File.ReadAllText(App.ConfigSettingsFilePath);
            _config = JsonConvert.DeserializeObject<SettingsConfig>(json);
            App.APIUrlCheckSerial = _config.APIUrl;
            App.TraceType = _config.TraceType;

            //*******Load Ports Config********
            _dual = new DualSerialManager();
            RenderPages.Children.Clear();

            try
            {
                _dual.Start();
                RenderPages.Children.Add(new TraceType1Control(_dual));
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
            _dual.Stop();
        }
    }
}
