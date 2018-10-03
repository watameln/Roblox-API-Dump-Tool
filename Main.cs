﻿using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Windows.Forms;

using Roblox.Reflection;

namespace Roblox
{
    public partial class Main : Form
    {
        private WebClient http = new WebClient();

        public Main()
        {
            InitializeComponent();
            branch.SelectedIndex = 0;
        }

        private string getBranch()
        {
            return branch.SelectedItem.ToString();
        }

        private void setWindowLocked(bool locked)
        {
            if (!locked)
                clearStatus();

            Enabled = !locked;
            UseWaitCursor = locked;
        }

        private async Task setStatus(string msg = "")
        {
            status.Text = "Status: " + msg;
            await Task.Delay(10);
        }

        private void clearStatus()
        {
            status.Text = "Status: Ready!";
        }

        private void writeAndViewFile(string path, string contents)
        {
            if (!File.Exists(path) || File.ReadAllText(path) != contents)
                File.WriteAllText(path, contents);

            Process.Start(path);
        }

        private async Task<string> getApiDumpFilePath(string branch, bool fetchPrevious = false)
        {
            await setStatus("Checking for update...");
            string localAppData = Environment.GetEnvironmentVariable("LocalAppData");

            string coreBin = Path.Combine(localAppData,"RobloxApiDumpFiles");
            Directory.CreateDirectory(coreBin);

            string setupUrl = "https://s3.amazonaws.com/setup." + branch + ".com/";
            string version = await http.DownloadStringTaskAsync(setupUrl + "versionQTStudio");

            if (fetchPrevious)
                version = await ReflectionHistory.GetPreviousVersionGuid(branch, version);

            string file = Path.Combine(coreBin, version + ".json");

            if (!File.Exists(file))
            {
                await setStatus("Grabbing the" + (fetchPrevious ? " previous " : " ") + "API Dump from " + branch);
                string apiDump = await http.DownloadStringTaskAsync(setupUrl + version + "-API-Dump.json");
                File.WriteAllText(file, apiDump);
            }
            else
            {
                await setStatus("Already up to date!");
            }

            return file;
        }

        private void branch_SelectedIndexChanged(object sender, EventArgs e)
        {
            viewApiDumpJson.Enabled = true;
            viewApiDumpClassic.Enabled = true;
            compareVersions.Enabled = true;

            if (getBranch() == "roblox")
                compareVersions.Text = "Compare Previous Version";
            else
                compareVersions.Text = "Compare to Production";

        }

        private async void viewApiDumpJson_Click(object sender, EventArgs e)
        {
            setWindowLocked(true);

            string branch = getBranch();
            string filePath = await getApiDumpFilePath(branch);
            Process.Start(filePath);

            setWindowLocked(false);
        }

        private async void viewApiDumpClassic_Click(object sender, EventArgs e)
        {
            setWindowLocked(true);

            string branch = getBranch();
            string apiFilePath = await getApiDumpFilePath(branch);
            string apiJson = File.ReadAllText(apiFilePath);

            ReflectionDatabase api = ReflectionDatabase.Load(apiJson);
            ReflectionDumper dumper = new ReflectionDumper(api);

            string result = dumper.Run();

            FileInfo info = new FileInfo(apiFilePath);
            string directory = info.DirectoryName;

            string resultPath = Path.Combine(directory, branch + "-api-dump.txt");
            writeAndViewFile(resultPath, result);

            setWindowLocked(false);
        }

        private async void compareVersions_Click(object sender, EventArgs e)
        {
            setWindowLocked(true);

            string newBranch = getBranch();
            bool fetchPrevious = (newBranch == "roblox");

            string newApiFilePath = await getApiDumpFilePath(newBranch);
            string oldApiFilePath = await getApiDumpFilePath("roblox", fetchPrevious);

            await setStatus("Reading " + (fetchPrevious ? "Previous" : "Production") + " API...");
            string oldApiJson = File.ReadAllText(oldApiFilePath);
            ReflectionDatabase oldApi = ReflectionDatabase.Load(oldApiJson);

            await setStatus("Reading " + (fetchPrevious ? "Production" : "New") + " API...");
            string newApiJson = File.ReadAllText(newApiFilePath);
            ReflectionDatabase newApi = ReflectionDatabase.Load(newApiJson);

            await setStatus("Comparing APIs...");

            ReflectionDiffer differ = new ReflectionDiffer();
            string result = differ.CompareDatabases(oldApi, newApi);

            if (result.Length > 0)
            {
                FileInfo info = new FileInfo(newApiFilePath);

                string directory = info.DirectoryName;
                string resultPath = Path.Combine(directory, newBranch + "-diff.txt");

                writeAndViewFile(resultPath, result);
            }
            else
            {
                MessageBox.Show("No differences were found!", "Well, this is awkward...", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            setWindowLocked(false);
        }
    }
}
