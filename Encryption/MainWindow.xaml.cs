using System;
using System.IO;
using System.Text;
using System.Windows;
using MahApps.Metro.Controls;
using Microsoft.Win32;
using System.Security.Cryptography;
using System.Diagnostics;
using BackRunner;
using System.Linq;
using System.Security.Permissions;
using System.Security;
using System.Windows.Input;

namespace Encryption
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        //版本号
        public const string version = "1.2";
        public const int build = 30;

        //应用信息
        public static string startupPath = Process.GetCurrentProcess().MainModule.FileName;
        public static string startupDirectory = Path.GetDirectoryName(startupPath);
        public static string configPath = startupDirectory + "\\config.ini";
        public static string updateConfigPath = startupDirectory + "\\updater.ini";

        //加密文件的位置
        public string filePath;
        //应用设置
        //mode 0:单文件
        //mode 1:文件夹
        public short mode = 0;
        //密钥设置
        //mode 1:md5
        //mode 2:sha512
        public static short originProcessKeyMode = 2;
        public static short processKeyMode = 2;

        //自解压设置
        //mode 1:普通
        //mode 2:临时
        //mode 3:可自更新
        public short extractMode = 1;

        //是否已经弹出过删除源文件警告框
        public bool isDeleteOriginMessage = false;
        public bool isDeleteOrigin = false;

        //提示弹窗
        public bool showMessageBox = true;

        //自解压
        public static bool isSelfExtract = false;

        //载入about窗体
        public static bool isAboutOpened = false;
        public static bool isSettingsOpened = false;

        //状态
        public static bool isUpdateEnable = true;

        private void btn_selectFile_Click(object sender, RoutedEventArgs e)
        {
            //根据模式切换选择框
            switch (mode)
            {
                case 0:
                    OpenFileDialog ofd = new OpenFileDialog();
                    ofd.ValidateNames = true;
                    ofd.CheckPathExists = true;
                    ofd.CheckFileExists = true;
                    if (ofd.ShowDialog() == true)
                    {
                        filePath = ofd.FileName;
                        txt_file.Text = filePath;
                    }
                    break;
                case 1:
                    System.Windows.Forms.FolderBrowserDialog fbd = new System.Windows.Forms.FolderBrowserDialog();
                    System.Windows.Forms.DialogResult dr = fbd.ShowDialog();
                    if (dr == System.Windows.Forms.DialogResult.OK)
                    {
                        filePath = fbd.SelectedPath;
                        txt_file.Text = filePath;
                    }
                    break;
            }
        }

        //加密
        private void btn_encrypt_Click(object sender, RoutedEventArgs e)
        {
            //无自解压
            if (!(bool)cb_selfextract.IsChecked)
            {
                isSelfExtract = false;
                switch (mode)
                {
                    case 0:
                        //检测是否选择文件
                        if (filePath.Length <= 0)
                        {
                            MessageBox.Show("请先选择文件。");
                            return;
                        }
                        //获取密钥
                        string key = pwd_key.Password;
                        //检测密钥
                        if (key.Length <= 0)
                        {
                            MessageBox.Show("请先输入密钥。");
                            return;
                        }
                        if (Path.GetExtension(filePath) == ".brencrypted" || (Path.GetExtension(filePath) == ".exe" && filePath.Contains(".brencrypted")))
                        {
                            MessageBox.Show("请勿二次加密加密文件。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                        else
                        {
                            encrypt_file(filePath, key);
                        }
                        break;
                    case 1:
                        //检测是否选择文件
                        if (filePath.Length <= 0)
                        {
                            MessageBox.Show("请先选择文件夹。");
                            return;
                        }
                        //获取密钥
                        key = pwd_key.Password;
                        //检测密钥
                        if (key.Length <= 0)
                        {
                            MessageBox.Show("请先输入密钥。");
                            return;
                        }
                        //确认加密
                        if (MessageBox.Show("您确定要加密该文件夹下的所有文件吗？\n程序会自动重命名文件名相同但格式不同的文件。", "确认", MessageBoxButton.OKCancel, MessageBoxImage.Information) == MessageBoxResult.OK)
                        {
                            var files = Directory.GetFiles(filePath, "*");
                            //检查文件名以防止重叠
                            if (files.Length > 1)
                            {
                                lbl_progress.Content = "正在检查文件名...";
                                int filecheck_count = 2;
                                for (int i = 0; i < files.Length - 1; i++)
                                {
                                    for (int j = 1; j < files.Length; j++)
                                    {
                                        if (Path.GetFileNameWithoutExtension(files[j]) == Path.GetFileNameWithoutExtension(files[i]))
                                        {
                                            try
                                            {
                                                File.Move(files[j], Path.GetDirectoryName(files[j]) + @"\" + Path.GetFileNameWithoutExtension(files[j]) + "_" + filecheck_count.ToString() + "_" + Path.GetExtension(files[j]).Replace(".", "") + Path.GetExtension(files[j]));
                                            }
                                            catch (Exception check_e)
                                            {
                                                MessageBox.Show("文件检查错误，加密中断\n错误信息：\n" + check_e.Message, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                                            }
                                        }
                                    }
                                }
                            }
                            //重新获取文件
                            files = Directory.GetFiles(filePath, "*");
                            //对文件夹进行加密
                            bool isSuccess = true;
                            int count = 0;
                            int skipcount = 0;
                            string errorFilePath = "";
                            foreach (var file in files)
                            {
                                lbl_progress.Content = "正在加密： " + Path.GetFileName(file) + " ，第 " + (count + skipcount + 1).ToString() + " / " + files.Length + " 个";
                                //加密文件跳过
                                if (Path.GetExtension(file) == ".brencrypted")
                                {
                                    skipcount++;
                                }
                                else
                                {
                                    if (!encrypt_file(file, key))
                                    {
                                        isSuccess = false;
                                        errorFilePath = file;
                                        break;
                                    }
                                    else
                                    {
                                        count++;
                                    }
                                }
                            }
                            if (isSuccess)
                            {
                                if (skipcount == 0 && showMessageBox)
                                {
                                    if (MessageBox.Show("文件加密完成，共加密 " + count.ToString() + " 个文件\n是否打开文件夹？", "完成", MessageBoxButton.YesNo, MessageBoxImage.Information) == MessageBoxResult.Yes)
                                    {
                                        Process.Start(filePath);
                                    }
                                }
                                else
                                {
                                    if (MessageBox.Show("文件加密完成，共加密 " + count.ToString() + " 个文件，其中跳过了 " + skipcount.ToString() + " 个加密文件\n是否打开文件夹？", "完成", MessageBoxButton.YesNo, MessageBoxImage.Information) == MessageBoxResult.Yes)
                                    {
                                        Process.Start(filePath);
                                    }
                                }
                            }
                            else
                            {
                                if (MessageBox.Show("批量加密已中断，已加密 " + count.ToString() + " 个文件\n错误文件：\n\n是否打开文件夹？" + errorFilePath, "中断", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                                {
                                    Process.Start(filePath);
                                }
                            }
                        }
                        lbl_progress.Content = "";
                        break;
                }
            }
            else
            {
                isSelfExtract = true;
                //有自解压
                switch (mode)
                {
                    //单文件
                    case 0:
                        //检测是否选择文件
                        if (filePath.Length <= 0)
                        {
                            MessageBox.Show("请先选择文件。");
                            return;
                        }
                        //获取密钥
                        string key = pwd_key.Password;
                        //检测密钥
                        if (key.Length <= 0)
                        {
                            MessageBox.Show("请先输入密钥。");
                            return;
                        }
                        if (Path.GetExtension(filePath) == ".brencrypted")
                        {
                            MessageBox.Show("请勿二次加密加密文件。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                        else
                        {
                            encrypt_file(filePath, key);
                            bind_file(filePath);
                        }
                        break;
                    //文件夹
                    case 1:
                        //检测是否选择文件
                        if (filePath.Length <= 0)
                        {
                            MessageBox.Show("请先选择文件夹。");
                            return;
                        }
                        //获取密钥
                        key = pwd_key.Password;
                        //检测密钥
                        if (key.Length <= 0)
                        {
                            MessageBox.Show("请先输入密钥。");
                            return;
                        }
                        //确认加密
                        if (MessageBox.Show("您确定要加密该文件夹下的所有文件吗？\n如果该目录下存在文件名相同但格式不同的文件，程序会对其进行自动编号。", "确认", MessageBoxButton.OKCancel, MessageBoxImage.Information) == MessageBoxResult.OK)
                        {
                            var files = Directory.GetFiles(filePath, "*");
                            //检查文件名以防止重叠
                            if (files.Length > 1)
                            {
                                lbl_progress.Content = "正在检查文件名...";
                                int filecheck_count = 2;
                                for (int i = 0; i < files.Length - 1; i++)
                                {
                                    for (int j = 1; j < files.Length; j++)
                                    {
                                        if (Path.GetFileNameWithoutExtension(files[j]) == Path.GetFileNameWithoutExtension(files[i]))
                                        {
                                            try
                                            {
                                                File.Move(files[j], Path.GetDirectoryName(files[j]) + @"\" + Path.GetFileNameWithoutExtension(files[j]) + "_" + filecheck_count.ToString() + "_" + Path.GetExtension(files[j]).Replace(".", "") + Path.GetExtension(files[j]));
                                            }
                                            catch (Exception check_e)
                                            {
                                                MessageBox.Show("文件检查错误，加密中断\n错误信息：\n" + check_e.Message, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                                            }
                                        }
                                    }
                                }
                            }
                            //重新获取文件
                            files = Directory.GetFiles(filePath, "*");
                            //对文件夹进行加密
                            bool isSuccess = true;
                            int count = 0;
                            int skipcount = 0;
                            string errorFilePath = "";
                            foreach (var file in files)
                            {
                                lbl_progress.Content = "正在加密： " + Path.GetFileName(file) + " ，第 " + (count + skipcount + 1).ToString() + " / " + files.Length + " 个";
                                //加密文件跳过
                                if (Path.GetExtension(file) == ".brencrypted")
                                {
                                    skipcount++;
                                }
                                else
                                {
                                    if (!encrypt_file(file, key))
                                    {
                                        isSuccess = false;
                                        errorFilePath = file;
                                        break;
                                    }
                                    if (!bind_file(file))
                                    {
                                        isSuccess = false;
                                        errorFilePath = file;
                                        break;
                                    }
                                    else
                                    {
                                        count++;
                                    }
                                }
                            }
                            if (isSuccess)
                            {
                                if (skipcount == 0 && showMessageBox)
                                {
                                    if (MessageBox.Show("文件加密完成，共加密 " + count.ToString() + " 个文件\n是否打开文件夹？", "完成", MessageBoxButton.YesNo, MessageBoxImage.Information) == MessageBoxResult.Yes)
                                    {
                                        Process.Start(filePath);
                                    }
                                }
                                else
                                {
                                    if (MessageBox.Show("文件加密完成，共加密 " + count.ToString() + " 个文件，其中跳过了 " + skipcount.ToString() + " 个加密文件\n是否打开文件夹？", "完成", MessageBoxButton.YesNo, MessageBoxImage.Information) == MessageBoxResult.Yes)
                                    {
                                        Process.Start(filePath);
                                    }
                                }
                            }
                            else
                            {
                                if (MessageBox.Show("批量加密已中断，已加密 " + count.ToString() + " 个文件\n错误文件：\n\n是否打开文件夹？" + errorFilePath, "中断", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                                {
                                    Process.Start(filePath);
                                }
                            }
                        }
                        lbl_progress.Content = "";
                        break;
                }
            }
        }
        //捆绑文件
        private bool bind_file(string filePath)
        {
            string extractorPath = "";
            switch (extractMode)
            {
                case 1:
                    extractorPath = startupDirectory + "/extractor/br_extractor.exe";
                    break;
                case 2:
                    extractorPath = startupDirectory + "/extractor/br_extractor_b.exe";
                    break;
                case 3:
                    extractorPath = startupDirectory + "/extractor/br_extractor_c.exe";
                    break;
            }
            //路径定义 & 流定义
            string cachePath = Path.GetDirectoryName(filePath) + "\\" + Path.GetFileNameWithoutExtension(filePath) + ".brencrypttemp";
            string outputPath = Path.GetDirectoryName(filePath) + "\\" + Path.GetFileNameWithoutExtension(filePath) + ".brencrypted.exe";
            //检查自解压文件是否存在
            if (File.Exists(extractorPath))
            {
                BinaryReader br = new BinaryReader(new FileStream(extractorPath, FileMode.Open, FileAccess.Read));
                BinaryReader tempbr;
                if (File.Exists(cachePath))
                {
                    tempbr = new BinaryReader(new FileStream(cachePath, FileMode.Open, FileAccess.Read));
                } else
                {
                    return false;
                }
                BinaryWriter bw = new BinaryWriter(new FileStream(outputPath, FileMode.Create));

                try
                {
                    //写入exe
                    byte[] buffer = new byte[256];
                    buffer = br.ReadBytes(256);
                    while (buffer.Length > 0)
                    {
                        bw.Write(buffer);
                        buffer = br.ReadBytes(256);
                    }

                    //写入标识
                    byte[] sign;
                    sign = Encoding.UTF8.GetBytes("brextract");
                    //填充
                    byte[] sign_filled = Enumerable.Repeat((byte)0x20, 256).ToArray();
                    for (int i = 0; i < sign.Length; i++)
                    {
                        sign_filled[i] = sign[i];
                    }
                    bw.Write(sign_filled);

                    //写入加密文件
                    buffer = tempbr.ReadBytes(256);
                    while (buffer.Length > 0)
                    {
                        bw.Write(buffer);
                        buffer = tempbr.ReadBytes(256);
                    }
                    bw.Flush();
                    bw.Close();
                    br.Close();
                    tempbr.Close();
                    File.Delete(cachePath);
                    if (mode == 0 && showMessageBox)
                    {
                        if (MessageBox.Show("文件加密完成，是否打开文件夹？", "完成", MessageBoxButton.YesNo, MessageBoxImage.Information) == MessageBoxResult.Yes)
                        {
                            Process.Start(Path.GetDirectoryName(filePath));
                        }
                    }
                }
                catch (Exception e)
                {
                    if (mode == 0)
                    {
                        MessageBox.Show("创建加密文件时遇到错误，错误如下：\n\n" + e.Message, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    return false;
                }
            }
            else
            {
                if (mode == 0)
                {
                    MessageBox.Show("找不到br_extractor，无法制作自解压程式。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                try
                {
                    File.Delete(cachePath);
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.Message);
                }
                return false;
            }
            if (isDeleteOrigin)
            {
                try
                {
                    File.Delete(filePath);
                }
                catch (Exception e)
                {
                    if (mode == 0)
                    {
                        throw (e);
                    }
                }
            }
            return true;
        }
        //加密文件
        private bool encrypt_file(string filePath, string key)
        {
            string encryptedFilePath;
            string encryptedExePath = "";

            //复原Processkeymode
            processKeyMode = originProcessKeyMode;
            key = processKey(key);

            if (isSelfExtract)
            {
                encryptedFilePath = Path.GetDirectoryName(filePath) + "\\" + Path.GetFileNameWithoutExtension(filePath) + ".brencrypttemp";
                encryptedExePath = Path.GetDirectoryName(filePath) + "\\" + Path.GetFileNameWithoutExtension(filePath) + ".brencrypted.exe";
            }
            else
            {
                encryptedFilePath = Path.GetDirectoryName(filePath) + "\\" + Path.GetFileNameWithoutExtension(filePath) + ".brencrypted";
            }

            if (isSelfExtract)
            {
                if (File.Exists(encryptedExePath))
                {
                    if (MessageBox.Show("当前文件：\n" + Path.GetFileName(filePath) + "\n加密后的文件已存在，是否覆盖？", "提示", MessageBoxButton.YesNo, MessageBoxImage.Information) == MessageBoxResult.No)
                    {
                        return true;
                    }
                }
            }
            else
            {
                if (File.Exists(encryptedFilePath))
                {
                    if (MessageBox.Show("当前文件：\n" + Path.GetFileName(filePath) + "\n加密后的文件已存在，是否覆盖？", "提示", MessageBoxButton.YesNo, MessageBoxImage.Information) == MessageBoxResult.No)
                    {
                        return true;
                    }
                }
            }
            FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            BinaryReader br = new BinaryReader(fs, new UTF8Encoding());
            //获取扩展名做文件头，文件头填充到64bytes
            string extension = Path.GetExtension(filePath);
            byte[] extension_byte = Encoding.UTF8.GetBytes(extension);
            //填充扩展名的块到64bytes
            byte[] extension_filled = new byte[64];
            for (int i = 0; i < extension_byte.Length; i++)
            {
                extension_filled[i] = extension_byte[i];
            }
            //向加密文件写入文件头
            BinaryWriter bw = new BinaryWriter(new FileStream(encryptedFilePath, FileMode.Create, FileAccess.ReadWrite));
            bw.Write(extension_filled);
            try
            {           
                byte[] buffer;
                //缓冲区
                buffer = br.ReadBytes(240);
                while (buffer.Length > 0)
                {
                    byte[] encrypted = encrypt_aes(buffer, key);
                    bw.Write(encrypted);
                    buffer = br.ReadBytes(240);
                }
                bw.Flush();
                br.Close();
                bw.Close();
                //弹出成功框
                if (mode == 0 && !isSelfExtract && showMessageBox)
                {
                    if (MessageBox.Show("文件加密完成，是否打开文件夹？", "完成", MessageBoxButton.YesNo, MessageBoxImage.Information) == MessageBoxResult.Yes)
                    {
                        Process.Start(Path.GetDirectoryName(filePath));
                    }
                }
                //删除源文件
                if (!isSelfExtract && isDeleteOrigin)
                {
                    try
                    {
                        File.Delete(filePath);
                    }
                    catch (Exception e)
                    {
                        if (mode == 0)
                        {
                            MessageBox.Show(e.Message, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }

                }
                return true;

            }
            catch (Exception e)
            {
                MessageBox.Show("加密文件错误。错误信息：\n" + e.Message, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                br.Close();
                bw.Flush();
                bw.Close();
                File.Delete(encryptedFilePath);
                return false;
            }
        }

        //文件解密
        private void btn_decrypt_Click(object sender, RoutedEventArgs e)
        {
            switch (mode)
            {
                case 0:
                    //检测是否选择文件
                    if (txt_file.Text.Length <= 0)
                    {
                        MessageBox.Show("请先选择文件。");
                        return;
                    }
                    //获取密钥
                    string key = pwd_key.Password;
                    //检测密钥
                    if (key.Length <= 0)
                    {
                        MessageBox.Show("请先输入密钥。");
                        return;
                    }
                    decrypt_file(filePath, key);
                    break;
                case 1:
                    //检测是否选择文件夹
                    if (txt_file.Text.Length <= 0)
                    {
                        MessageBox.Show("请先选择文件夹。");
                        return;
                    }
                    //获取密钥
                    key = pwd_key.Password;
                    //检测密钥
                    if (key.Length <= 0)
                    {
                        MessageBox.Show("请先输入密钥。");
                        return;
                    }
                    if (MessageBox.Show("您确定要解密该文件夹下的所有加密文件吗？", "确认", MessageBoxButton.OKCancel, MessageBoxImage.Information) == MessageBoxResult.OK)
                    {
                        var files = Directory.GetFiles(filePath, "*.brencrypted");
                        if (files.Length > 0)
                        {
                            bool isSuccess = true;
                            int count = 0;
                            string errorFilePath = "";
                            foreach (var file in files)
                            {
                                lbl_progress.Content = "正在解密： " + Path.GetFileName(file) + " ，第 " + (count + 1).ToString() + " / " + files.Length + " 个";
                                if (!decrypt_file(file, key))
                                {
                                    errorFilePath = file;
                                    isSuccess = false;
                                    break;
                                }
                                else
                                {
                                    count++;
                                }
                            }
                            if (isSuccess && showMessageBox)
                            {
                                if (MessageBox.Show("文件解密完成，共解密 " + count.ToString() + " 个文件\n是否打开文件夹？", "完成", MessageBoxButton.YesNo, MessageBoxImage.Information) == MessageBoxResult.Yes)
                                {
                                    Process.Start(filePath);
                                }
                            }
                            else
                            {
                                if (MessageBox.Show("批量解密已中断，已解密 " + count.ToString() + " 个文件\n错误文件：\n\n是否打开文件夹？" + errorFilePath, "中断", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                                {
                                    Process.Start(filePath);
                                }
                            }
                            lbl_progress.Content = "";
                        }
                        else
                        {
                            MessageBox.Show("当前文件夹下不存在加密文件", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                    break;
            }
        }

        //解密文件
        private bool decrypt_file(string filePath, string key)
        {
            //复原Processkeymode
            processKeyMode = originProcessKeyMode;
            try
            {
                using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    using (BinaryReader br = new BinaryReader(fs, new UTF8Encoding()))
                    {
                        //获取扩展名
                        string extension = Path.GetExtension(filePath);
                        //扩展名检测
                        if (extension == ".brencrypted")
                        {
                            //读取写在文件头的扩展名
                            byte[] extension_byte = br.ReadBytes(64);
                            extension = Encoding.UTF8.GetString(extension_byte).Replace("\0", "");
                            //解密后文件地址
                            string des_path = Path.GetDirectoryName(filePath) + "\\" + Path.GetFileNameWithoutExtension(filePath) + extension;
                            byte[] buffer;
                            buffer = br.ReadBytes(256);
                            //第一个块为密钥检测
                            byte[] des_check = decrypt_aes(buffer, key, 0, true);
                            //检测是否正确
                            if (des_check == null)
                            {
                                return false;
                            }
                            else
                            {
                                key = processKey(key);
                            }
                            //如果文件存在
                            if (File.Exists(des_path))
                            {
                                if (MessageBox.Show("当前文件：\n" + Path.GetFileName(filePath) + "\n解密后的文件已存在，是否覆盖？", "提示", MessageBoxButton.YesNo, MessageBoxImage.Information) == MessageBoxResult.No)
                                {
                                    return true;
                                }
                            }
                            //创建写入流
                            BinaryWriter bw = new BinaryWriter(new FileStream(des_path, FileMode.OpenOrCreate));
                            //写入检测块，开始解密后续部分
                            bw.Write(des_check);
                            buffer = br.ReadBytes(256);
                            while (buffer.Length > 0)
                            {
                                //密钥不对返回null
                                byte[] decrypted = decrypt_aes(buffer, key);
                                bw.Write(decrypted);
                                buffer = br.ReadBytes(256);
                            }
                            //关闭流
                            bw.Flush();
                            bw.Close();
                            br.Close();
                            if (mode == 0 && showMessageBox)
                            {
                                if (MessageBox.Show("文件解密完成，是否打开文件夹？", "完成", MessageBoxButton.YesNo, MessageBoxImage.Information) == MessageBoxResult.Yes)
                                {
                                    Process.Start(Path.GetDirectoryName(filePath));
                                }
                            }
                            if (isDeleteOrigin)
                            {
                                try
                                {
                                    File.Delete(filePath);
                                }
                                catch (Exception e)
                                {
                                    if (mode == 0)
                                    {
                                        MessageBox.Show(e.Message, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                                    }
                                }
                            }
                            return true;
                        }
                        else
                        {
                            if (mode == 0)
                            {
                                MessageBox.Show("这不是一个由本程序加密过的文件。","错误",MessageBoxButton.OK,MessageBoxImage.Error);
                            }
                            return false;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                MessageBox.Show("解密文件错误。错误信息：\n" + e.Message);
                return false;
            }
        }
        //加密算法
        public static byte[] encrypt_aes(byte[] block, string key)
        {                     
            RijndaelManaged rdel = new RijndaelManaged();            
            switch (processKeyMode) {
                case 1:
                    byte[] keyArray = Encoding.UTF8.GetBytes(key);
                    rdel.Key = keyArray;
                    byte[] iv = Encoding.UTF8.GetBytes(key.Substring(7, 16));
                    rdel.IV = iv;
                    rdel.Mode = CipherMode.CFB;
                    rdel.Padding = PaddingMode.PKCS7;
                    ICryptoTransform transform = rdel.CreateEncryptor();
                    return transform.TransformFinalBlock(block, 0, block.Length);
                default:
                    keyArray = Encoding.UTF8.GetBytes(key.Substring(92,32));
                    string _iv = key.Substring(31, 16);
                    iv = Encoding.UTF8.GetBytes(_iv);
                    rdel.Mode = CipherMode.CFB;
                    rdel.Padding = PaddingMode.PKCS7;
                    transform = rdel.CreateEncryptor(keyArray, iv);
                    return transform.TransformFinalBlock(block, 0, block.Length);
            }           
        }
        //解密算法
        public static byte[] decrypt_aes(byte[] block, string key,short retry=0,bool istry=false)
        {
            RijndaelManaged rdel = new RijndaelManaged();
            ICryptoTransform cTransform;
            string originKey = key;
            if (istry)
            {
                key = processKey(key);
            }
            try
            {
                switch (processKeyMode)
                {
                    case 1:
                        byte[] keyArray = Encoding.UTF8.GetBytes(key);
                        byte[] iv = Encoding.UTF8.GetBytes(key.Substring(7, 16));
                        rdel.IV = iv;
                        rdel.Key = keyArray;
                        rdel.Mode = CipherMode.CFB;
                        rdel.Padding = PaddingMode.PKCS7;
                        cTransform = rdel.CreateDecryptor();
                        break;
                    default:
                        keyArray = Encoding.UTF8.GetBytes(key.Substring(92, 32));
                        string _iv = key.Substring(31, 16);
                        iv = Encoding.UTF8.GetBytes(_iv);
                        rdel.Mode = CipherMode.CFB;
                        rdel.Padding = PaddingMode.PKCS7;
                        cTransform = rdel.CreateDecryptor(keyArray, iv);
                        break;
                }
                return cTransform.TransformFinalBlock(block, 0, block.Length);
            }
            catch (Exception e)
            {
                if (retry>=1)
                {
                    MessageBox.Show("尝试解密错误，请检查你输入的密钥。\n\n错误信息：\n"+e.Message, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    return null;
                } else
                {
                    processKeyMode++;
                    if (processKeyMode > 2)
                    {
                        processKeyMode = 1;
                    }
                    return decrypt_aes(block, originKey, ++retry,true);
                }                
            }
        }
        //处理Key
        public static string processKey(string key)
        {
            switch (processKeyMode)
            {
                case 1:
                    byte[] source = Encoding.UTF8.GetBytes(key);
                    MD5 md5 = MD5.Create();
                    byte[] result = md5.ComputeHash(source);
                    StringBuilder strb = new StringBuilder(40);
                    for (int i = 0; i < result.Length; i++)
                    {
                        strb.Append(result[i].ToString("x2"));
                    }
                    md5.Clear();
                    return strb.ToString();
                default:
                    source = Encoding.UTF8.GetBytes(key);
                    SHA512 sha512 = SHA512.Create();
                    result = sha512.ComputeHash(source);
                    strb = new StringBuilder(260);
                    for (int i = 0; i < result.Length; i++)
                    {
                        strb.Append(result[i].ToString("x2"));
                    }
                    return strb.ToString();
            }
        }

        //窗体load
        private void window_main_Loaded(object sender, RoutedEventArgs e)
        {
            //程序名称检查
            string appName = Path.GetFileName(startupPath);
            if (appName != "br_encryptor.exe")
            {
                MessageBox.Show("请勿重命名程序(br_encryptor.exe)，这将造成程序功能错误！", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                //错误则退出
                Environment.Exit(0);
            }

            //ini检查
            IniFile ini = new IniFile(configPath);
            string _version = ini.readValue("app", "version");
            if (_version == "")
            {
                ini.writeValue("app", "version", version);
            }
            string _build = ini.readValue("app", "build");
            if (_build == "")
            {
                ini.writeValue("app", "build", build.ToString());
            }
            else
            {
                //高版本ini覆盖
                if (build > int.Parse(_build))
                {
                    ini.writeValue("app", "version", version);
                    ini.writeValue("app", "build", build.ToString());
                }
            }

            //清理cache目录
            if (Directory.Exists(startupDirectory + "\\cache"))
            {
                try
                {
                    Directory.Delete(startupDirectory + "\\cache", true);
                }
                catch (Exception de)
                {
                }
            }

            //检查自动更新配置            
            IniFile updateini = new IniFile(updateConfigPath);
            string _name = updateini.readValue("app", "name");
            if (_name == "")
            {
                updateini.writeValue("app", "name", "br_encryptor");
            }
            string _enable = updateini.readValue("updater", "enable");
            isUpdateEnable = BackRunner.Convert.str_to_bool(_enable);

            if (isUpdateEnable)
            {
                //拉起自动更新
                Process update_process = Process.Start(startupDirectory + "\\br_updater.exe");
            }

            //更新自解压check
            switch (ini.readValue("config", "selfextract"))
            {
                default:
                    cb_selfextract.IsChecked = false;
                    break;
                case "true":
                    cb_selfextract.IsChecked = true;
                    radio_extract_a.Visibility = Visibility.Visible;
                    radio_extract_b.Visibility = Visibility.Visible;
                    radio_extract_c.Visibility = Visibility.Visible;
                    break;
            }
            switch (ini.readValue("config", "extractmode"))
            {
                default:
                    radio_extract_a.IsChecked = true;
                    break;
                case "1":
                    radio_extract_a.IsChecked = true;
                    break;
                case "2":
                    radio_extract_b.IsChecked = true;
                    break;
                case "3":
                    radio_extract_c.IsChecked = true;
                    break;
            }
            //messageBox check
            switch (ini.readValue("config", "showMessageBox"))
            {
                case "true":
                    showMessageBox = true;
                    cb_messageBox.IsChecked = true;
                    break;
                case "false":
                    showMessageBox = false;
                    cb_messageBox.IsChecked = false;
                    break;
                default:
                    showMessageBox = true;
                    cb_messageBox.IsChecked = true;
                    break;
            }
            //keymode
            switch (ini.readValue("config", "processKeyMode"))
            {
                case "1":
                    originProcessKeyMode = 1;
                    break;
                default:
                    originProcessKeyMode = 2;
                    break;
            }
            processKeyMode = originProcessKeyMode;
        }

        //右上角菜单定义
        private void btn_batch_Click(object sender, RoutedEventArgs e)
        {
            //模式
            switch (mode)
            {
                //当前为单个，执行批量
                case 0:
                    System.Windows.Forms.FolderBrowserDialog fbd = new System.Windows.Forms.FolderBrowserDialog();
                    System.Windows.Forms.DialogResult dr = fbd.ShowDialog();
                    if (dr == System.Windows.Forms.DialogResult.OK)
                    {
                        mode = 1;
                        filePath = fbd.SelectedPath;
                        txt_file.Text = filePath;
                        lbl_selectFile.Content = "选择文件夹";
                        btn_batch.Content = "加/解密单个文件";
                    }
                    break;
                //当前为批量，执行单个
                case 1:
                    OpenFileDialog ofd = new OpenFileDialog();
                    ofd.ValidateNames = true;
                    ofd.CheckPathExists = true;
                    ofd.CheckFileExists = true;
                    if (ofd.ShowDialog() == true)
                    {
                        mode = 0;
                        filePath = ofd.FileName;
                        txt_file.Text = filePath;
                        lbl_selectFile.Content = "选择文件";
                        btn_batch.Content = "批量加/解密";
                    }
                    break;
            }
        }

        //删除源文件复选框逻辑
        private void cb_deleteOrigin_Checked(object sender, RoutedEventArgs e)
        {
            if (!isDeleteOriginMessage)
            {
                if (MessageBox.Show("您确定要在加/解密完成后删除源文件吗？", "警告", MessageBoxButton.YesNo, MessageBoxImage.Exclamation) == MessageBoxResult.Yes)
                {
                    isDeleteOrigin = true;
                    isDeleteOriginMessage = true;
                }
                else
                {
                    isDeleteOrigin = false;
                    cb_deleteOrigin.IsChecked = false;
                }
            }
        }

        private void cb_deleteOrigin_Unchecked(object sender, RoutedEventArgs e)
        {
            isDeleteOrigin = false;
        }

        //自解压单选框逻辑
        private void radio_extract_a_Checked(object sender, RoutedEventArgs e)
        {
            radio_extract_b.IsChecked = false;
            radio_extract_c.IsChecked = false;
            extractMode = 1;
        }

        private void radio_extract_b_Checked(object sender, RoutedEventArgs e)
        {
            radio_extract_a.IsChecked = false;
            radio_extract_c.IsChecked = false;
            extractMode = 2;
        }

        private void radio_extract_c_Checked(object sender, RoutedEventArgs e)
        {
            radio_extract_a.IsChecked = false;
            radio_extract_b.IsChecked = false;
            extractMode = 3;
        }

        //窗体退出
        private void window_main_Closed(object sender, EventArgs e)
        {
            //保存设置            
            IniFile ini = new IniFile(configPath);
            //自解压
            if ((bool)cb_selfextract.IsChecked)
            {
                ini.writeValue("config", "selfextract", "true");
            }
            else
            {
                ini.writeValue("config", "selfextract", "false");
            }
            //自解压模式
            switch (extractMode)
            {
                case 1:
                    ini.writeValue("config", "extractmode", "1");
                    break;
                case 2:
                    ini.writeValue("config", "extractmode", "2");
                    break;
                case 3:
                    ini.writeValue("config", "extractmode", "3");
                    break;
                default:
                    ini.writeValue("config", "extractmode", "1");
                    break;
            }
            if ((bool)cb_messageBox.IsChecked)
            {
                ini.writeValue("config", "showMessageBox", "true");
            }
            else
            {
                ini.writeValue("config", "showMessageBox", "false");
            }
            switch (originProcessKeyMode)
            {
                case 1:
                    ini.writeValue("config", "processKeyMode", "1");
                    break;
                case 2:
                    ini.writeValue("config", "processKeyMode", "2");
                    break;
                default:
                    ini.writeValue("config", "processKeyMode", "3");
                    break;
            }
            //主窗体退出后结束进程
            Environment.Exit(0);
        }

        //自解压复选框UI逻辑
        private void cb_selfextract_Checked(object sender, RoutedEventArgs e)
        {
            radio_extract_a.Visibility = Visibility.Visible;
            radio_extract_b.Visibility = Visibility.Visible;
            radio_extract_c.Visibility = Visibility.Visible;
        }

        private void cb_selfextract_Unchecked(object sender, RoutedEventArgs e)
        {
            radio_extract_a.Visibility = Visibility.Hidden;
            radio_extract_b.Visibility = Visibility.Hidden;
            radio_extract_c.Visibility = Visibility.Hidden;
        }

        //弹框复选框逻辑
        private void cb_messageBox_Checked(object sender, RoutedEventArgs e)
        {
            showMessageBox = true;
        }

        private void cb_messageBox_Unchecked(object sender, RoutedEventArgs e)
        {
            showMessageBox = false;
        }

        private void btn_about_Click(object sender, RoutedEventArgs e)
        {
            if (!isAboutOpened)
            {
                about about = new about();
                about.Show();
                isAboutOpened = true;
            }
        }

        private void btn_settings_Click(object sender, RoutedEventArgs e)
        {
            if (!isSettingsOpened)
            {
                settings settings = new settings();
                settings.Show();
                isSettingsOpened = true;
            }
        }

        //密码框触发
        private void pwd_key_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (mode == 0)
                {
                    if (txt_file.Text.Contains(".brencrypted"))
                    {
                        btn_decrypt_Click(sender, e);
                    }
                    else
                    {
                        btn_encrypt_Click(sender, e);
                    }
                }
            }
        }
    }
}
