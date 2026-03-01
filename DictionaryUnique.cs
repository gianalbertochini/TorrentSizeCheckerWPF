using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TorrentSizeCheckerWPF
{
    internal class DictionaryUnique
    {
        public Dictionary<string, List<FileAndSizeInBytes>> dic = new Dictionary<string, List<FileAndSizeInBytes>>();

        public DictionaryUnique()
        {
            this.dic = new Dictionary<string, List<FileAndSizeInBytes>>();
        }
        public void DeleteDictionary()
        {
            dic = new Dictionary<string, List<FileAndSizeInBytes>>();
        }
        public bool AddFilesToDictionary(string nameFileTorrent, FileAndSizeInBytes file)
        {
            if ((nameFileTorrent == null) || (file == null)) return false;

            if (dic.ContainsKey(nameFileTorrent))
            {
                dic[nameFileTorrent].Add(file);
            }
            else
            {
                List<FileAndSizeInBytes> list = [file];
                dic.Add(nameFileTorrent, list);
            }
            return true;
        }

        public bool AddFilesToDictionary(string nameFileTorrent, List<FileAndSizeInBytes> files)
        {
            if ((nameFileTorrent == null) || (files == null)) return false;

            List<FileAndSizeInBytes> list;
            if (dic.ContainsKey(nameFileTorrent))
            {
                list = dic[nameFileTorrent];
            }
            else
            {
                list = new List<FileAndSizeInBytes>();
                dic.Add(nameFileTorrent, list);
            }

            foreach (FileAndSizeInBytes file in files)
            {
                list.Add(file);
            }
            return true;
        }

        public List<FileAndSizeInBytes> getListUnique(string nameFileTorrent)
        {
            if (dic.ContainsKey(nameFileTorrent))
            {
                return dic[nameFileTorrent];
            }
            else
            {
                return [];
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

    }
}
