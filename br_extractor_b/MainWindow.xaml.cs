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

        public static short processKeyMode = 2;

        private bool extract_file()
        {
            try
            {
                bool findSign = false;
                //定义文件流                
                BinaryReader br = new BinaryReader(new FileStream(startupPath,FileMode.Open,FileAccess.Read), new UTF8Encoding());
                BinaryWriter bw = new BinaryWriter(new FileStream(cachePath, FileMode.Create));
                byte[] buffer = new byte[256];
                buffer = br.ReadBytes(256);
                //判断文件区域
                while (buffer.Length > 0)
                {
                    if (Encoding.UTF8.GetString(buffer, 0, buffer.Length).Trim() == "brextract")
                    {
                        findSign = true;
                        break;
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
                    br.Close();
                }
                else
                {
                    MessageBox.Show("提取加密文件时发生错误，未找到加密文件分割标识。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    bw.Flush();
                    bw.Close();
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
                            extension = Encoding.UTF8.GetString(extension_byte).Replace("\0", "");
                            //解密后文件地址
                            string des_path = Path.GetDirectoryName(filePath) + "\\" + Path.GetFileNameWithoutExtension(filePath).Replace(".brtemp","") + extension;                            
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
                            File.Delete(des_path);
                            Environment.Exit(0);
                        }
                    }
                }
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message + "\nEncrypt File Error");
                MessageBox.Show("解密文件错误。错误信息：\n" + e.Message,"错误",MessageBoxButton.OK,MessageBoxImage.Error);
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
            string key = pwd_input.Password;
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
    }
}
