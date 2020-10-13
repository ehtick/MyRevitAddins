﻿using System;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.WindowsAPICodePack.Dialogs;
using mySettings = MEPUtils.DrawingListManager.Properties.Settings;

namespace MEPUtils.DrawingListManager
{
    public partial class DrawingListManagerForm : Form
    {
        private string pathToDwgFolder = string.Empty;
        private string pathToDwgList = string.Empty;
        private DrwgLstMan dlm = new DrwgLstMan();

        public DrawingListManagerForm()
        {
            InitializeComponent();
            pathToDwgFolder = mySettings.Default.PathToDwgFolder;
            pathToDwgList = mySettings.Default.PathToDwgList;
        }

        /// <summary>
        /// Select directory with drawings.
        /// </summary>
        private void button1_Click(object sender, EventArgs e)
        {
            CommonOpenFileDialog dialog = new CommonOpenFileDialog(); //https://stackoverflow.com/a/41511598/6073998
            if (string.IsNullOrEmpty(pathToDwgFolder)) dialog.InitialDirectory = Environment.ExpandEnvironmentVariables("%userprofile%");
            else dialog.InitialDirectory = pathToDwgFolder;
            dialog.IsFolderPicker = true;
            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                pathToDwgFolder = dialog.FileName + "\\";
                mySettings.Default.PathToDwgFolder = pathToDwgFolder;
                textBox2.Text = pathToDwgFolder;
            }

        }

        /// <summary>
        /// Select drawing list Excel file.
        /// </summary>
        private void button2_Click(object sender, EventArgs e)
        {
            CommonOpenFileDialog dialog = new CommonOpenFileDialog(); //https://stackoverflow.com/a/41511598/6073998
            if (string.IsNullOrEmpty(pathToDwgList)) dialog.InitialDirectory = Environment.ExpandEnvironmentVariables("%userprofile%");
            else dialog.InitialDirectory = Path.GetDirectoryName(pathToDwgList);
            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                pathToDwgList = dialog.FileName;
                mySettings.Default.PathToDwgList = pathToDwgList;
                textBox1.Text = pathToDwgList;
            }
        }

        private void DrawingListManagerForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            mySettings.Default.Save();
        }

        /// <summary>
        /// Enumerate pdf files in the selected folder
        /// </summary>
        private void button3_Click(object sender, EventArgs e)
        {
            dlm.EnumeratePdfFiles(pathToDwgFolder);
            textBox3.Text = dlm.drwgFileNameList.Count.ToString();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            dlm.ScanRescanFilesAndList(pathToDwgFolder);
            dataGridView1.DataSource = dlm.Data;
        }
    }
}
