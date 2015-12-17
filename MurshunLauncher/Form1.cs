﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net;
using System.IO;
using System.Diagnostics;
using System.Threading;

namespace MurshunLauncher
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            
            iniFilePath = iniDirectoryPath + "\\MurshunLauncherClient.ini";
            xmlFilePath = iniDirectoryPath + "\\MurshunLauncher.xml";

            pathToArma3Client_textBox.Text = pathToTheLauncher + "\\arma3.exe";
            pathToArma3ClientMods_textBox.Text = pathToTheLauncher;

            pathToArma3Server_textBox.Text = pathToTheLauncher + "\\arma3server.exe";
            pathToArma3ServerMods_textBox.Text = pathToTheLauncher;

            lastSelectedFolder = pathToTheLauncher;

            xmlPath_textBox.Text = xmlFilePath;
            defaultStartLine_textBox.Text = "-world=empty -nosplash -skipintro -nologs -nofilepatching";

            if (!Directory.Exists(iniDirectoryPath))
            {
                Directory.CreateDirectory(iniDirectoryPath);
            }

            if (File.Exists(iniFilePath))
            {
                ReadIniFile();
                SaveXmlFile();
                File.Delete(iniFilePath);
            }

            if (File.Exists(xmlFilePath))
            {
                ReadXmlFile();
            }
            else
            {
                SaveXmlFile();
            }

            if (File.Exists(iniDirectoryPath + "\\MurshunLauncherPreset.txt"))
            {
                string[] infoFromPresetFile = File.ReadAllLines(iniDirectoryPath + "\\MurshunLauncherPreset.txt");

                string modsStringArray = infoFromPresetFile[0];

                presetModsList = modsStringArray.Split(';').ToList();

                presetModsList = presetModsList.Select(s => s.Replace("-mod=", "")).ToList();
                presetModsList = presetModsList.Select(s => s.Replace(";", "")).ToList();
                presetModsList.RemoveAll(s => String.IsNullOrEmpty(s.Trim()));
                
                RefreshPresetModsList();

                Thread NewThread = new Thread(() => GetWebModLine());
                NewThread.Start();
            }
            else
            {
                MessageBox.Show("MurshunLauncherPreset.txt not found. Trying to get it from Poddy...");

                GetWebModLine();
            }

            label3.Text = "Version " + launcherVersion.ToString();
        }

        string pathToTheLauncher = Directory.GetCurrentDirectory();
        string iniDirectoryPath = System.Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\MurshunLauncher";
        string iniFilePath;
        string xmlFilePath;
        string lastSelectedFolder;

        bool isCustomModsMouseButtonDown = false;
        bool serverTabEnabled = false;

        string[] missingFilesArray = {};
        string[] excessFilesArray = {};

        List<string> presetModsList;

        double launcherVersion = 0.23;

        public void ReadIniFile()
        {
            string[] infoFromIniFile = File.ReadAllLines(iniFilePath);
            
            foreach (string X in infoFromIniFile)
            {
                if (X.Contains("GAMEPATH="))
                {
                    pathToArma3Client_textBox.Text = X.Replace("GAMEPATH=", "");
                }
                if (X.Contains("MODPATH="))
                {
                    pathToArma3ClientMods_textBox.Text = X.Replace("MODPATH=", "");
                }
                if (X.Contains("CUSTOMMOD="))
                {
                    clientCustomMods_listView.Items.Add(X.Replace("CUSTOMMOD=", ""));
                }
            }
        }

        public class MurshunLauncherXmlSettings
        {
            public string pathToArma3Client_textBox;
            public string pathToArma3ClientMods_textBox;
            public bool showScriptErrors_checkBox;
            public bool joinTheServer_checkBox;
            public List<string> clientCustomMods_listView;
            public List<string> clientCheckedModsList_listView;
            public string defaultStartLine_textBox;
            public string advancedStartLine_textBox;
            public bool verifyBeforeLaunch_checkBox;            
            public string lastSelectedFolder;
            public bool serverTabEnabled;

            public string pathToArma3Server_textBox;
            public string pathToArma3ServerMods_textBox;
            public List<string> serverCustomMods_listView;
            public List<string> serverCheckedModsList_listView;
            public string serverConfig_textBox;
            public string serverCfg_textBox;
            public string serverProfiles_textBox;
            public string serverProfileName_textBox;
        }

        public void ReadXmlFile()
        {
            System.Xml.Serialization.XmlSerializer serializer = new System.Xml.Serialization.XmlSerializer(typeof(MurshunLauncherXmlSettings));
            
            StreamReader reader = new StreamReader(xmlFilePath);

            try
            {
                MurshunLauncherXmlSettings LauncherSettings = (MurshunLauncherXmlSettings)serializer.Deserialize(reader);
                reader.Close();

                pathToArma3Client_textBox.Text = LauncherSettings.pathToArma3Client_textBox;
                pathToArma3ClientMods_textBox.Text = LauncherSettings.pathToArma3ClientMods_textBox;
                showScriptErrors_checkBox.Checked = LauncherSettings.showScriptErrors_checkBox;
                joinTheServer_checkBox.Checked = LauncherSettings.joinTheServer_checkBox;

                foreach (string X in LauncherSettings.clientCustomMods_listView)
                {
                    if (!clientCustomMods_listView.Items.Cast<ListViewItem>().Select(x => x.Text).Contains(X))
                    {
                        clientCustomMods_listView.Items.Add(X);
                    }
                }

                for (int i = 0; i < clientCustomMods_listView.Items.Count; i++)
                {
                    if (LauncherSettings.clientCheckedModsList_listView.Contains(clientCustomMods_listView.Items[i].Text))
                    {
                        clientCustomMods_listView.Items[i].Checked = true;
                    }
                }

                defaultStartLine_textBox.Text = LauncherSettings.defaultStartLine_textBox;
                advancedStartLine_textBox.Text = LauncherSettings.advancedStartLine_textBox;
                verifyBeforeLaunch_checkBox.Checked = LauncherSettings.verifyBeforeLaunch_checkBox;
                lastSelectedFolder = LauncherSettings.lastSelectedFolder;
                serverTabEnabled = LauncherSettings.serverTabEnabled;

                if (serverTabEnabled)
                {
                    button1.Visible = true;
                }

                pathToArma3Server_textBox.Text = LauncherSettings.pathToArma3Server_textBox;
                pathToArma3ServerMods_textBox.Text = LauncherSettings.pathToArma3ServerMods_textBox;

                foreach (string X in LauncherSettings.serverCustomMods_listView)
                {
                    if (!serverCustomMods_listView.Items.Cast<ListViewItem>().Select(x => x.Text).Contains(X))
                    {
                        serverCustomMods_listView.Items.Add(X);
                    }
                }

                for (int i = 0; i < serverCustomMods_listView.Items.Count; i++)
                {
                    if (LauncherSettings.serverCheckedModsList_listView.Contains(serverCustomMods_listView.Items[i].Text))
                    {
                        serverCustomMods_listView.Items[i].Checked = true;
                    }
                }

                serverConfig_textBox.Text = LauncherSettings.serverConfig_textBox;
                serverCfg_textBox.Text = LauncherSettings.serverCfg_textBox;
                serverProfiles_textBox.Text = LauncherSettings.serverProfiles_textBox;
                serverProfileName_textBox.Text = LauncherSettings.serverProfileName_textBox;
            }
            catch
            {
                reader.Close();

                DialogResult dialogResult = MessageBox.Show("Create a new one?", "Xml file is corrupted", MessageBoxButtons.YesNo);

                if (dialogResult == DialogResult.Yes)
                {
                    SaveXmlFile();
                }
                if (dialogResult == DialogResult.No)
                {
                    System.Environment.Exit(1);
                }
            }
        }

        public void SaveXmlFile()
        {
            MurshunLauncherXmlSettings LauncherSettings = new MurshunLauncherXmlSettings();

            LauncherSettings.pathToArma3Client_textBox = pathToArma3Client_textBox.Text;
            LauncherSettings.pathToArma3ClientMods_textBox = pathToArma3ClientMods_textBox.Text;
            LauncherSettings.showScriptErrors_checkBox = showScriptErrors_checkBox.Checked;
            LauncherSettings.joinTheServer_checkBox = joinTheServer_checkBox.Checked;
            LauncherSettings.clientCustomMods_listView = clientCustomMods_listView.Items.Cast<ListViewItem>().Select(x => x.Text).ToList();
            LauncherSettings.clientCheckedModsList_listView = clientCustomMods_listView.CheckedItems.Cast<ListViewItem>().Select(x => x.Text).ToList();
            LauncherSettings.defaultStartLine_textBox = defaultStartLine_textBox.Text;
            LauncherSettings.advancedStartLine_textBox = advancedStartLine_textBox.Text;
            LauncherSettings.verifyBeforeLaunch_checkBox = verifyBeforeLaunch_checkBox.Checked;
            LauncherSettings.lastSelectedFolder = lastSelectedFolder;
            LauncherSettings.serverTabEnabled = serverTabEnabled;

            LauncherSettings.pathToArma3Server_textBox = pathToArma3Server_textBox.Text;
            LauncherSettings.pathToArma3ServerMods_textBox = pathToArma3ServerMods_textBox.Text;
            LauncherSettings.serverCustomMods_listView = serverCustomMods_listView.Items.Cast<ListViewItem>().Select(x => x.Text).ToList();
            LauncherSettings.serverCheckedModsList_listView = serverCustomMods_listView.CheckedItems.Cast<ListViewItem>().Select(x => x.Text).ToList();
            LauncherSettings.serverConfig_textBox = serverConfig_textBox.Text;
            LauncherSettings.serverCfg_textBox = serverCfg_textBox.Text;
            LauncherSettings.serverProfiles_textBox = serverProfiles_textBox.Text;
            LauncherSettings.serverProfileName_textBox = serverProfileName_textBox.Text;

            System.Xml.Serialization.XmlSerializer serializer = new System.Xml.Serialization.XmlSerializer(typeof(MurshunLauncherXmlSettings));

            System.IO.FileStream writer = System.IO.File.Create(xmlFilePath);
            serializer.Serialize(writer, LauncherSettings);
            writer.Close();
        }

        public void RefreshInterface()
        {
            foreach (ListViewItem X in clientPresetMods_listView.Items)
            {
                if (Directory.Exists(pathToArma3ClientMods_textBox.Text + "\\" + X.Text + "\\addons") || Directory.Exists(pathToArma3ClientMods_textBox.Text + "\\" + X.Text + "\\Addons"))
                {
                    if (X.BackColor != Color.Green)
                    X.BackColor = Color.Green;
                }
                else
                {
                    if (X.BackColor != Color.Red)
                    X.BackColor = Color.Red;
                }
            }

            foreach (ListViewItem X in clientCustomMods_listView.Items)
            {
                if (Directory.Exists(X.Text + "\\addons") || Directory.Exists(X.Text + "\\Addons"))
                {
                    if (X.BackColor != Color.Green)
                    X.BackColor = Color.Green;
                }
                else
                {
                    if (X.BackColor != Color.Red)
                    X.BackColor = Color.Red;
                }
            }

            columnHeader7.Width = -2;

            foreach (ListViewItem X in serverPresetMods_listView.Items)
            {
                if (Directory.Exists(pathToArma3ServerMods_textBox.Text + "\\" + X.Text + "\\addons") || Directory.Exists(pathToArma3ServerMods_textBox.Text + "\\" + X.Text + "\\Addons"))
                {
                    if (X.BackColor != Color.Green)
                    X.BackColor = Color.Green;
                }
                else
                {
                    if (X.BackColor != Color.Red)
                    X.BackColor = Color.Red;
                }
            }

            foreach (ListViewItem X in serverCustomMods_listView.Items)
            {
                if (Directory.Exists(X.Text + "\\addons") || Directory.Exists(X.Text + "\\Addons"))
                {
                    if (X.BackColor != Color.Green)
                    X.BackColor = Color.Green;
                }
                else
                {
                    if (X.BackColor != Color.Red)
                    X.BackColor = Color.Red;
                }
            }

            columnHeader9.Width = -2;
        }

        public void VerifyMods()
        {
            if (File.Exists(pathToArma3ClientMods_textBox.Text + "\\MurshunLauncherFiles.txt"))
            {
                textBox2.Text = pathToArma3ClientMods_textBox.Text + "\\MurshunLauncherFiles.txt";

                string[] btsync_fileLinesArray = File.ReadAllLines(pathToArma3ClientMods_textBox.Text + "\\MurshunLauncherFiles.txt");

                List<string> btsync_fileLinesList = new List<string>();

                btsync_fileLinesList = btsync_fileLinesArray.ToList();

                btsync_fileLinesList.Add("\\Arma 3\\Userconfig\\task_force_radio\\radio_settings.hpp");

                btsync_fileLinesArray = btsync_fileLinesList.ToArray();

                List<string> btsync_filesList = new List<string>();
                List<string> btsync_foldersList = new List<string>();

                foreach (string X in btsync_fileLinesArray)
                {
                    int count = X.Count(f => f == '\\');

                    if (count > 1)
                    {
                        btsync_filesList.Add(X);
                    }
                    else
                    {
                        btsync_foldersList.Add(X + "\\");
                    }
                }

                listView4.Items.Clear();

                foreach (string X in btsync_filesList)
                {
                    listView4.Items.Add(X);
                }

                textBox1.Text = pathToArma3ClientMods_textBox.Text;

                string[] folder_foldersArray = Directory.GetDirectories(pathToArma3ClientMods_textBox.Text, "*", SearchOption.TopDirectoryOnly).Where(s => s.Contains("@")).ToArray();
                string[] folder_filesArray = Directory.GetFiles(pathToArma3ClientMods_textBox.Text, "*", SearchOption.AllDirectories).Where(s => s.Contains("@")).ToArray();

                folder_foldersArray = folder_foldersArray.Select(s => s.Replace(pathToArma3ClientMods_textBox.Text, "")).ToArray();
                folder_filesArray = folder_filesArray.Select(s => s.Replace(pathToArma3ClientMods_textBox.Text, "")).ToArray();

                List<string> folder_filesList = new List<string>();

                foreach (string X in folder_foldersArray)
                {
                    if (btsync_foldersList.Any(X.Contains) && X.Substring(0, 2) == "\\@")
                    {
                        folder_filesList.Add(X);
                    }
                }

                foreach (string X in folder_filesArray)
                {
                    if (btsync_foldersList.Any(X.Contains) && X.Substring(0, 2) == "\\@")
                    {
                        FileInfo F = new FileInfo(pathToArma3ClientMods_textBox.Text + X);
                        folder_filesList.Add(X + ":" + F.Length);
                    }
                }

                listView1.Items.Clear();

                if (File.Exists(pathToArma3Client_textBox.Text.Replace("\\arma3.exe", "") + "\\Userconfig\\task_force_radio\\radio_settings.hpp"))
                {
                    folder_filesList.Add("\\Arma 3\\Userconfig\\task_force_radio\\radio_settings.hpp");
                }

                foreach (string X in folder_filesList)
                {
                    listView1.Items.Add(X);
                }

                folder_filesArray = folder_filesList.ToArray();

                excessFilesArray = folder_filesArray.Where(x => !btsync_fileLinesArray.Contains(x)).ToArray();

                missingFilesArray = btsync_fileLinesArray.Where(x => !folder_filesArray.Contains(x)).ToArray();
                missingFilesArray = missingFilesArray.Where(x => !folder_foldersArray.Contains(x)).ToArray();

                listView2.Items.Clear();

                listView3.Items.Clear();

                if (missingFilesArray.Length == 0)
                {
                    listView2.Items.Add("No missing files.");
                }
                else
                {
                    foreach (string X in missingFilesArray)
                    {
                        listView2.Items.Add(X);
                    }
                }

                if (excessFilesArray.Length == 0)
                {
                    listView3.Items.Add("No excess files.");
                }
                else
                {
                    foreach (string X in excessFilesArray)
                    {
                        listView3.Items.Add(X);
                    }
                }
            }
            else
            {
                MessageBox.Show("MurshunLauncherFiles.txt not found.");
            }
        }

        public void GetWebModLine()
        {
            WebClient client = new WebClient();

            try
            {
                string webModLine = client.DownloadString("http://dedick.podkolpakom.net/arma/mods.php");

                File.WriteAllText(iniDirectoryPath + "\\MurshunLauncherPreset.txt", webModLine);
                
                presetModsList = webModLine.Split(';').ToList();

                presetModsList = presetModsList.Select(s => s.Replace("-mod=", "")).ToList();
                presetModsList = presetModsList.Select(s => s.Replace(";", "")).ToList();
                presetModsList.RemoveAll(s => String.IsNullOrEmpty(s.Trim()));
                
                if (InvokeRequired)
                {
                    this.Invoke(new Action(() => RefreshPresetModsList()));
                    this.Invoke(new Action(() => RefreshInterface()));
                }
                else
                {
                    RefreshPresetModsList();
                    RefreshInterface();
                }
            }
            catch (Exception)
            {
                if (InvokeRequired)
                {
                    this.Invoke(new Action(() => RefreshInterface()));
                }
                else
                {
                    RefreshInterface();
                }

                if (!InvokeRequired)
                {
                    MessageBox.Show("Couldn't retrieve MurshunLauncherPreset.txt from Poddy. Restart the launcher to try again.");
                }
            }
        }

        public void RefreshPresetModsList()
        {
            clientPresetMods_listView.Items.Clear();

            serverPresetMods_listView.Items.Clear();

            foreach (string X in presetModsList)
            {
                clientPresetMods_listView.Items.Add(X);
            }

            foreach (string X in presetModsList)
            {
                serverPresetMods_listView.Items.Add(X.Replace("@allinarmaterrainpack", "@allinarmaterrainpacklite"));
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog chosenFolder = new FolderBrowserDialog();
            chosenFolder.Description = "Select client mods folder.";
            chosenFolder.SelectedPath = pathToArma3ClientMods_textBox.Text;

            if (chosenFolder.ShowDialog() == DialogResult.OK)
            {
                textBox1.Text = chosenFolder.SelectedPath;

                string[] folder_foldersArray = Directory.GetDirectories(chosenFolder.SelectedPath, "*", SearchOption.TopDirectoryOnly).Where(s => s.Contains("@")).ToArray();
                string[] folder_filesArray = Directory.GetFiles(chosenFolder.SelectedPath, "*", SearchOption.AllDirectories).Where(s => s.Contains("@")).ToArray();

                folder_foldersArray = folder_foldersArray.Select(s => s.Replace(chosenFolder.SelectedPath, "")).ToArray();
                folder_filesArray = folder_filesArray.Select(s => s.Replace(chosenFolder.SelectedPath, "")).ToArray();

                listView1.Items.Clear();

                foreach (string X in folder_foldersArray)
                {
                    if (X.Substring(0, 2) == "\\@")
                    {
                        listView1.Items.Add(X);
                    }
                }

                foreach (string X in folder_filesArray)
                {
                    if (X.Substring(0, 2) == "\\@")
                    {
                        FileInfo F = new FileInfo(chosenFolder.SelectedPath + X);
                        listView1.Items.Add(X + ":" + F.Length);
                    }
                }

                SaveFileDialog saveFile = new SaveFileDialog();

                saveFile.Title = "Save File Dialog";
                saveFile.Filter = "Text File (.txt) | *.txt";
                saveFile.FileName = "MurshunLauncherFiles.txt";
                saveFile.InitialDirectory = pathToArma3ClientMods_textBox.Text;
                saveFile.RestoreDirectory = true;

                if (saveFile.ShowDialog() == DialogResult.OK)
                {
                    File.WriteAllLines(saveFile.FileName.ToString(), listView1.Items.Cast<ListViewItem>().Select(X => X.Text));
                }
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            VerifyMods();
        }

        private void launch_button_Click(object sender, EventArgs e)
        {
                SaveXmlFile();

                if (verifyBeforeLaunch_checkBox.Checked)
                {
                    VerifyMods();

                    if (missingFilesArray.Length == 0 && excessFilesArray.Length == 0)
                    {
                        MessageBox.Show("No missing or excess files.");
                    }
                    else
                    {
                        tabControl1.SelectedTab = tabPage2;
                        return;
                    }
                }

                string modLine;

                modLine = defaultStartLine_textBox.Text;

                if (advancedStartLine_textBox.Text != "")
                {
                    modLine = modLine + " " + advancedStartLine_textBox.Text;
                }

                if (showScriptErrors_checkBox.Checked)
                {
                    modLine = modLine + " -showscripterrors";
                }

                modLine = modLine + " \"-mod=";

                foreach (ListViewItem X in clientPresetMods_listView.Items)
                {
                    modLine = modLine + pathToArma3ClientMods_textBox.Text + "\\" + X.Text + ";";
                    if (X.BackColor == Color.Red)
                    {
                        MessageBox.Show(X.Text + " not found.");
                        return;
                    }
                }

                foreach (ListViewItem X in clientCustomMods_listView.CheckedItems)
                {
                    modLine = modLine + X.Text + ";";
                    if (X.BackColor == Color.Red)
                    {
                        MessageBox.Show(X.Text + " not found.");
                        return;
                    }
                }

                modLine = modLine + "\"";

                if (joinTheServer_checkBox.Checked)
                {
                    modLine = modLine + " -connect=109.87.76.153 -port=2302 -password=v";
                }

                if (File.Exists(pathToArma3Client_textBox.Text))
                {
                    Process myProcess = new Process();

                    myProcess.StartInfo.FileName = pathToArma3Client_textBox.Text;
                    myProcess.StartInfo.Arguments = modLine;
                    myProcess.Start();

                    Thread.Sleep(1000);

                    System.Windows.Forms.Application.Exit();
                }
                else
                {
                    MessageBox.Show("arma3.exe not found.");
                }
        }

        private void button6_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog chosenFolder = new FolderBrowserDialog();
            chosenFolder.Description = "Select custom mod folder.";
            chosenFolder.SelectedPath = lastSelectedFolder;

            if (chosenFolder.ShowDialog() == DialogResult.OK)
            {
                lastSelectedFolder = chosenFolder.SelectedPath;

                clientCustomMods_listView.Items.Add(chosenFolder.SelectedPath);
                
                SaveXmlFile();
                RefreshInterface();
            }
        }

        private void checkBox1_Click(object sender, EventArgs e)
        {
            SaveXmlFile();
        }

        private void checkBox2_Click(object sender, EventArgs e)
        {
            SaveXmlFile();
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start("http://murshun.club/");
        }

        private void linkLabel2_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start("http://steamcommunity.com/groups/murshun");
        }

        private void checkBox3_Click(object sender, EventArgs e)
        {
            SaveXmlFile();
        }

        private void linkLabel3_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            defaultStartLine_textBox.Text = "-world=empty -nosplash -skipintro -nologs -nofilepatching";

            SaveXmlFile();
        }

        private void button8_Click(object sender, EventArgs e)
        {
            button9_Click(sender, e);

            if (listView12.Items.Count != 0 || listView10.Items.Count != 0)
            {
                tabControl1.SelectedTab = tabPage4;
                return;
            }

            try
            {
                List<string> clientMissionlist = Directory.GetFiles(pathToArma3Client_textBox.Text.Replace("arma3.exe", "MPMissions"), "*", SearchOption.AllDirectories).Where(s => s.Contains(".pbo")).ToList();

                MessageBox.Show("Copying " + clientMissionlist.Count + " missions.");

                foreach (string X in clientMissionlist)
                {
                    File.Copy(X, X.Replace(pathToArma3Client_textBox.Text.Replace("arma3.exe", "MPMissions"), pathToArma3Server_textBox.Text.Replace("arma3server.exe", "mpmissions")), true);
                }

                MessageBox.Show("Done.");
            }
            catch
            {
                MessageBox.Show("Can't find the mission folder.");
                return;
            }

            string modLine;

            modLine = "-port=2302";

            modLine = modLine + " \"-config=" + serverConfig_textBox.Text + "\"";

            modLine = modLine + " \"-cfg=" + serverCfg_textBox.Text + "\"";

            modLine = modLine + " \"-profiles=" + serverProfiles_textBox.Text + "\"";

            modLine = modLine + " -name=" + serverProfileName_textBox.Text;

            modLine = modLine + " \"-mod=";

            foreach (ListViewItem X in serverPresetMods_listView.Items)
            {
                modLine = modLine + pathToArma3ServerMods_textBox.Text + "\\" + X.Text + ";";
                if (X.BackColor == Color.Red)
                {
                    MessageBox.Show(X.Text + " not found.");
                    return;
                }
            }

            foreach (ListViewItem X in serverCustomMods_listView.CheckedItems)
            {
                modLine = modLine + X.Text + ";";
                if (X.BackColor == Color.Red)
                {
                    MessageBox.Show(X.Text + " not found.");
                    return;
                }
            }

            modLine = modLine + "\"";

            modLine = modLine + " -nologs -nofilepatching";

            if (File.Exists(pathToArma3Server_textBox.Text))
            {
                Process myProcess = new Process();

                myProcess.StartInfo.FileName = pathToArma3Server_textBox.Text;
                myProcess.StartInfo.Arguments = modLine;
                myProcess.Start();
                myProcess.ProcessorAffinity = (System.IntPtr)12;
                myProcess.PriorityClass = ProcessPriorityClass.BelowNormal;
            }
            else
            {
                MessageBox.Show("arma3server.exe not found.");
            }
        }

        private void button9_Click(object sender, EventArgs e)
        {
            textBox12.Text = pathToArma3ClientMods_textBox.Text;
            textBox11.Text = pathToArma3ServerMods_textBox.Text;

            List<string> folder_clientFilesList = Directory.GetFiles(pathToArma3ClientMods_textBox.Text, "*", SearchOption.AllDirectories).Where(s => s.Contains("@")).ToList();
            List<string> folder_serverFilesList = Directory.GetFiles(pathToArma3ServerMods_textBox.Text, "*", SearchOption.AllDirectories).Where(s => s.Contains("@")).ToList();

            listView13.Items.Clear();
            listView11.Items.Clear();

            foreach (string X in folder_clientFilesList)
            {
                FileInfo F = new FileInfo(X);
                listView13.Items.Add(X.Replace(pathToArma3ClientMods_textBox.Text, "") + ":" + F.Length);
            }

            foreach (string X in folder_serverFilesList)
            {
                FileInfo F = new FileInfo(X);
                listView11.Items.Add(X.Replace(pathToArma3ServerMods_textBox.Text, "") + ":" + F.Length);
            }

            folder_clientFilesList = listView13.Items.Cast<ListViewItem>().Select(x => x.Text).ToList();
            folder_serverFilesList = listView11.Items.Cast<ListViewItem>().Select(x => x.Text).ToList();

            List<string> missingFilesList = folder_clientFilesList.Where(x => !folder_serverFilesList.Contains(x)).ToList();
            List<string> excessFilesList = folder_serverFilesList.Where(x => !folder_clientFilesList.Contains(x)).ToList();

            listView12.Items.Clear();
            listView10.Items.Clear();

            foreach (string X in missingFilesList)
            {
                if (!X.Contains("@allinarmaterrainpack"))
                {
                    listView12.Items.Add(X);
                }
            }

            foreach (string X in excessFilesList)
            {
                if (!X.Contains("@allinarmaterrainpack"))
                {
                    listView10.Items.Add(X);
                }
            }
        }

        private void button10_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Removing " + listView10.Items.Count + " files.");

            foreach (ListViewItem item in listView10.Items)
            {
                File.Delete(pathToArma3ServerMods_textBox.Text + item.Text.Split(':')[0]);
            }

            MessageBox.Show("Copying " + listView12.Items.Count + " files.");

            foreach (ListViewItem item in listView12.Items)
            {
                File.Copy(pathToArma3ClientMods_textBox.Text + item.Text.Split(':')[0], pathToArma3ServerMods_textBox.Text + item.Text.Split(':')[0], true);
            }

            MessageBox.Show("Done.");
        }

        private void clientCustomMods_listView_MouseDown(object sender, MouseEventArgs e)
        {
            isCustomModsMouseButtonDown = true;
        }

        private void clientCustomMods_listView_ItemChecked(object sender, ItemCheckedEventArgs e)
        {
            if (isCustomModsMouseButtonDown)
                SaveXmlFile();

            isCustomModsMouseButtonDown = false;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            OpenFileDialog selectFile = new OpenFileDialog();

            selectFile.Title = "Select arma3.exe";
            selectFile.Filter = "Executable File (.exe) | *.exe";
            selectFile.RestoreDirectory = true;

            if (selectFile.ShowDialog() == DialogResult.OK)
            {
                pathToArma3Client_textBox.Text = selectFile.FileName;
                
                SaveXmlFile();
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog chosenFolder = new FolderBrowserDialog();
            chosenFolder.Description = "Select client mods folder.";
            chosenFolder.SelectedPath = lastSelectedFolder;

            if (chosenFolder.ShowDialog() == DialogResult.OK)
            {
                lastSelectedFolder = chosenFolder.SelectedPath;

                pathToArma3ClientMods_textBox.Text = chosenFolder.SelectedPath;
                
                SaveXmlFile();
                RefreshInterface();
            }
        }

        private void defaultStartLine_textBox_Leave(object sender, EventArgs e)
        {
            SaveXmlFile();
        }

        private void advancedStartLine_textBox_Leave(object sender, EventArgs e)
        {
            SaveXmlFile();
        }

        private void changePathToArma3Server_button_Click(object sender, EventArgs e)
        {
            OpenFileDialog selectFile = new OpenFileDialog();

            selectFile.Title = "Select arma3server.exe";
            selectFile.Filter = "Executable File (.exe) | *.exe";
            selectFile.RestoreDirectory = true;

            if (selectFile.ShowDialog() == DialogResult.OK)
            {
                pathToArma3Server_textBox.Text = selectFile.FileName;

                SaveXmlFile();
            }
        }

        private void changePathToArma3ServerMods_button_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog chosenFolder = new FolderBrowserDialog();
            chosenFolder.Description = "Select server mods folder.";
            chosenFolder.SelectedPath = lastSelectedFolder;

            if (chosenFolder.ShowDialog() == DialogResult.OK)
            {
                lastSelectedFolder = chosenFolder.SelectedPath;

                pathToArma3ServerMods_textBox.Text = chosenFolder.SelectedPath;

                SaveXmlFile();
                RefreshInterface();
            }
        }

        private void changeServerConfig_button_Click(object sender, EventArgs e)
        {
            OpenFileDialog selectFile = new OpenFileDialog();

            selectFile.Title = "Select Config";
            selectFile.Filter = "Config File (.cfg) | *.cfg";
            selectFile.RestoreDirectory = true;

            if (selectFile.ShowDialog() == DialogResult.OK)
            {
                serverConfig_textBox.Text = selectFile.FileName;

                SaveXmlFile();
            }
        }

        private void changeServerCfg_button_Click(object sender, EventArgs e)
        {
            OpenFileDialog selectFile = new OpenFileDialog();

            selectFile.Title = "Select Cfg";
            selectFile.Filter = "Cfg File (.cfg) | *.cfg";
            selectFile.RestoreDirectory = true;

            if (selectFile.ShowDialog() == DialogResult.OK)
            {
                serverCfg_textBox.Text = selectFile.FileName;

                SaveXmlFile();
            }
        }

        private void changeServerProfiles_button_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog chosenFolder = new FolderBrowserDialog();
            chosenFolder.Description = "Select profiles folder.";
            chosenFolder.SelectedPath = lastSelectedFolder;

            if (chosenFolder.ShowDialog() == DialogResult.OK)
            {
                lastSelectedFolder = chosenFolder.SelectedPath;

                serverProfiles_textBox.Text = chosenFolder.SelectedPath;

                SaveXmlFile();
            }
        }

        private void serverProfileName_textBox_Leave(object sender, EventArgs e)
        {
            SaveXmlFile();
        }

        private void addCustomServerMod_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog chosenFolder = new FolderBrowserDialog();
            chosenFolder.Description = "Select custom mod folder.";
            chosenFolder.SelectedPath = lastSelectedFolder;

            if (chosenFolder.ShowDialog() == DialogResult.OK)
            {
                lastSelectedFolder = chosenFolder.SelectedPath;

                serverCustomMods_listView.Items.Add(chosenFolder.SelectedPath);

                SaveXmlFile();
                RefreshInterface();
            }
        }

        private void button2_Click_1(object sender, EventArgs e)
        {
            MessageBox.Show("Removing " + excessFilesArray.Length + " files.");

            foreach (string X in excessFilesArray)
            {
                File.Delete(pathToArma3ClientMods_textBox.Text + X.Split(':')[0]);
            }

            MessageBox.Show("Done.");
        }

        private void tabControl1_Selecting(object sender, TabControlCancelEventArgs e)
        {
            if (e.TabPage == tabPage3 || e.TabPage == tabPage4)
            {
                if (!serverTabEnabled)
                {
                    e.Cancel = true;
                }
            }
        }

        private void removeUncheckedMod_button_Click(object sender, EventArgs e)
        {
            ListView clientCustomMods_listViewTemp;

            clientCustomMods_listViewTemp = clientCustomMods_listView;

            foreach (ListViewItem item in clientCustomMods_listViewTemp.Items)
            {
                if (!item.Checked)
                    item.Remove();
            }

            SaveXmlFile();
        }

        private void removeUncheckedServerMod_button_Click(object sender, EventArgs e)
        {
            ListView clientCustomMods_listViewTemp;

            clientCustomMods_listViewTemp = serverCustomMods_listView;

            foreach (ListViewItem item in serverCustomMods_listView.Items)
            {
                if (!item.Checked)
                    item.Remove();
            }

            SaveXmlFile();
        }

        private void serverCustomMods_listView_MouseDown(object sender, MouseEventArgs e)
        {
            isCustomModsMouseButtonDown = true;
        }

        private void serverCustomMods_listView_ItemChecked(object sender, ItemCheckedEventArgs e)
        {
            if (isCustomModsMouseButtonDown)
                SaveXmlFile();

            isCustomModsMouseButtonDown = false;
        }
    }
}