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
using MahApps.Metro.Controls;
using System.IO;
using Microsoft.Win32;
using System.Security.Cryptography;
using System.Diagnostics;

namespace br_extractor
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

        //应用路径信息
        public static string startupPath = Process.GetCurrentProcess().MainModule.FileName;
        public static string startupDirectory = Path.GetDirectoryName(startupPath);
        public static string tempPath = Environment.GetEnvironmentVariable("TEMP");
        public static string cachePath = tempPath + "/" + Path.GetFileNameWithoutExtension(startupPath) + ".brtemp";
        public static string execachePath = tempPath + "/" + "brextractor.exe.brtemp";
        public static string encryptedFilePath = tempPath + "/" + Path.GetFileNameWithoutExtension(startupPath) + ".brencrypted";
        public static string key = "";
        public static string des_path = "";

        public static short processKeyMode = 2;

        //系统API
        [System.Runtime.InteropServices.DllImport("kernel32.dll")]
        public static extern uint WinExec(string lpCmdLine, uint uCmdShow);

        private bool extract_file()
        {
            try
            {
                bool findSign = false;
                //定义文件流                
                BinaryReader br = new BinaryReader(new FileStream(startupPath,FileMode.Open,FileAccess.Read), new UTF8Encoding());
                BinaryWriter bw = new BinaryWriter(new FileStream(cachePath, FileMode.Create));
                BinaryWriter bw_exe = new BinaryWriter(new FileStream(execachePath, FileMode.Create));

                byte[] buffer = new byte[256];
                buffer = br.ReadBytes(256);
                //判断文件区域
                while (buffer.Length > 0)
                {
                    if (Encoding.UTF8.GetString(buffer, 0, buffer.Length).Trim() == "brextract")
                    {
                        findSign = true;
                        break;
                    } else
                    {
                        bw_exe.Write(buffer);
                    }
                    buffer = br.ReadBytes(256);
                }
                if (findSign)
                {
                    buffer = br.ReadBytes(256);
                    while (buffer.Length > 0)
                    {
                        bw.Write(buffer);
                        buffer = br.ReadBytes(256);
                    }
                    bw.Flush();
                    bw.Close();
                    bw_exe.Close();
                    br.Close();
                }
                else
                {
                    MessageBox.Show("提取加密文件时发生错误，未找到加密文件分割标识。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    bw.Flush();
                    bw.Close();
                    bw_exe.Close();
                    //删除exe缓存
                    File.Delete(execachePath);
                    br.Close();
                    return false;
                }
            } catch (Exception e)
            {
                MessageBox.Show("提取加密文件时发生错误。错误信息:\n\n"+e.Message, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            return true;
        }

        //解密逻辑
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
                        if (extension == ".brtemp")
                        {
                            //读取写在文件头的扩展名
                            byte[] extension_byte = br.ReadBytes(64);
                            extension = Encoding.UTF8.GetString(extension_byte).Replace("\0","");
                            //解密后文件地址
                            des_path = Path.GetDirectoryName(filePath) + "\\" + Path.GetFileNameWithoutExtension(filePath).Replace(".brencrypted","").Replace(".brtemp","") + extension;                            
                            byte[] buffer;
                            buffer = br.ReadBytes(256);
                            //第一个块为密钥检测
                            byte[] des_check = decrypt_aes(buffer, key,0,true);
                            //检测是否正确
                            if (des_check == null)
                            {
                                br.Close();
                                File.Delete(cachePath);
                                return false;
                            } else
                            {
                                key = processKey(key);
                            }
                            //如果文件存在
                            if (File.Exists(des_path))
                            {
                                File.Delete(des_path);
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
                            File.Delete(cachePath);
                            Process p = Process.Start(des_path);
                            this.ShowInTaskbar = false;
                            this.Visibility = Visibility.Hidden;
                            p.WaitForExit();
                            //重打包文件
                            if (!repackfile(des_path, execachePath))
                            {
                                Environment.Exit(0);
                            }
                        }
                    }
                }
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message + "\nEncrypt File Error");
                MessageBox.Show("错误信息：\n" + e.Message,"错误",MessageBoxButton.OK,MessageBoxImage.Error);
                try
                {
                    File.Delete(cachePath);
                } catch (Exception e2)
                {
                    throw (e2);
                }
                return false;
            }
        }
        
        private bool repackfile(string filePath,string exePath)
        {
            if (encrypt_file(filePath, key))
            {
                if (bind_file(encryptedFilePath, exePath))
                {
                    //生成自删除bat
                    string batPath = startupDirectory + "\\" + Path.GetFileNameWithoutExtension(startupPath) + ".brtemp.bat";
                    StreamWriter sw = new StreamWriter(new FileStream(batPath, FileMode.Create),Encoding.Default);
                    string bat = ":deletefile\r\n  del \"" + startupPath + "\"\r\nif exist \"" + startupPath + "\" goto deletefile\r\nren \"" + Path.GetFileNameWithoutExtension(filePath) + ".new.brencrypted.exe\" \"" + Path.GetFileName(startupPath) + "\"\r\ndel \""+batPath.Replace("\\\\","\\")+"\"";
                    sw.Write(bat);
                    sw.Flush();
                    sw.Close();
                    WinExec(batPath, 0);
                    Environment.Exit(0);
                }
                else
                {
                    return false;
                }
            } else
            {
                return false;
            }
            return true;
        }

        private bool bind_file(string filePath, string extractorPath)
        {
            //检查自解压文件是否存在
            if (File.Exists(extractorPath))
            {
                //路径定义 & 流定义
                string cachePath = filePath;
                string outputPath = startupDirectory + "/" + Path.GetFileNameWithoutExtension(filePath).Replace(".brencrypted","") + ".new.brencrypted.exe";
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
                    File.Delete(des_path);
                    File.Delete(extractorPath);
                    File.Delete(encryptedFilePath);
                }
                catch (Exception e)
                {
                    File.Delete(cachePath);
                    File.Delete(des_path);
                    File.Delete(outputPath);
                    File.Delete(extractorPath);
                    File.Delete(encryptedFilePath);
                    MessageBox.Show("文件没有自更新成功：\n\n" + e.Message, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }
            }
            return true;
        }

        //加密文件
        private bool encrypt_file(string filePath, string key)
        {
            try
            {                
                if (File.Exists(encryptedFilePath))
                {
                  File.Delete(encryptedFilePath);
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

                        key = processKey(key);

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
                        return true;
                    }
                }
            }
            catch (Exception e)
            {
                MessageBox.Show("加密文件错误。错误信息：\n" + e.Message);
                return false;
            }
        }

        //加密算法
        public static byte[] encrypt_aes(byte[] block, string key)
        {
            RijndaelManaged rdel = new RijndaelManaged();
            switch (processKeyMode)
            {
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
                    keyArray = Encoding.UTF8.GetBytes(key.Substring(92, 32));
                    string _iv = key.Substring(31, 16);
                    iv = Encoding.UTF8.GetBytes(_iv);
                    rdel.Mode = CipherMode.CFB;
                    rdel.Padding = PaddingMode.PKCS7;
                    transform = rdel.CreateEncryptor(keyArray, iv);
                    return transform.TransformFinalBlock(block, 0, block.Length);
            }
        }
        //解密算法
        public static byte[] decrypt_aes(byte[] block, string key, short retry = 0, bool istry = false)
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
                if (retry >= 1)
                {
                    MessageBox.Show("尝试解密错误，请检查你输入的密钥。\n\n错误信息：\n" + e.Message, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    return null;
                }
                else
                {
                    processKeyMode++;
                    if (processKeyMode > 2)
                    {
                        processKeyMode = 1;
                    }
                    return decrypt_aes(block, originKey, ++retry, true);
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

        private void btn_submit_Click(object sender, RoutedEventArgs e)
        {
            key = pwd_input.Password;            
            if (extract_file())
            {
                if (!decrypt_file(cachePath, key))
                {
                    Environment.Exit(0);
                }
            }
            else
            {
                File.Delete(cachePath);
                Environment.Exit(0);
            }
        }

        private void pwd_input_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                btn_submit_Click(sender, e);
            }
        }
    }
}
