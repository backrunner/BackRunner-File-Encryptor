using System;
using System.IO;
using System.Text;
using System.Windows;
using MahApps.Metro.Controls;
using Microsoft.Win32;
using System.Security.Cryptography;

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

        public string filePath;
        private void btn_selectFile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.ValidateNames = true;
            ofd.CheckPathExists = true;
            ofd.CheckFileExists = true;
            if (ofd.ShowDialog() == true)
            {
                filePath = ofd.FileName;
                txt_file.Text = filePath;
                //其他代码
            }
        }

        //加密
        private void Button_Click(object sender, RoutedEventArgs e)
        {
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
            try
            {
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
                        bw = new BinaryWriter(new FileStream(Path.GetDirectoryName(filePath) + "\\" + Path.GetFileNameWithoutExtension(filePath) + ".brencrypted", FileMode.Create));
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
                        MessageBox.Show("文件加密完成");
                    }
                }
            }
            catch (IOException ioe)
            {
                Console.WriteLine(ioe.Message + "\nEncrypt File Error");
            }
        }
        //文件解密
        private void btn_decrypt_Click_1(object sender, RoutedEventArgs e)
        {
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
            try
            {
                using (FileStream fs = new FileStream(filePath, FileMode.Open))
                {
                    using (BinaryReader br = new BinaryReader(fs, new UTF8Encoding()))
                    {
                        //获取扩展名
                        string extension = System.IO.Path.GetExtension(filePath);
                        //扩展名检测
                        if (extension == ".brencrypted")
                        {
                            //读取写在文件头的扩展名
                            byte[] extension_byte = br.ReadBytes(64);
                            extension = Encoding.UTF8.GetString(extension_byte).Replace("\0", "");
                            //解密后文件地址
                            string des_path = Path.GetDirectoryName(filePath) + "\\" + Path.GetFileNameWithoutExtension(filePath) + extension;
                            //如果文件存在
                            if (File.Exists(des_path))
                            {
                                if(MessageBox.Show("解密后的文件已存在，是否覆盖？", "提示", MessageBoxButton.YesNo, MessageBoxImage.Information) == MessageBoxResult.No)
                                {
                                    return;
                                }
                            }
                            byte[] buffer;
                            buffer = br.ReadBytes(256);
                            //第一个块为密钥检测
                            byte[] des_check = decrypt_aes(buffer, key);
                            //检测是否正确
                            if (des_check == null)
                            {
                                return;
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
                            MessageBox.Show("文件解密完成");
                        }
                        else
                        {
                            MessageBox.Show("这不是一个由本程序加密过的文件。");
                            return;
                        }
                    }
                }
            }
            catch (IOException ioe)
            {
                Console.WriteLine(ioe.Message + "\nDecrypt File Error");
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
            } catch (CryptographicException e)
            {
                MessageBox.Show("您输入的密钥有误");
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
            //注册表检查

        }
    }
}
