using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Windows;
using MahApps.Metro.Controls;
using BackRunner;

namespace Updater
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        public MainWindow()
        {
            InitializeComponent();
            startup();
        }

        //版本号
        public const string version = "beta 1";
        public const int build = 2;

        //应用信息
        public static string startupPath = Process.GetCurrentProcess().MainModule.FileName;
        public static string startupDirectory = Path.GetDirectoryName(startupPath);

        //ini
        public IniFile ini = new IniFile(startupDirectory + "\\updater.ini");

        //更新配置
        public string appName = "";
        public int maxRetry = 5;
        public const string SERVER = "http://static.backrunner.top/app_updates";

        //DLLImport
        [DllImport("wininet.dll")]
        private extern static bool InternetGetConnectedState(int Description, int ReservedValue);

        //启动
        public void startup()
        {
            //程序名称检查
            string appName = Path.GetFileName(startupPath);
            if (appName != "br_updater.exe")
            {
                MessageBox.Show("请勿重命名程序(br_updater.exe)，这将造成程序功能错误！","错误",MessageBoxButton.OK,MessageBoxImage.Error);
                //错误则退出
                Environment.Exit(0);
            }

            //应用ini检查
            if (!File.Exists(startupDirectory + "\\config.ini"))
            {
                Environment.Exit(0);
            }

            //ini检查

            //检查应用配置
            string _app = ini.readValue("app", "name");
            if (_app.Trim() != "")
            {
                appName = _app.Trim();
            }
            else
            {
                //无配置，退出
                System.Environment.Exit(0);
            }
            //版本检查            
            string _version = ini.readValue("updater", "version");
            if (_version == "")
            {
                ini.writeValue("updater", "version", version);
            }
            string _build = ini.readValue("updater", "build");
            if (_build == "")
            {
                ini.writeValue("updater", "build", build.ToString());
            }
            else
            {
                //高版本ini覆盖
                if (build > int.Parse(_build))
                {
                    ini.writeValue("updater", "version", version);
                    ini.writeValue("updater", "build", build.ToString());
                }
            }
            //设置检查
            string _maxRetry = ini.readValue("updater", "maxRetry");
            if (_maxRetry == "")
            {
                ini.writeValue("updater", "maxRetry", maxRetry.ToString());
            }
            else
            {
                maxRetry = int.Parse(_maxRetry);
            }

            //检查网络连接
            checkInternet();

            //获取更新信息
            getVerInfo("br_updater");
            getVerInfo(appName);

            //流程执行完毕，退出
            System.Environment.Exit(0);
        }

        //网络连接检查
        void checkInternet()
        {
            if (!IsConnectInternet())
            {
                System.Environment.Exit(0);
            }
        }
        public static bool IsConnectInternet()
        {
            int Description = 0;
            return InternetGetConnectedState(Description, 0);
        }

        void getVerInfo(string appName)
        {
            //创建缓存目录
            string cachePath = startupDirectory + "\\cache";
            if (!Directory.Exists(cachePath))
            {
                Directory.CreateDirectory(cachePath);
            }

            //定义下载器
            BRDownloader downloader = new BRDownloader();
            string verFilePath = SERVER + "/" + appName + "/ver";
            string localverFilePath = cachePath + "\\" + appName + ".ver";

            //检查文件是否存在
            if (!downloader.existFile(verFilePath))
            {
                //不存在直接返回
                return;
            }

            //检查本地文件是否存在，是则删除
            if (File.Exists(localverFilePath))
            {
                File.Delete(localverFilePath);
            }

            //下载ver文件
            //重试次数
            int count = 0;
            bool isSuccess = true;
            while (!downloader.downloadFile(localverFilePath, verFilePath))
            {
                count++;
                if (count > maxRetry)
                {
                    isSuccess = false;
                    break;
                }
            }
            //下载失败
            if (!isSuccess)
            {
                File.Delete(cachePath + "\\" + appName + ".ver");
                checkInternet();
            }
            else
            {
                //下载成功
                IniFile verFile = new IniFile(localverFilePath);
                //解析ver文件
                string remotebuild = verFile.readValue("ver", "build");
                string remoteversion = verFile.readValue("ver", "version");
                if (remotebuild != "")
                {
                    //是否是更新器的检查
                    if (appName != "br_updater")
                    {
                        //更新应用
                        IniFile appini = new IniFile(startupDirectory + "\\config.ini");
                        string localbuild = appini.readValue("app", "build");
                        if (localbuild != "")
                        {
                            if (int.Parse(remotebuild) > int.Parse(localbuild))
                            {
                                if (MessageBox.Show("检测到应用有更新，是否更新？", "更新", MessageBoxButton.YesNo, MessageBoxImage.Information) == MessageBoxResult.No)
                                {
                                    return;
                                }
                                updateApp(appName, remoteversion);
                                return;
                            }
                        }
                        else
                        {
                            //版本未配置，退出
                            System.Environment.Exit(0);
                        }
                    }
                    else
                    {
                        //更新更新器
                        if (int.Parse(remotebuild) > build)
                        {
                            if (MessageBox.Show("检测到自动更新器有更新，是否更新？", "更新", MessageBoxButton.YesNo, MessageBoxImage.Information) == MessageBoxResult.No)
                            {
                                return;
                            }
                            updateApp(appName, remoteversion);
                            return;
                        }
                    }
                }
            }
        }

        void updateApp(string appName, string version)
        {
            //下载器
            BRDownloader downloader = new BRDownloader();

            //路径定义
            string remotePackage = SERVER + "/" + appName + "/packages" + "/" + version + ".zip";
            string cachePath = startupDirectory + "/cache";
            string localPackage = cachePath + "\\" + appName + "_" + version + ".zip";
            string extractDirectory = cachePath + "\\" + appName + "\\";

            //检测更新包是否存在
            if (downloader.existFile(remotePackage))
            {
                //检查本地包是否存在，存在则删除
                if (File.Exists(localPackage))
                {
                    File.Delete(localPackage);
                }

                //下载包
                int count = 0;
                bool isSuccess = true;
                while (!downloader.downloadFile(localPackage, remotePackage))
                {
                    count++;
                    if (count > maxRetry)
                    {
                        isSuccess = false;
                        break;
                    }
                }
                if (!isSuccess)
                {
                    //下载失败
                    MessageBox.Show("无法获取更新包", "更新失败", MessageBoxButton.OK, MessageBoxImage.Error);
                    this.Visibility = Visibility.Hidden;
                }
                else
                {
                    //下载成功
                    try
                    {
                        if (Directory.Exists(extractDirectory))
                        {
                            Directory.Delete(extractDirectory, true);
                        }
                        FileStream package = new FileStream(localPackage, FileMode.Open);
                        ZipArchive zip = new ZipArchive(package, ZipArchiveMode.Update);
                        zip.ExtractToDirectory(extractDirectory);
                        package.Close();
                    }
                    catch (Exception e)
                    {
                        MessageBox.Show(e.ToString());
                        MessageBox.Show("无法解压更新包", "更新失败", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    //检查更新脚本
                    if (File.Exists(extractDirectory + "update.exe"))
                    {
                        Process.Start(extractDirectory + "update.exe");
                    }
                    else
                    {
                        if (File.Exists(extractDirectory + "update.bat"))
                        {
                            Process.Start(extractDirectory + "update.bat");
                        }
                        else
                        {
                            MessageBox.Show("未找到更新程序或脚本", "更新失败", MessageBoxButton.OK, MessageBoxImage.Error);
                            Directory.Delete(extractDirectory,true);
                            return;
                        }
                    }
                }
            }
            else
            {
                MessageBox.Show("无法获取更新包", "更新失败", MessageBoxButton.OK, MessageBoxImage.Error);
                this.Visibility = Visibility.Hidden;
                return;
            }
        }
    }
}
