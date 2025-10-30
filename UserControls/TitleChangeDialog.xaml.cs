using MaterialDesignThemes.Wpf;
using System;
using System.Collections.Generic;
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

namespace wpfGMTraceability.UserControls
{
    /// <summary>
    /// Interaction logic for TitleChangeDialog.xaml
    /// </summary>
    public partial class TitleChangeDialog : UserControl
    {
        public TitleChangeDialog()
        {
            InitializeComponent();
        }

        private void btnOk_Click(object sender, RoutedEventArgs e)
        {
            string windowName = txtWindowName.Text.Trim();
            if (!String.IsNullOrWhiteSpace(windowName))
            {
                DialogHost.CloseDialogCommand.Execute(windowName, null);
            }
            else
            {
                MessageBox.Show("Teclea el nombre de la estacion");
                txtWindowName.Focus();
            }
        }
    }
}
