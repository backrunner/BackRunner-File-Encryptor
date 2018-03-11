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
using MahApps.Metro.Controls;
using BackRunner;

namespace Encryption
{
    /// <summary>
    /// settings.xaml 的交互逻辑
    /// </summary>
    public partial class settings : MetroWindow
    {
        public settings()
        {
            InitializeComponent();
        }

        private short processKeyMode = MainWindow.originProcessKeyMode;

        private void settingsWindow_Loaded(object sender, RoutedEventArgs e)
        {
            processKeyMode = MainWindow.originProcessKeyMode;
            //获取key处理模式
            switch (processKeyMode)
            {
                case 1:
                    rb_processkey_md5.IsChecked = true;
                    rb_processkey_sha512.IsChecked = false;
                    break;
                case 2:
                    rb_processkey_md5.IsChecked = false;
                    rb_processkey_sha512.IsChecked = true;
                    break;
                default:
                    rb_processkey_md5.IsChecked = false;
                    rb_processkey_sha512.IsChecked = true;
                    break;
            }
        }

        private void settingsWindow_Closed(object sender, EventArgs e)
        {
            MainWindow.isSettingsOpened = false;
        }

        private void btn_save_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.originProcessKeyMode = processKeyMode;           
            try
            {
                IniFile updateConfig = new IniFile(MainWindow.updateConfigPath);
                if (MainWindow.isUpdateEnable)
                {
                    updateConfig.writeValue("updater", "enable", "true");
                }
                else
                {
                    updateConfig.writeValue("updater", "enable", "false");
                }
            } catch (Exception err)
            {
                MessageBox.Show("保存时出现错误。\n\n错误信息：\n"+err.Message, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            MessageBox.Show("设置已保存成功。", "保存", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        //Processkeymode单选框UI逻辑
        private void rb_processkey_md5_Checked(object sender, RoutedEventArgs e)
        {
            rb_processkey_sha512.IsChecked = false;
            processKeyMode = 1;
        }
        private void rb_processkey_sha512_Checked(object sender, RoutedEventArgs e)
        {
            rb_processkey_md5.IsChecked = false;
            processKeyMode = 2;
        }

        //自动更新逻辑
        private void rb_update_on_Checked(object sender, RoutedEventArgs e)
        {
            rb_update_off.IsChecked = false;
            MainWindow.isUpdateEnable = true;
        }

        private void rb_update_off_Checked(object sender, RoutedEventArgs e)
        {
            rb_update_on.IsChecked = false;
            MainWindow.isUpdateEnable = false;
        }
    }
}
