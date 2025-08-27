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
using System.Windows.Shapes;

namespace wpfGMTraceability.Views
{
    /// <summary>
    /// Interaction logic for ModelPauseWindow.xaml
    /// </summary>
    public partial class ModelPauseWindow : Window
    {
        public ModelPauseWindow()
        {
            InitializeComponent();
        }

        private void ModelPause_Window_Loaded(object sender, RoutedEventArgs e)
        {
            txtPassword.Clear();
            txtPassword.Focus();
        }
        private void BtnValidate_Click(object sender, RoutedEventArgs e)
        {

        }
        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
