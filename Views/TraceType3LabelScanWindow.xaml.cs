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
using System.Windows.Shapes;
using wpfGMTraceability.Managers;
using wpfGMTraceability.Models;

namespace wpfGMTraceability.Views
{
    /// <summary>
    /// Interaction logic for TraceType3LabelScanWindow.xaml
    /// </summary>
    public partial class TraceType3LabelScanWindow : Window
    {
        #region Inicialización y carga
        public string LabelScanCode { get; private set; }
        private readonly SerialPortSession _session;
        public event EventHandler ShowLoadOverlay;
        public event EventHandler HideLoadOverlay;
        ObservableCollection<ScanLogItem> logItems = new ObservableCollection<ScanLogItem>();
        public TraceType3LabelScanWindow(SerialPortSession session)
        {
            InitializeComponent();
            _session = session;
            _session.AssignOwner(this, OnModalData);
        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            LabelScanCode = "";
        }
        #endregion
        #region Eventos del sistema
        private void BtnCancel_Click(object sender, RoutedEventArgs e)
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
            });
            FinishAction();
        }
        #endregion
        private void FinishAction()
        {
            LabelScanCode = txtScanCode.Text.Trim();
            this.DialogResult = true;
        }
    }
}
