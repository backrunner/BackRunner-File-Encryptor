using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using MahApps.Metro.Controls;
using BackRunner;

namespace Encryption
{
    /// <summary>
    /// about.xaml 的交互逻辑
    /// </summary>
    public partial class about : MetroWindow
    {
        public about()
        {
            InitializeComponent();
        }


        private void lbl_gitlink_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Process.Start("https://coding.net/u/BackRunner/p/BackRunner-Encryptor/git");
        }

        private void aboutSoftware_Loaded(object sender, RoutedEventArgs e)
        {
            lbl_ver.Content = "Version: " + MainWindow.version + "  Build: " + MainWindow.build;            
        }
    }
}
