using System;
using System.IO;
using System.Net;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Updater
{
    class BRDownloader
    {
        public bool downloadFile(string localFilePath,string URL)
        {
            bool isSuccess = false;
            //上次的位置
            long lastPosition = 0;
            FileStream fs;
            fs = new FileStream(localFilePath, FileMode.OpenOrCreate);
            //读取文件长度，实现断点续传
            lastPosition = fs.Length;
            fs.Seek(lastPosition, SeekOrigin.Current);
            try
            {
                //建立request
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(URL);
                if (lastPosition > 0)
                {
                    request.AddRange(range: lastPosition);
                }
                Stream stream = request.GetResponse().GetResponseStream();
                byte[] content = new byte[512];
                int size = 0;
                size = stream.Read(content, 0, 512);
                while (size > 0)
                {
                    //将流的数据写入文件
                    fs.Write(content, 0, 512);
                    size = stream.Read(content, 0, 512);
                }
                fs.Flush();
                fs.Close();
                isSuccess = true;
            } catch (Exception e)
            {
                fs.Close();
                isSuccess = false;
            }
            //返回值为是否成功
            return isSuccess;
        }
    }
}
