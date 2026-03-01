using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TorrentSizeCheckerWPF
{
    public class DictionaryDuplicates
    {
        // Tuple<FileInTorrent,FileInDatabase>
        public Dictionary<string, List<Tuple<FileAndSizeInBytes, FileAndSizeInBytes>>> dic = new Dictionary<string, List<Tuple<FileAndSizeInBytes, FileAndSizeInBytes>>>();

        public DictionaryDuplicates()
        {
            this.dic = new Dictionary<string, List<Tuple<FileAndSizeInBytes, FileAndSizeInBytes>>>();
        }

        public void DeleteDictionary()
        {
            dic = new Dictionary<string, List<Tuple<FileAndSizeInBytes, FileAndSizeInBytes>>>();
        }

        public bool AddFilesToDictionary(string nameFileTorrent, FileAndSizeInBytes fileInTorrent, FileAndSizeInBytes fileOnHD)
        {
            if ((nameFileTorrent == null) || (fileInTorrent == null) || (fileOnHD == null)) return false;

            if (dic.ContainsKey(nameFileTorrent))
            {
                dic[nameFileTorrent].Add(new Tuple<FileAndSizeInBytes, FileAndSizeInBytes>(fileInTorrent, fileOnHD));
            }
            else
            {
                List<Tuple<FileAndSizeInBytes, FileAndSizeInBytes>> list = new List<Tuple<FileAndSizeInBytes, FileAndSizeInBytes>>();
                list.Add(new Tuple<FileAndSizeInBytes, FileAndSizeInBytes>(fileInTorrent, fileOnHD));
                dic.Add(nameFileTorrent, list);
            }
            return true;
        }

        public List<Tuple<FileAndSizeInBytes, FileAndSizeInBytes>> getListTouple(string nameFileTorrent)
        {
            if (dic.ContainsKey(nameFileTorrent))
            {
                return dic[nameFileTorrent];
            }
            else
            {
                return new List<Tuple<FileAndSizeInBytes, FileAndSizeInBytes>>();
            }

        }

        public bool containsKey(string nameFileTorrent)
        {
            if (dic.ContainsKey(nameFileTorrent))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public int sizeDictionary()
        {
            return dic.Count;
        }

        public List<string> keyList()
        {
            List<string> output = new List<string>();
            foreach (string s in dic.Keys)
            {
                output.Add(s);
            }
            return output;
        }
    }
}
