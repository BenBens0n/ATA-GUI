﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

namespace ATA_GUI
{
    public partial class LoadingForm : Form
    {
        private readonly List<string> array;
        private readonly string command;
        private readonly string deviceSerial;
        private readonly OperationType operation;

        public LoadingForm(List<string> arrayApkTemp, string commandTemp, string label)
        {
            InitializeComponent();
            array = arrayApkTemp;
            command = commandTemp;
            labelText.Text = label;
            operation = OperationType.Install;
        }

        public LoadingForm(List<string> arrayFile, string deviceSerialTmp)
        {
            InitializeComponent();
            array = arrayFile;
            labelText.Text = "Transfering file:";
            deviceSerial = deviceSerialTmp;
            operation = OperationType.Transfer;
        }

        public LoadingForm(string deviceSerialTmp, List<string> arrayApp)
        {
            InitializeComponent();
            array = arrayApp;
            labelText.Text = "Extracting file:";
            deviceSerial = deviceSerialTmp;
            operation = OperationType.Extraction;
        }

        private void LoadingForm_Shown(object sender, EventArgs e)
        {
            if (!backgroundWorker.IsBusy)
            {
                backgroundWorker.RunWorkerAsync();
            }
            else
            {
                MainForm.MessageShowBox("Background worker is busy", 0);
            }
        }


        private void backgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            progressBar1.Value = progressBar1.Maximum;
            progressBar1.Update();
            progressBar1.Refresh();
            DialogResult = DialogResult.OK;
            Close();
        }

        private void backgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            _ = Invoke((Action)delegate
            {
                progressBar1.Minimum = 1;
                progressBar1.Step = 1;
                progressBar1.Maximum = array.Count;
                switch (operation)
                {
                    case OperationType.Install:
                        array.ForEach(x =>
                        {
                            labelFileName.Text = x;
                            Refresh();
                            _ = MainForm.systemCommand(command + x);
                            ReportProgress();
                        });
                        break;
                    case OperationType.Transfer:
                        _ = MainForm.adbFastbootCommandR(new[] { "-s " + deviceSerial + " shell mkdir sdcard/ATA" }, 0);
                        array.ForEach(file =>
                        {
                            if (File.Exists(file))
                            {
                                labelFileName.Text = file.Substring(file.LastIndexOf('\\') + 1);
                                Refresh();
                                if (MainForm.adbFastbootCommandR(new[] { "-s " + deviceSerial + " push " + file + " sdcard/ATA " }, 0) == null)
                                {
                                    MainForm.MessageShowBox(labelFileName.Text + " not transfered", 0);
                                }
                                ReportProgress();
                            }
                            else
                            {
                                ReportProgress();
                            }
                        });
                        break;
                    case OperationType.Extraction:
                        if (!Directory.Exists("APKS"))
                        {
                            _ = MainForm.systemCommand("mkdir APKS");
                        }
                        array.ForEach(x =>
                        {
                            labelFileName.Text = x;
                            Refresh();
                            string[] pathList = MainForm.systemCommand("adb.exe " + "-s " + deviceSerial + " shell pm path " + x).Split('\n').Where(it => it.Contains("package")).ToArray();
                            foreach (string path in pathList)
                            {
                                _ = MainForm.systemCommand("adb.exe " + "-s " + deviceSerial + " pull " + path.Replace("package:", "") + " " + Application.StartupPath + "\\APKS\\" + x + "_base.apk");
                            }
                            ReportProgress();
                        });
                        break;
                    default:
                        break;
                }
            });
        }

        private void ReportProgress()
        {
            _ = Invoke((MethodInvoker)delegate
            {
                progressBar1.PerformStep();
                progressBar1.Refresh();
                Application.DoEvents();
            });
            Thread.Sleep(500);
        }
    }

    public enum OperationType
    {
        Transfer,
        Extraction,
        Install
    }
}
