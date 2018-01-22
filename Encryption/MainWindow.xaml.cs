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
        public const string version = "beta 3";
        public const int build = 4;

        //应用信息
        public static string startupPath = Process.GetCurrentProcess().MainModule.FileName;
        public static string startupDirectory = Path.GetDirectoryName(startupPath);

        //加密文件的位置
        public string filePath;

        //应用设置
        //mode 0:单文件
        //mode 1:文件夹
        public int mode = 0;

        //自解压
        public static bool isSelfExtract = false;

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
                        else
                        {
                            key = processKey(key);
                        }
                        if (Path.GetExtension(filePath) == ".brencrypted")
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
                        else
                        {
                            key = processKey(key);
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
                                if (skipcount == 0)
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
                        else
                        {
                            key = processKey(key);
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
                }
            }
        }
        //捆绑文件
        private bool bind_file(string filePath)
        {
            //检查自解压文件是否存在
            if (File.Exists(startupDirectory + "/br_extractor.exe"))
            {
                //路径定义 & 流定义
                string cachePath = Path.GetDirectoryName(filePath) + "\\" + Path.GetFileNameWithoutExtension(filePath) + ".brencrypttemp";
                string extractorPath = startupDirectory + "/extractor/br_extractor.exe";
                string outputPath = Path.GetDirectoryName(filePath) + "\\" + Path.GetFileNameWithoutExtension(filePath) + ".brencrypted.exe";
                BinaryReader br = new BinaryReader(new FileStream(extractorPath, FileMode.Open));
                BinaryReader tempbr = new BinaryReader(new FileStream(cachePath, FileMode.Open));
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
                    if (MessageBox.Show("文件加密完成，是否打开文件夹？", "完成", MessageBoxButton.YesNo, MessageBoxImage.Information) == MessageBoxResult.Yes)
                    {
                        Process.Start(Path.GetDirectoryName(filePath));
                    }
                } catch (Exception e)
                {
                    MessageBox.Show("创建加密文件时遇到错误，错误如下：\n\n"+e.Message, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }
            } else
            {
                MessageBox.Show("找不到br_extractor.exe，无法制作自解压程式。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            return true;
        }
        //加密文件
        private bool encrypt_file(string filePath, string key)
        {
            try
            {
                string encryptedFilePath;
                if (isSelfExtract)
                {
                    encryptedFilePath = Path.GetDirectoryName(filePath) + "\\" + Path.GetFileNameWithoutExtension(filePath) + ".brencrypttemp";
                } else
                {
                    encryptedFilePath = Path.GetDirectoryName(filePath) + "\\" + Path.GetFileNameWithoutExtension(filePath) + ".brencrypted";
                }
                if (File.Exists(encryptedFilePath))
                {
                    if (isSelfExtract)
                    {
                        if (MessageBox.Show("当前文件：\n" + Path.GetFileName(filePath) + "\n加密后的文件已存在，是否覆盖？", "提示", MessageBoxButton.YesNo, MessageBoxImage.Information) == MessageBoxResult.No)
                        {
                            return true;
                        }
                    } else
                    {
                        File.Delete(encryptedFilePath);
                    }
                }
                //读文件流
                using (FileStream fs = new FileStream(filePath, FileMode.Open))
                {
                    using (BinaryReader br = new BinaryReader(fs, new UTF8Encoding()))
                    {
                        //先定义写入流
                        BinaryWriter bw;
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
                        bw = new BinaryWriter(new FileStream(encryptedFilePath, FileMode.Create));
                        bw.Write(extension_filled);

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
                        if (mode == 0 && !isSelfExtract)
                        {
                            if (MessageBox.Show("文件加密完成，是否打开文件夹？", "完成", MessageBoxButton.YesNo, MessageBoxImage.Information) == MessageBoxResult.Yes)
                            {
                                Process.Start(Path.GetDirectoryName(filePath));
                            }
                        }
                        return true;
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message + "\nEncrypt File Error");
                MessageBox.Show("加密文件错误。错误信息：\n" + e.Message);
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
                    else
                    {
                        key = processKey(key);
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
                    else
                    {
                        key = processKey(key);
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
                            if (isSuccess)
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
            try
            {
                using (FileStream fs = new FileStream(filePath, FileMode.Open,FileAccess.Read))
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
                            byte[] des_check = decrypt_aes(buffer, key);
                            //检测是否正确
                            if (des_check == null)
                            {
                                return false;
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
                            if (mode == 0)
                            {
                                if (MessageBox.Show("文件解密完成，是否打开文件夹？", "完成", MessageBoxButton.YesNo, MessageBoxImage.Information) == MessageBoxResult.Yes)
                                {
                                    Process.Start(Path.GetDirectoryName(filePath));
                                }
                            }
                            return true;
                        }
                        else
                        {
                            if (mode == 0)
                            {
                                MessageBox.Show("这不是一个由本程序加密过的文件。");
                            }
                            return false;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message + "\nEncrypt File Error");
                MessageBox.Show("解密文件错误。错误信息：\n" + e.Message);
                return false;
            }
        }
        //加密算法
        public static byte[] encrypt_aes(byte[] block, string key)
        {
            byte[] keyArray = UTF8Encoding.UTF8.GetBytes(key);
            RijndaelManaged rdel = new RijndaelManaged();
            rdel.Key = keyArray;
            rdel.Mode = CipherMode.ECB;
            rdel.Padding = PaddingMode.PKCS7;
            ICryptoTransform transform = rdel.CreateEncryptor();
            return transform.TransformFinalBlock(block, 0, block.Length);
        }
        //解密算法
        public static byte[] decrypt_aes(byte[] block, string key)
        {
            byte[] keyArray = UTF8Encoding.UTF8.GetBytes(key);
            RijndaelManaged rDel = new RijndaelManaged();
            rDel.Key = keyArray;
            rDel.Mode = CipherMode.ECB;
            rDel.Padding = PaddingMode.PKCS7;
            ICryptoTransform cTransform = rDel.CreateDecryptor();

            try
            {
                return cTransform.TransformFinalBlock(block, 0, block.Length);
            }
            catch (CryptographicException e)
            {
                MessageBox.Show("您输入的密钥有误。","错误",MessageBoxButton.OK,MessageBoxImage.Error);
                return null;
            }
        }
        //对key进行MD5处理
        public static string processKey(string key)
        {
            byte[] source = Encoding.UTF8.GetBytes(key);
            MD5 md5 = MD5.Create();
            byte[] result = md5.ComputeHash(source);
            StringBuilder strb = new StringBuilder(40);
            for (int i = 0; i < result.Length; i++)
            {
                strb.Append(result[i].ToString("x2"));
            }
            return strb.ToString();
        }

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
            string configPath = startupDirectory + "\\config.ini";
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

            //检查自动更新配置
            string updateConfigPath = startupDirectory + "\\updater.ini";
            IniFile updateini = new IniFile(updateConfigPath);
            string _name = updateini.readValue("app", "name");
            if (_name == "")
            {
                updateini.writeValue("app", "name", "br_encryptor");
            }

            //拉起自动更新
            Process.Start(startupDirectory + "\\br_updater.exe");
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

        //实时更新ini
        private void cb_selfextract_Checked(object sender, RoutedEventArgs e)
        {

        }

        private void cb_selfextract_Unchecked(object sender, RoutedEventArgs e)
        {

        }
    }
}
