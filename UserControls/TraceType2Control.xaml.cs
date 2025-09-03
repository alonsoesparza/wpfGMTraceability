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

namespace wpfGMTraceability.UserControls
{
    /// <summary>
    /// Interaction logic for TraceType2Control.xaml
    /// </summary>
    public partial class TraceType2Control : UserControl, IOverlayAware
    {
        private readonly DualSerialManager _serialManager;
        ObservableCollection<ScanLogItem> logItems = new ObservableCollection<ScanLogItem>();
        DispatcherTimer cleanTimer;

        public event EventHandler ShowLoadOverlay;
        public event EventHandler HideLoadOverlay;
        public TraceType2Control(DualSerialManager _dualManager)
        {
            InitializeComponent();
        }
        private void TraceType2_Control_Loaded(object sender, RoutedEventArgs e)
        {
            var window = Window.GetWindow(this) as IMainWindowHost;
            window?.SetWindowTitle("Nuevo título desde el UserControl");
        }
        private void TraceType2_Control_Unloaded(object sender, RoutedEventArgs e)
        {

        }

        private void BtnPlayVideo_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
