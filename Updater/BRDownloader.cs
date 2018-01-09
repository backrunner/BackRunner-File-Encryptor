using System;
using System.IO;
using System.Net;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Updater;
using System.Windows;
using System.Web;

namespace BackRunner
{
    class BRDownloader
    {
        public static long currentFileLength = 0;
        public static long downloadedLength = 0;

        private MainWindow mainWindow = (MainWindow)Application.Current.MainWindow;
        public bool downloadFile(string localFilePath,string URL)
        {
            bool isSuccess = true;
            try
            {
                using (WebClient wc = new WebClient())
                {
                    wc.DownloadFile(new Uri(URL), localFilePath);
                }
            } catch (Exception e)
            {
                MessageBox.Show(e.ToString());
                isSuccess = false;
            }
            return isSuccess;
        }

        public bool existFile(string URL)
        {
            int count = 0;
            bool isSuccess = true;
            while (!checkFileExist(URL))
            {
                count++;
                if (count > 5)
                {
                    isSuccess = false;
                    break;
                }
            }
            return isSuccess;
        }

        private bool checkFileExist(string URL)
        {
            HttpWebRequest req = null;
            HttpWebResponse res = null;
            try
            {
                req = (HttpWebRequest)WebRequest.Create(URL);
                req.Method = "HEAD";
                req.Timeout = 300;
                res = (HttpWebResponse)req.GetResponse();
                return (res.StatusCode == HttpStatusCode.OK);
            }
            catch
            {
                return false;
            }
            finally
            {
                //回收资源
                if (res != null)
                {
                    res.Close();
                    res = null;
                }
                if (req != null)
                {
                    req.Abort();
                    req = null;
                }
            }
        }
    }
}
