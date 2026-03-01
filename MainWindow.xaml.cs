using System;
using System.Collections.Generic;
using System.IO;
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
using System.Windows.Shapes;
using Path = System.IO.Path;
using BencodeNET.Parsing;
using BencodeNET.Objects;
using BencodeNET.Torrents;
using System.Reflection.Emit;
using System.Security;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace TorrentSizeCheckerWPF
{
    public sealed class DuplicateRow
    {
        public long Size { get; init; }
        public string SizeHR { get; init; } = "";
        public string TorrentFile { get; init; } = "";
        public string FileDBFile { get; init; } = "";
        public string FileDBFullPath { get; init; } = "";
    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 

    public partial class MainWindow : Window
    {
        public static readonly long MinSizeFile = 1024 * 500;
        public static readonly string NameFileDatabase = Path.Combine(Directory.GetCurrentDirectory(), "database.txt");

        private List<FileAndSizeInBytes> ListFileInDatabase = new List<FileAndSizeInBytes>();

        private DictionaryDuplicates dicDuplicates = new DictionaryDuplicates();

        private DictionaryUnique dicUnique = new DictionaryUnique();

        private readonly Dictionary<GridViewColumn, double> _savedColumnWidths = new();
        public MainWindow()
        {
            InitializeComponent();
            checkAndLoadDatabaseFile();
        }

        private void ColumnsContextMenu_Opened(object sender, RoutedEventArgs e)
        {
            if (sender is not ContextMenu menu)
            {
                return;
            }

            menu.Items.Clear();

            if (ListViewDuplicates.View is not GridView gv)
            {
                return;
            }

            foreach (var col in gv.Columns)
            {
                var headerText = GetHeaderText(col) ?? "(Column)";

                var item = new MenuItem
                {
                    Header = headerText,
                    IsCheckable = true,
                    IsChecked = col.Width > 0,
                    Tag = col
                };

                item.Click += ColumnVisibilityMenuItem_Click;
                menu.Items.Add(item);
            }
        }

        private void ColumnVisibilityMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not MenuItem mi || mi.Tag is not GridViewColumn col)
            {
                return;
            }

            if (mi.IsChecked)
            {
                ShowColumn(col);
            }
            else
            {
                HideColumn(col);
            }
        }

        private void HideColumn(GridViewColumn col)
        {
            if (col.Width > 0)
            {
                _savedColumnWidths[col] = col.Width;
            }

            col.Width = 0;
        }

        private void ShowColumn(GridViewColumn col)
        {
            if (_savedColumnWidths.TryGetValue(col, out var w) && w > 0)
            {
                col.Width = w;
                return;
            }

            // fallback se non era mai stata visibile
            col.Width = 120;
        }

        private static string? GetHeaderText(GridViewColumn col)
        {
            return col.Header switch
            {
                string s => s,
                GridViewColumnHeader h => h.Content?.ToString(),
                _ => col.Header?.ToString()
            };
        }

        public void setLableInvalidDatabase()
        {
            LabelDatabaseUpdateTime.Content = "Invalid database";
            MessageBox.Show("Invalid Database");
            ListFileInDatabase = new List<FileAndSizeInBytes>();
        }

        private static string BytesToHuman(long bytes)
        {
            if (bytes < 0) bytes = 0;

            const long K = 1024;
            const long M = K * 1024;
            const long G = M * 1024;
            const long T = G * 1024;

            if (bytes < K) return $"{bytes}B";
            if (bytes < M) return $"{(double)bytes / K:F2}K";
            if (bytes < G) return $"{(double)bytes / M:F2}M";
            if (bytes < T) return $"{(double)bytes / G:F2}G";
            return $"{(double)bytes / T:F2}T";
        }


        private void checkAndLoadDatabaseFile()
        {
            List<string> databaseList;
            try
            {
                //Create file if not exists
                if (!File.Exists(NameFileDatabase))
                {
                    MessageBox.Show("Il database non esiste ancora. Sarà creato in: " + NameFileDatabase);
                    using (StreamWriter w = File.AppendText(NameFileDatabase))
                    {
                        w.WriteLine(); //Add an empty line for the directory path
                        w.WriteLine(); //Add an empty line for the date
                        w.WriteLine(); //Add an empty line for the source directory of the torrent
                        w.WriteLine(); //Add an empty line for the destination directory of the torrent
                    }
                }
                //Load all file into memory 
                databaseList = [.. File.ReadAllLines(NameFileDatabase)];

                if (databaseList.Count < 4)
                {
                    MessageBox.Show("Il database è invalido. Il database deve avere almeno 4 linee. Cancellare o verificare il file in: " + NameFileDatabase);
                    setLableInvalidDatabase();
                    return;
                }    
            }
            catch (Exception)
            {
                MessageBox.Show("Impossibile creare il database. Verificare le permissions in: " + NameFileDatabase);
                setLableInvalidDatabase();
                return;
            }

            
            //Put le first line on the TextBoxAggiornaDatabase and delete it from databaseList
            TextBoxRoot.Text = databaseList.First();
            databaseList.RemoveAt(0);
            //Put the second line (but is the first in the array now) in the LabelDatabaseUpdateTime and delete it from databaseList
            if (databaseList.First() != "")
            {
                try
                {
                    LabelDatabaseUpdateTime.Content = DateTime.Parse(databaseList.First()).ToString();
                }
                catch (Exception)
                {
                    setLableInvalidDatabase();
                    return;
                }
            }
            databaseList.RemoveAt(0);
            // Put the third line (but is the first in the array now) on the TextBoxSourceDir and delete it from databaseList
            TextBoxSourceDir.Text = databaseList.First();
            databaseList.RemoveAt(0);
            // Put the fourth line (but is the first in the array now) on the TextBoxDestDir and delete it from databaseList
            TextBoxDestDir.Text = databaseList.First();
            databaseList.RemoveAt(0);

            TextBoxRoot.Background = new SolidColorBrush(Colors.White);
            TextBoxSourceDir.Background = new SolidColorBrush(Colors.White);
            TextBoxDestDir.Background = new SolidColorBrush(Colors.White);

            //Fill the listFileInDatabase
            ListFileInDatabase = new List<FileAndSizeInBytes>();
            for (int i = 0; i < databaseList.Count; i = i + 2)
            {
                FileAndSizeInBytes f = new("", 0, "");
                try
                {
                    f.FileName = Path.GetFileName(databaseList[i]);
                    f.SizeInBytes = long.Parse(databaseList[i + 1]);
                    f.FileCompleteName = databaseList[i];
                }
                catch (Exception)
                {
                    f = new FileAndSizeInBytes("", 0, "");
                }

                if (f.SizeInBytes != 0 && f.FileName.Length != 0)
                {
                    ListFileInDatabase.Add(f);
                }
            }

        }

        public static List<string> ApplyAllFiles(string folder, bool recursive)
        {
            List<string> strOut = new List<string>();
            foreach (string file in Directory.GetFiles(folder))
            {
                strOut.Add(file);
            }
            if (recursive)
            {
                foreach (string subDir in Directory.GetDirectories(folder))
                {
                    try
                    {
                        strOut.AddRange(ApplyAllFiles(subDir, true));
                    }
                    catch (Exception)
                    {
                        // swallow, log, whatever
                    }
                }
            }
            return strOut;
        }

        List<FileAndSizeInBytes> getfilesInTorrent(string s)
        {
            List<FileAndSizeInBytes> ret = new List<FileAndSizeInBytes>();

            try
            {
                var parser = new BencodeParser(); // Default encoding is Encoding.UT8F, but you can specify another if you need to
                Torrent torrent;

                // Alternatively, handle the stream yourself
                using (var stream = File.OpenRead(s))
                {
                    torrent = parser.Parse<Torrent>(stream);
                }


                SingleFileInfo torrentFile = torrent.File;
                MultiFileInfoList torrentFiles = torrent.Files;

                if (torrentFile != null)
                {
                    ret.Add(new FileAndSizeInBytes(torrentFile.FileName, torrentFile.FileSize, torrentFile.FileName));
                }

                if (torrentFiles != null)
                {
                    foreach (MultiFileInfo f in torrentFiles)
                    {
                        ret.Add(new FileAndSizeInBytes(f.FileName, f.FileSize, f.FullPath));
                    }
                }
            }
            catch
            {
            }
            return ret;
        }


        private void ButtonUpdateDatabaseFile_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show("Sei sicuro di voler ricostruire il database dei file? Questo richiedera un po di tempo.", "Conferma", MessageBoxButton.YesNo);
            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    File.Delete(NameFileDatabase);
                    if (!Directory.Exists(TextBoxRoot.Text))
                    {
                        MessageBox.Show("The path of the ROOT directory is not a directory");
                        return;
                    }
                    using (StreamWriter w = File.AppendText(NameFileDatabase))
                    {
                        w.WriteLine(TextBoxRoot.Text); //Aggiungi al database come prima linea la directory root di esplorazione dei file
                        w.WriteLine(DateTime.Now); //Aggiungi al database come seconda linea la data di aggiornamento del database
                        w.WriteLine(TextBoxSourceDir.Text); //Aggiungi al database come terza linea la directory sorgente dei torrent 
                        w.WriteLine(TextBoxDestDir.Text); //Aggiungi al database come quarta linea la directory di destinazione dei torrent

                        List<string> allfiles = ApplyAllFiles(TextBoxRoot.Text, true);
                        //MessageBox.Show("Dati dei file letti. Ci sono "+ allfiles.Count +" files. Adesso analizzo la dimensione");
                        int i = 1;
                        int j = 1000;
                        foreach (string s in allfiles)
                        {
                            if (i % j == 0)
                            {
                                j = j + j;
                                //MessageBox.Show("La dimensione di " + i + " files è stata analizzata. Ne mancano "+ (allfiles.Count-i) + " Ora verranno analizzati altri "+j+" files");
                            }
                            try
                            {
                                long sizefile = new System.IO.FileInfo(s).Length;

                                if (sizefile >= MinSizeFile)
                                {
                                    //Aggiungi il path del file e la dimensione al database
                                    w.WriteLine(s);
                                    w.WriteLine(sizefile);
                                    w.Flush();
                                }
                            }
                            catch (Exception)
                            {

                            }
                            i++;

                        }
                    }
                    MessageBox.Show("Aggiornamento database completato.");
                    TextBoxRoot.Background = new SolidColorBrush(Colors.White);
                    TextBoxSourceDir.Background = new SolidColorBrush(Colors.White);
                    TextBoxDestDir.Background = new SolidColorBrush(Colors.White);
                }
                catch (Exception exc)
                {
                    MessageBox.Show("Something in ButtonUpdateDatabaseFile_Click is wrong. " + exc.Message);
                }
            }
        }

        public void reInsertDataInListDuplicates()
        {
            // per ogni file I presente nella lista del dizionario[alla chiave selezionata]
            //   aggiungi I alla listBoxResult
            ListViewDuplicates.Items.Clear();
            if ((ListBoxTorrent.Items.Count > 0) && (ListBoxTorrent.SelectedIndex >= 0))
            {

                    int numFileStessoNome = 0;
                    var torrentPath = Path.Combine(TextBoxSourceDir.Text, (string)ListBoxTorrent.Items[ListBoxTorrent.SelectedIndex]);

                    List<Tuple<FileAndSizeInBytes, FileAndSizeInBytes>> listDuplicates = dicDuplicates.getListTouple(torrentPath);
                    List<FileAndSizeInBytes> listUnique = dicUnique.getListUnique(torrentPath);
      
                    foreach (Tuple<FileAndSizeInBytes, FileAndSizeInBytes> t in listDuplicates)
                    {
                        ListViewDuplicates.Items.Add(new DuplicateRow
                        {
                            Size = t.Item1.SizeInBytes,
                            SizeHR = BytesToHuman(t.Item1.SizeInBytes),
                            TorrentFile = t.Item1.FileName,
                            FileDBFile = t.Item2.FileName,
                            FileDBFullPath = t.Item2.FileCompleteName
                        });
                        if (t.Item1.FileName == t.Item2.FileName)
                        {
                            numFileStessoNome++;
                        }
                    }
                    for (int i = 0; i < 1; i++) {                         
                        ListViewDuplicates.Items.Add(new DuplicateRow
                        {
                            Size = 0,
                            SizeHR = "===",
                            TorrentFile = "=====================",
                            FileDBFile = "=====================",
                            FileDBFullPath = "============================================"
                        });
                    }
                    foreach (FileAndSizeInBytes s in listUnique)
                    {
                        ListViewDuplicates.Items.Add(new DuplicateRow
                        {
                            Size = s.SizeInBytes,
                            SizeHR = BytesToHuman(s.SizeInBytes),
                            TorrentFile = s.FileName,
                            FileDBFile = "XXXXXXXXXXX",
                            FileDBFullPath = "XXXXXXXXXXX"
                        });
                    }

                    LabelNumeroFileTotali.Content = listDuplicates.Count + listUnique.Count;
                    LabelNumeroFileUguali.Content = listDuplicates.Count;
                    labelNumeroFileDiversi.Content = listUnique.Count;
                    labelNumeroFileStessoNome.Content = numFileStessoNome;

            }
            else
            {
                LabelNumeroFileTotali.Content = "";
                LabelNumeroFileUguali.Content = 0;
                labelNumeroFileStessoNome.Content = 0;
                labelNumeroFileDiversi.Content = 0;
                ListViewDuplicates.Items.Add(new DuplicateRow
                {
                    Size = 0,
                    SizeHR = "NA",
                    TorrentFile = "No Torrent",
                    FileDBFile = "==No Torrent==",
                    FileDBFullPath = "==No Torrent=="
                });
            }
        }


        private void ButtonMoveFile_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!Directory.Exists(TextBoxSourceDir.Text))
                {
                    MessageBox.Show("The path of the SOURCE directory is not a directory. No operation will be done.");
                    return;
                }
                if (!Directory.Exists(TextBoxDestDir.Text))
                {
                    MessageBox.Show("The path of the DEST directory is not a directory. No operation will be done.");
                    return;
                }

                string srcDir = TextBoxSourceDir.Text;
                string destDir = TextBoxDestDir.Text;

                // Crea il dizionario delle liste dei file uguali
                List<string> allFilesInDir = ApplyAllFiles(TextBoxSourceDir.Text, false);
                List<string> torrentToMove = new List<string>(allFilesInDir);
                //per ogni torrent S presente nella cartella
                foreach (string fileInDirectory in allFilesInDir)
                {
                    if (Path.GetExtension(fileInDirectory).ToLower() == ".torrent")
                    {
                        List<FileAndSizeInBytes> filesInTorrent = getfilesInTorrent(fileInDirectory);
                        List<FileAndSizeInBytes> filesInTorrentToKeep = new List<FileAndSizeInBytes>();


                        if (filesInTorrent.Count <= 0)
                        {
                            MessageBox.Show("The torrent " + fileInDirectory + " is a file in the directory that is or empty (it has no files to download) or invalid (is not a .torrent)");
                        }
                        else
                        {
                            //per ogni file F presente nel torrent
                            foreach (FileAndSizeInBytes fileInTorrent in filesInTorrent)
                            {
                                Boolean addTorrentToKeep = true;
                                filesInTorrentToKeep.Add(new FileAndSizeInBytes(fileInTorrent.FileCompleteName, fileInTorrent.SizeInBytes, fileInTorrent.FileCompleteName));
                                //per ogni file G presente nel database
                                foreach (FileAndSizeInBytes fileInDB in ListFileInDatabase)
                                {
                                    //se F.dimensione = G.dimensione
                                    if (fileInTorrent.SizeInBytes == fileInDB.SizeInBytes)
                                    {
                                        //memorizza {F,G} nel dizionario D
                                        dicDuplicates.AddFilesToDictionary(fileInDirectory, fileInTorrent, fileInDB);
                                        torrentToMove.Remove(fileInDirectory);
                                        addTorrentToKeep = false;
                                    }
                                }

                                if (addTorrentToKeep)
                                {
                                    filesInTorrentToKeep.Add(new FileAndSizeInBytes(fileInTorrent.FileCompleteName, fileInTorrent.SizeInBytes, fileInTorrent.FileCompleteName));
                                }
                            }
                        }
                        dicUnique.AddFilesToDictionary(fileInDirectory, filesInTorrentToKeep);
                    }
                }


                // Move the files form the src directory to the destDirectory
                foreach (string s in torrentToMove)
                {
                    try
                    {
                        File.Move(s, Path.Combine(destDir, Path.GetFileName(s)));
                    }
                    catch (Exception)
                    {
                        MessageBox.Show("Something is wrong in moving the torrent " + s + " to the " + destDir + " directory");
                    }
                }


                //Create the torrenToBeChecked List
                List<string> torrentToBeChecked = new List<string>(allFilesInDir);
                foreach (string s in torrentToMove)
                {
                    torrentToBeChecked.Remove(s);
                }

                // Write he dictionary in the ListBoxes

                // per ogni file S presente in dizionario
                //   aggiungi S alla listboxTorrent
                ListBoxTorrent.Items.Clear();
                if (torrentToBeChecked.Count > 0)
                {
                    foreach (string s in torrentToBeChecked)
                    {
                        //ListBoxTorrent.Items.Add(Path.GetFileName(s));
                        ListBoxTorrent.Items.Add(Path.GetFileName(s));
                    }
                }

                reInsertDataInListDuplicates();

            }
            catch (Exception)
            {
                MessageBox.Show("Something in ButtonMoveFile_Click is wrong");
            }



        }

        private void ListBoxTorrent_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            reInsertDataInListDuplicates();
        }

        private void ButtonSpostaComunque_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show("Sei sicuro di spostare comunque i tottent?", "Conferma", MessageBoxButton.YesNo);

            if (result == MessageBoxResult.Yes)
            {
                // Move the files form the src directory to the destDirectory
                if (ListBoxTorrent.SelectedItems.Count > 0)
                {
                    string srcDir = TextBoxSourceDir.Text;
                    string destDir = TextBoxDestDir.Text;
                    string srcFile = "";
                    string destFile = "";
                    try
                    {
                        srcFile = Path.Combine(srcDir, (string)ListBoxTorrent.SelectedItem);
                        destFile = Path.Combine(destDir, (string)ListBoxTorrent.SelectedItem);

                        File.Move(srcFile, destFile);

                        ListBoxTorrent.Items.Remove(ListBoxTorrent.SelectedItem);

                    }
                    catch (Exception)
                    {
                        MessageBox.Show("Something is wrong in moving the torrent " + srcFile + " to the " + destFile + " location");
                    }
                }
                else
                {
                    MessageBox.Show("Nessun file selezionato");
                }
            }

        }

        private void ButtonSelectSourceDir_Click(object sender, RoutedEventArgs e)
        {
            OpenFolderDialog openFolderDialog = new OpenFolderDialog();
            if (openFolderDialog.ShowDialog() == true)
                TextBoxSourceDir.Text = openFolderDialog.FolderName;
        }

        private void ButtonSelectDestDir_Click(object sender, RoutedEventArgs e)
        {
            OpenFolderDialog openFolderDialog = new OpenFolderDialog();
            if (openFolderDialog.ShowDialog() == true)
                TextBoxDestDir.Text = openFolderDialog.FolderName;
        }

        private void ButtonChangeRootPath_Click(object sender, RoutedEventArgs e)
        {
            OpenFolderDialog openFolderDialog = new OpenFolderDialog();
            if (openFolderDialog.ShowDialog() == true)
                TextBoxRoot.Text = openFolderDialog.FolderName;
        }

        private void TextBoxRoot_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBoxRoot.Background = new SolidColorBrush(Colors.LightSalmon);
        }

        private void TextBoxSourceDir_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBoxSourceDir.Background = new SolidColorBrush(Colors.LightSalmon);
        }

        private void TextBoxDestDir_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBoxDestDir.Background = new SolidColorBrush(Colors.LightSalmon);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            HideColumn(HeaderSize.Column);
        }
    }
}
