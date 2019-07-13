using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DownloadFolderSorter
{
    public class Matching { public string Name, Match, Target; }

    public partial class MainForm : Form
    {
        FileSystemWatcher weightwatchers;
        bool dfolderExists;
        bool phaseShift;
        int currentMouseOverRow;
        
        // StartUp
        public MainForm()
        {
            InitializeComponent();
        }
        private void MainForm_Load(object sender, EventArgs e)
        {
            weightwatchers = new FileSystemWatcher();
            SetDownloadFolder(config.Data.downloadFolder);
            LoadDataGrid();
            Task.Factory.StartNew(() => { this.InvokeIfRequired(() => { HideForm(); }); });
        }

        // SetFolder
        private void SetDownloadFolder(string folder)
        {
            dfolderExists = false;

            tFolder.Text = folder;
            config.Data.downloadFolder = folder;
            config.Save();

            dfolderExists = Directory.Exists(folder);
            dataGrid.Enabled = dfolderExists;
            bApply.Enabled = dfolderExists;
            lStatus.Enabled = dfolderExists;

            if (!dfolderExists)
                lStatus.Text = "Status: Got no valid Download-Folder!";
            else
            {
                lStatus.Text = "Status: Ready";

                weightwatchers.Path = folder;
                weightwatchers.Changed += ((object source, FileSystemEventArgs e) =>
                {
                    try
                    {
                        SortDownloadFolder();
                    }
                    catch (Exception ex) { MessageBox.Show("Sorting Error: \n" + e.ToString()); }
                });
                weightwatchers.EnableRaisingEvents = true;
                weightwatchers.IncludeSubdirectories = false;

                SortDownloadFolder();
            }
        }
        private void BBrowser_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog dialog = new FolderBrowserDialog();
            dialog.SelectedPath = tFolder.Text;
            dialog.Description = "Select your Download-Folder";
            if (dialog.ShowDialog() == DialogResult.OK)
                SetDownloadFolder(dialog.SelectedPath);
        }

        // Converters
        private void DatagridIntoConfig()
        {
            config.Data.Matches.Clear();
            int rows = dataGrid.Rows.Count - 1;
            for (int i = 0; i < rows; i++)
            {
                config.Data.Matches.Add(new Matching()
                {
                    Name = (string)dataGrid.Rows[i].Cells[0].Value,
                    Match = (string)dataGrid.Rows[i].Cells[1].Value,
                    Target = (string)dataGrid.Rows[i].Cells[2].Value
                });
            }
        }
        private void ConfigIntoDatagrid()
        {
            if (config.Data.Matches == null)
                return;
            
            dataGrid.Rows.Clear();
            for (int i = 0; i < config.Data.Matches.Count; i++)
                dataGrid.Rows.Add(new object[] { config.Data.Matches[i].Name, config.Data.Matches[i].Match, config.Data.Matches[i].Target });
        }

        // Actual Sorting
        private void BApply_Click(object sender, EventArgs e)
        {
            DatagridIntoConfig();

            config.Save();

            SortDownloadFolder();
        }
        private void LoadDataGrid()
        {
            ConfigIntoDatagrid();
        }
        private bool CanSort()
        {
            if (!dfolderExists || !Directory.Exists(config.Data.downloadFolder) ||
                config.Data.Matches == null || config.Data.Matches.Count == 0)
                return false;

            for (int i = 0; i < config.Data.Matches.Count; i++)
                if (!Directory.Exists(config.Data.Matches[i].Target))
                    return false;

            return true;
        }
        private void SortDownloadFolder()
        {
            Task.Factory.StartNew(() => {
                if (!bApply.Enabled)
                    return;

                lock (lStatus)
                {
                    if (!CanSort())
                    {
                        lStatus.InvokeIfRequired(() => lStatus.Text = "Status: Error, current configuration is invalid");
                        return;
                    }

                    bApply.InvokeIfRequired(() => bApply.Enabled = false);
                    lStatus.InvokeIfRequired(() => lStatus.Text = "Status: Sorting...");
                    List<Thread> sortThreads = new List<Thread>();
                    string[] files = Directory.GetFiles(config.Data.downloadFolder);
                    for (int i = 0; i < files.Length; i++)
                        for (int j = 0; j < config.Data.Matches.Count; j++)
                        {
                            if (!string.IsNullOrWhiteSpace(config.Data.Matches[j].Match))
                            {
                                string[] splitOR = config.Data.Matches[j].Match.Split('|');
                                for (int k = 0; k < splitOR.Length; k++)
                                {
                                    if (Path.GetFileName(files[i]).ContainsAll(splitOR[k].Split('&')) &&
                                        !File.Exists(config.Data.Matches[j].Target + "\\" + Path.GetFileName(files[i])))
                                    {
                                        Thread t = new Thread(new ParameterizedThreadStart(ThreadedFileMove));
                                        sortThreads.Add(t);
                                        t.Name = "SortThread" + sortThreads.Count;
                                        t.Start(new string[] { files[i], config.Data.Matches[j].Target + "\\" + Path.GetFileName(files[i]) });
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

            Thread.Sleep(3000);

            lock (this)
            {
                if (File.Exists(fromTo[0]) && !File.Exists(fromTo[1]))
                    File.Move(fromTo[0], fromTo[1]);
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

        private void DataGrid_CellMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e != null && e.Button == MouseButtons.Right)
            {
                ContextMenu m = new ContextMenu();
                m.MenuItems.Add(new MenuItem("↑", ((object s, EventArgs ev) =>
                {
                    try
                    {
                        if (currentMouseOverRow > 0)
                        {
                            DatagridIntoConfig();

                            Matching M = config.Data.Matches[currentMouseOverRow];
                            config.Data.Matches.RemoveAt(currentMouseOverRow);
                            config.Data.Matches.Insert(currentMouseOverRow - 1, M);

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

                            Matching M = config.Data.Matches[currentMouseOverRow];
                            config.Data.Matches.RemoveAt(currentMouseOverRow);
                            config.Data.Matches.Insert(currentMouseOverRow + 1, M);

                            ConfigIntoDatagrid();
                        }
                    }
                    catch { }
                })));
                currentMouseOverRow = e.RowIndex;
                m.Show(dataGrid, new Point(e.X + dataGrid.GetColumnDisplayRectangle(e.ColumnIndex, true).X, e.Y + dataGrid.GetRowDisplayRectangle(e.RowIndex, true).Y));
            }
        }
    }
}
