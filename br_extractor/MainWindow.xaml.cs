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
        public static string cachePath = startupDirectory + "/" + Path.GetFileNameWithoutExtension(startupPath) + ".brtemp";

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
                            string des_path = Path.GetDirectoryName(filePath) + "\\" + Path.GetFileNameWithoutExtension(filePath).Replace(".brencrypted","") + extension;                            
                            byte[] buffer;
                            buffer = br.ReadBytes(256);
                            //第一个块为密钥检测
                            byte[] des_check = decrypt_aes(buffer, key);
                            //检测是否正确
                            if (des_check == null)
                            {
                                br.Close();
                                File.Delete(cachePath);
                                return false;
                            }
                            //如果文件存在
                            if (File.Exists(des_path))
                            {
                                if (MessageBox.Show("当前文件：\n" + Path.GetFileName(filePath).Replace(".brtemp","") + "\n解密后的文件已存在，是否覆盖？", "提示", MessageBoxButton.YesNo, MessageBoxImage.Information) == MessageBoxResult.No)
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
                            File.Delete(cachePath);
                            if (MessageBox.Show("文件解密完成，是否打开文件？", "完成", MessageBoxButton.YesNo, MessageBoxImage.Information) == MessageBoxResult.Yes)
                            {
                                Process.Start(filePath);
                            }                            
                        }
                    }
                }
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message + "\nEncrypt File Error");
                MessageBox.Show("解密文件错误。错误信息：\n" + e.Message);
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
                MessageBox.Show("您输入的密钥有误。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
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

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            string key = processKey(pwd_input.Password);
            if (extract_file())
            {
                if(!decrypt_file(cachePath, key))
                {
                    Environment.Exit(0);
                }
            } else
            {
                File.Delete(cachePath);
                Environment.Exit(0);
            }            
        }
    }
}
