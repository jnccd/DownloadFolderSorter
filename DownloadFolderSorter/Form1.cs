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
    public partial class MainForm : Form
    {
        FileSystemWatcher weightwatchers;
        bool dfolderExists;
        bool isSortable;

        public MainForm()
        {
            InitializeComponent();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            weightwatchers = new FileSystemWatcher();
            setDownloadFolder(config.Default.downloadFolder);
            loadDataGrid();
            ShowWindow(this.Handle, 2);
        }

        private void setDownloadFolder(string folder)
        {
            dfolderExists = false;

            tFolder.Text = folder;
            config.Default.downloadFolder = folder;
            config.Default.Save();

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
                    } catch (Exception ex) { MessageBox.Show("Sorting Error: \n" + e.ToString()); }
                });
                weightwatchers.EnableRaisingEvents = true;

                SortDownloadFolder();
            }
        }
        private void bBrowser_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog dialog = new FolderBrowserDialog();
            dialog.SelectedPath = tFolder.Text;
            dialog.Description = "Select your Download-Folder";
            if (dialog.ShowDialog() == DialogResult.OK)
                setDownloadFolder(dialog.SelectedPath);
        }
        
        private void bApply_Click(object sender, EventArgs e)
        {
            int rows = dataGrid.Rows.Count - 1;
            config.Default.names = new string[rows];
            config.Default.fileMatches = new string[rows];
            config.Default.targetFolders = new string[rows];
            
            for (int i = 0; i < rows; i++)
            {
                config.Default.names[i] = (string)dataGrid.Rows[i].Cells[0].Value;
                config.Default.fileMatches[i] = (string)dataGrid.Rows[i].Cells[1].Value;
                config.Default.targetFolders[i] = (string)dataGrid.Rows[i].Cells[2].Value;
            }

            config.Default.Save();

            if (!CanSort())
                lStatus.InvokeIfRequired(() => { lStatus.Text = "Status: Error, current configuration is invalid, check the input matrix"; });

            SortDownloadFolder();
        }
        private void loadDataGrid()
        {
            if (config.Default.names == null || config.Default.fileMatches == null || config.Default.targetFolders == null)
                return;

            int rows = config.Default.targetFolders.Length;

            for (int i = 0; i < rows; i++)
                dataGrid.Rows.Add(new object[] { config.Default.names[i], config.Default.fileMatches[i], config.Default.targetFolders[i] });
        }
        private bool CanSort()
        {
            if (!dfolderExists || !Directory.Exists(config.Default.downloadFolder) ||
                config.Default.targetFolders == null || config.Default.names == null || config.Default.fileMatches == null ||
                config.Default.targetFolders.Length == 0 || config.Default.names.Length != config.Default.targetFolders.Length || 
                config.Default.fileMatches.Length != config.Default.targetFolders.Length)
                return false;

            for (int i = 0; i < config.Default.targetFolders.Length; i++)
                if (!Directory.Exists(config.Default.targetFolders[i]))
                    return false;

            return true;
        }
        private void SortDownloadFolder()
        {
            if (!CanSort())
                return;

            lStatus.InvokeIfRequired(() => { lStatus.Text = "Status: Sorting..."; });
            string[] files = Directory.GetFiles(config.Default.downloadFolder);
            for (int i = 0; i < files.Length; i++)
                for (int j = 0; j < config.Default.fileMatches.Length; j++)
                {
                    string[] split = config.Default.fileMatches[j].Split('|');
                    for (int k = 0; k < split.Length; k++)
                        if (Path.GetFileName(files[i]).Contains(split[k]) && !File.Exists(config.Default.targetFolders[j] + "\\" + Path.GetFileName(files[i])))
                            File.Move(files[i], config.Default.targetFolders[j] + "\\" + Path.GetFileName(files[i]));
                }
            lStatus.InvokeIfRequired(() => { lStatus.Text = "Status: Ready"; });
        }

        // dll Imports
        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
    }

    public static class Extensions
    {
        public static void InvokeIfRequired(this ISynchronizeInvoke obj, MethodInvoker action)
        {
            if (obj.InvokeRequired)
            {
                var args = new object[0];
                obj.Invoke(action, args);
            }
            else
            {
                action();
            }
        }
    }
}
