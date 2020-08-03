using ImageProcessor.Plugins.WebP.Imaging.Formats;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;

namespace DownloadFolderSorter
{
    public partial class MainForm : Form
    {
        FileSystemWatcher weightwatchers;
        bool dfolderExists;
        bool phaseShift;
        int currentMouseOverRow;
        readonly object FileMoveLock = new object();
        readonly object PictureConversionLock = new object();

        // StartUp
        public MainForm()
        {
            InitializeComponent();
        }
        private void MainForm_Load(object sender, EventArgs e)
        {
            weightwatchers = new FileSystemWatcher();
            SetDownloadFolder(Config.Data.downloadFolder);
            LoadDataGrid();
            Task.Factory.StartNew(() => { this.InvokeIfRequired(() => { HideForm(); }); });
            delayPicker.Value = Config.Data.sortDelay / 1000;
            lDelay.Text = $"Delay: {delayPicker.Value} sec.";
        }

        // SetFolder
        private void SetDownloadFolder(string folder)
        {
            dfolderExists = false;

            tFolder.Text = folder;
            Config.Data.downloadFolder = folder;
            Config.Save();

            dfolderExists = Directory.Exists(folder);
            dataGrid.Enabled = dfolderExists;
            bApply.Enabled = dfolderExists;
            lStatus.Enabled = dfolderExists;

            if (!dfolderExists)
                lStatus.Text = "Status: Got no valid Download-Folder!";
            else
            {
                lStatus.Text = "Status: Ready";

                FileSystemEventHandler onChanged = (object source, FileSystemEventArgs e) =>
                {
                    try
                    {
                        SortDownloadFolder();
                    }
                    catch (Exception ex) { MessageBox.Show("Sorting Error: \n" + ex.ToString()); }
                };
                RenamedEventHandler onRenamed = (object source, RenamedEventArgs e) =>
                {
                    try
                    {
                        SortDownloadFolder();
                    }
                    catch (Exception ex) { MessageBox.Show("Sorting Error: \n" + ex.ToString()); }
                };

                weightwatchers.Path = folder;
                weightwatchers.Changed += onChanged;
                weightwatchers.Created += onChanged;
                weightwatchers.Renamed += onRenamed;
                weightwatchers.EnableRaisingEvents = true;
                weightwatchers.IncludeSubdirectories = false;

                SortDownloadFolder();
            }
        }
        private void BBrowser_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog dialog = new FolderBrowserDialog
            {
                SelectedPath = tFolder.Text,
                Description = "Select your Download-Folder"
            };
            if (dialog.ShowDialog() == DialogResult.OK)
                SetDownloadFolder(dialog.SelectedPath);
        }

        // Converters
        private void DatagridIntoConfig()
        {
            Config.Data.Matches.Clear();
            int rows = dataGrid.Rows.Count - 1;
            for (int i = 0; i < rows; i++)
            {
                Config.Data.Matches.Add(new Matching()
                {
                    Name = (string)dataGrid.Rows[i].Cells[0].Value,
                    Match = (string)dataGrid.Rows[i].Cells[1].Value,
                    Target = (string)dataGrid.Rows[i].Cells[2].Value
                });
            }
        }
        private void ConfigIntoDatagrid()
        {
            if (Config.Data.Matches == null)
                return;
            
            dataGrid.Rows.Clear();
            for (int i = 0; i < Config.Data.Matches.Count; i++)
                dataGrid.Rows.Add(new object[] { Config.Data.Matches[i].Name, Config.Data.Matches[i].Match, Config.Data.Matches[i].Target });
        }

        // Actual Sorting
        private void BApply_Click(object sender, EventArgs e)
        {
            DatagridIntoConfig();

            Config.Save();

            SortDownloadFolder();
        }
        private void LoadDataGrid()
        {
            ConfigIntoDatagrid();
        }
        private string CanSort()
        {
            if (!dfolderExists || !Directory.Exists(Config.Data.downloadFolder))
                return "invalid Downlaod Folder";

            if (Config.Data.Matches == null || Config.Data.Matches.Count == 0)
                return "empty Matches";

            for (int i = 0; i < Config.Data.Matches.Count; i++)
                if (!Directory.Exists(Config.Data.Matches[i].Target))
                    return $"{Config.Data.Matches[i].Name}'s target folder is invalid";

            return null;
        }
        private void SortDownloadFolder()
        {
            Task.Factory.StartNew(() => {
                if (!bApply.Enabled)
                    return;

                lock (lStatus)
                {
                    string errorMsg;
                    if ((errorMsg = CanSort()) != null)
                    {
                        lStatus.InvokeIfRequired(() => lStatus.Text = "Status: Error, " + errorMsg);
                        return;
                    }

                    bApply.InvokeIfRequired(() => bApply.Enabled = false);
                    lStatus.InvokeIfRequired(() => lStatus.Text = "Status: Sorting...");
                    Thread.Sleep(Config.Data.sortDelay);
                    List<Thread> sortThreads = new List<Thread>();
                    string[] files = Directory.GetFiles(Config.Data.downloadFolder);
                    for (int i = 0; i < files.Length; i++)
                        for (int j = 0; j < Config.Data.Matches.Count; j++)
                        {
                            if (!string.IsNullOrWhiteSpace(Config.Data.Matches[j].Match))
                            {
                                string[] splitOR = Config.Data.Matches[j].Match.Split('|');
                                for (int k = 0; k < splitOR.Length; k++)
                                {
                                    if (Path.GetFileName(files[i]).ContainsAll(splitOR[k].Split('&')))
                                    {
                                        string fileName = Path.GetFileName(files[i]);
                                        while (File.Exists(Config.Data.Matches[j].Target + "\\" + fileName))
                                        {
                                            int index = fileName.LastIndexOf('(');
                                            if (index != -1)
                                            {
                                                try
                                                {
                                                    string num = new string(fileName.Remove(0, index + 1).TakeWhile(x => char.IsDigit(x)).ToArray());
                                                    int number = Convert.ToInt32(num);
                                                    fileName = fileName.Substring(0, index + 1) + (number + 1) + fileName.Substring(index + 1 + num.Length);
                                                }
                                                catch
                                                {
                                                    fileName = Path.GetFileNameWithoutExtension(fileName) + " (1)" + Path.GetExtension(fileName);
                                                }
                                            }
                                            else
                                            {
                                                fileName = Path.GetFileNameWithoutExtension(fileName) + " (1)" + Path.GetExtension(fileName);
                                            }
                                        }

                                        Thread t = new Thread(new ParameterizedThreadStart(ThreadedFileMove));
                                        sortThreads.Add(t);
                                        t.Name = "SortThread" + sortThreads.Count;
                                        t.Start(new string[] { files[i], Config.Data.Matches[j].Target + "\\" + fileName });

                                        // Display toast
                                        try
                                        {
                                            new Process()
                                            {
                                                StartInfo = new ProcessStartInfo("toast.exe", $"-h \"Downloaded File\" -m \"{fileName}\" -f \"To {Config.Data.Matches[j].Target}\"")
                                                { UseShellExecute = false, CreateNoWindow = true }
                                            }.Start();
                                        }
                                        catch { }
                                    }
                                }
                            }
                        }

                    while (sortThreads.Count > 0)
                    {
                        sortThreads[0].Join();
                        if (!sortThreads[0].IsAlive)
                            sortThreads.RemoveAt(0);
                    }

                    lStatus.InvokeIfRequired(() => lStatus.Text = "Status: Ready");
                    bApply.InvokeIfRequired(() => bApply.Enabled = true);
                }
            });
        }
        private void ThreadedFileMove(object o)
        {
            if (o.GetType() != typeof(string[]))
                return;
            string[] fromTo = o as string[];
            if (fromTo.Length != 2)
                return;

            lock (FileMoveLock)
            {
                try
                {
                    if (File.Exists(fromTo[0]) && !File.Exists(fromTo[1]))
                        if (fromTo[0].EndsWith(".jfif"))
                        {
                            var jfif = Image.FromFile(fromTo[0]);
                            var targetPath = fromTo[1].Substring(0, fromTo[1].LastIndexOf('.')) + ".jpeg";
                            jfif.Save(targetPath, ImageFormat.Jpeg);
                            jfif.Dispose();
                            File.Delete(fromTo[0]);
                        }
                        else if (fromTo[0].EndsWith(".webp"))
                            lock (PictureConversionLock)
                            {
                                var stream = new FileStream(fromTo[0], FileMode.Open);
                                try
                                {
                                    var webp = new WebPFormat().Load(stream);
                                    var targetPath = fromTo[1].Substring(0, fromTo[1].LastIndexOf('.')) + ".jpeg";
                                    webp.Save(targetPath, ImageFormat.Jpeg);
                                    webp.Dispose();
                                    stream.Dispose();
                                    File.Delete(fromTo[0]);
                                }
                                catch
                                {
                                    stream.Dispose();
                                    File.Move(fromTo[0], fromTo[1]);
                                }
                            }
                        else if (fromTo[0].EndsWith(".jpg_large"))
                            File.Move(fromTo[0], fromTo[1].Split('_').Reverse().Skip(1).Reverse().Combine("_"));
                        else if (fromTo[0].EndsWith(".crdownload") || fromTo[0].EndsWith(".opdownload"))
                            return;
                        else
                            File.Move(fromTo[0], fromTo[1]);
                }
                catch { }
            }
        }

        // Show/Hide Icon
        private void NotifyIcon_DoubleClick(object sender, EventArgs e)
        {
            this.InvokeIfRequired(() => { ShowForm(); });
        }
        private void MainForm_Resize(object sender, EventArgs e)
        {
            if (WindowState == FormWindowState.Minimized && !phaseShift)
                HideForm();
        }
        private void HideForm()
        {
            phaseShift = true;
            this.ForceHide();
            phaseShift = false;
        }
        private void ShowForm()
        {
            phaseShift = true;
            this.ForceShow();
            WindowState = FormWindowState.Normal;
            phaseShift = false;
        }

        // Other Events
        private void DataGrid_CellMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e != null && e.Button == MouseButtons.Right && e.ColumnIndex >= 0 && e.RowIndex >= 0)
            {
                ContextMenu m = new ContextMenu();
                m.MenuItems.Add(new MenuItem("↑", ((object s, EventArgs ev) =>
                {
                    try
                    {
                        if (currentMouseOverRow > 0)
                        {
                            DatagridIntoConfig();

                            Matching M = Config.Data.Matches[currentMouseOverRow];
                            Config.Data.Matches.RemoveAt(currentMouseOverRow);
                            Config.Data.Matches.Insert(currentMouseOverRow - 1, M);

                            ConfigIntoDatagrid();
                        }
                    }
                    catch { }
                })));
                m.MenuItems.Add(new MenuItem("↓", ((object s, EventArgs ev) =>
                {
                    try
                    {
                        if (currentMouseOverRow < dataGrid.Rows.Count - 1)
                        {
                            DatagridIntoConfig();

                            Matching M = Config.Data.Matches[currentMouseOverRow];
                            Config.Data.Matches.RemoveAt(currentMouseOverRow);
                            Config.Data.Matches.Insert(currentMouseOverRow + 1, M);

                            ConfigIntoDatagrid();
                        }
                    }
                    catch { }
                })));
                m.MenuItems.Add(new MenuItem("🗑️", ((object s, EventArgs ev) =>
                {
                    try
                    {
                        if (currentMouseOverRow < dataGrid.Rows.Count - 1)
                        {
                            DatagridIntoConfig();

                            Config.Data.Matches.RemoveAt(currentMouseOverRow);

                            ConfigIntoDatagrid();
                        }
                    }
                    catch { }
                })));
                currentMouseOverRow = e.RowIndex;
                m.Show(dataGrid, new Point(e.X + dataGrid.GetColumnDisplayRectangle(e.ColumnIndex, true).X, e.Y + dataGrid.GetRowDisplayRectangle(e.RowIndex, true).Y));
            }
        }
        private void DelayPicker_Scroll(object sender, EventArgs e)
        {
            lDelay.Text = $"Delay: {delayPicker.Value} sec.";
            Config.Data.sortDelay = delayPicker.Value * 1000;
        }
        private void DelayPicker_MouseUp(object sender, MouseEventArgs e)
        {
            Config.Save();
        }
        private void MainForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            Config.Save();
            Application.Exit();
        }
    }
}
