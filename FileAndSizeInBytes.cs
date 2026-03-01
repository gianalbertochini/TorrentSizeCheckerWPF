using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TorrentSizeCheckerWPF
{
    public class FileAndSizeInBytes
    {

        public string FileName; // "" if no filename entered
        public long SizeInBytes; // 0 if no size entered
        public string FileCompleteName; //"" if no filecompletename entered

        public FileAndSizeInBytes(string fileName, long sizeInBytes, string fileCompleteName)
        {
            FileName = fileName;
            SizeInBytes = sizeInBytes;
            FileCompleteName = fileCompleteName;
        }
    }
}
