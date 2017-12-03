using System;
using System.IO;
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
            bool flag = false;
            //上次的位置
            long lastPosition = 0;
            FileStream fs;
            //判断本地文件是否存在
            if (File.Exists(localFilePath))
            {
                fs = new FileStream(localFilePath, FileMode.OpenOrCreate);
                lastPosition = fs.Length;
                fs.Seek(lastPosition, SeekOrigin.Current);
            }

        }
    }
}
