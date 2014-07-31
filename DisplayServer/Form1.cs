using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using WSNUtil;
using System.Threading;
using System.IO;

namespace DisplayServer
{
    public partial class frmDisplayServer : Form
    {
        private static string INIFile = Variables.DefaultINILocation;
        private static string CSVFile = Variables.DefaultCSVLocation;
        private int CurrImageID = 0;
        private Mutex DrawingMutex = new Mutex();
        private int MessageCount = 0;
        private StateDrawer Drawer;
        private string FolderPath; // For image and CSV result output
        const string STATE_HISTORY_FILENAME = "StateHistory.csv";

        public frmDisplayServer()
        {
            InitializeComponent();
            try
            {
                FolderPath = Variables.GetDisplayServerResultFolder(INIFile);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Fatal error retrieving configuration information from INIFile: '{0}'", ex.Message);
                Environment.Exit(1);
            }
        }

        public void MessageReceived(List<ObjectEstimate> rawData, List<ObjectEstimate> stateEstimate)
        {
            if (rawData == null || stateEstimate == null)
                return;
            DrawingMutex.WaitOne();

            if (Drawer == null)
                Drawer = new StateDrawer(rawData, stateEstimate, picCurrState.Width, picCurrState.Height, CSVFile);
            else
                Drawer.SetStates(rawData, stateEstimate);
            Bitmap Bmp = Drawer.DrawState();

            Bitmap BmpCopy = (Bitmap)Bmp.Clone(); // Need a copy in order to Save() and display it at the same time, since Bitmap is not a thread safe class
            picCurrState.Image = BmpCopy;
            picCurrState.Invalidate();

            //Bmp.Save(string.Format("testImage-{0}.png", CurrImageID++));
            SaveMessage(Bmp, CurrImageID++, rawData, stateEstimate);

            lblMessageCount.Invoke((MethodInvoker)(() => lblMessageCount.Text = (++MessageCount).ToString()));
            DrawingMutex.ReleaseMutex();
        }

        Mutex CSVMutex = new Mutex(false);
        private void SaveMessage(Bitmap bmp, int CurrImageID, List<ObjectEstimate> rawData, List<ObjectEstimate> stateEstimate)
        {
            bmp.Save(string.Format("{0}/SavedState-{1}.png", FolderPath, CurrImageID++));
            CSVMutex.WaitOne();
            StreamWriter Writer = new StreamWriter(string.Format("{0}/{1}", FolderPath, STATE_HISTORY_FILENAME), true);
            for (int i = 0; i < rawData.Count; i++)
                Writer.WriteLine("r,{0},{1},{2},{3},{4}", CurrImageID, rawData[i].PositionX, rawData[i].PositionY, rawData[i].VelocityX, rawData[i].VelocityY);
            for (int i = 0; i < stateEstimate.Count; i++)
                Writer.WriteLine("s,{0},{1},{2},{3},{4}", CurrImageID, stateEstimate[i].PositionX, stateEstimate[i].PositionY, stateEstimate[i].VelocityX, stateEstimate[i].VelocityY);
            Writer.Close();
            CSVMutex.ReleaseMutex();
        }

        /// <summary>
        /// Gets a string for a folderPath which does not conflict with any existing folders. If a folder already exists a "-x" will
        /// be appended to the folder name where x is the lowest possible integer. This folder will then be created.
        /// </summary>
        /// <returns></returns>
        private string GetFolderPath(string folderPath)
        {
            folderPath = folderPath.TrimEnd(new char[] { '/', '\\' }); //Get rid of trailing directory serpators to expose the folder name
            string OriginalFolderName = FolderPath;
            int CurrAppend = 0;
            while (Directory.Exists(folderPath))
                folderPath = OriginalFolderName + "-" + CurrAppend++;
            Directory.CreateDirectory(folderPath);
            return folderPath + "/";
        }

        private void lnkOpenHistory_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start("explorer.exe", System.IO.Directory.GetCurrentDirectory());
        }

        private void frmDisplayServer_Load(object sender, EventArgs e)
        {
            Thread T = new Thread(new ThreadStart(Init_Form));
            T.Start(); // Execute work on a thread to keep the form responsive
        }

        private void Init_Form()
        {
            DisplayReceiver Receiver = new DisplayReceiver(INIFile, lblDebugInfo);
            Receiver.OnMessageReceived += MessageReceived;
            FolderPath = GetFolderPath(FolderPath); //Update the FolderPath value to not overwrite previous runs
            InitStateHistoryFile();
            Receiver.Start();

            Receiver.ConnectionEstablishedSemaphore.WaitOne();
            lblMessageCount.Invoke((MethodInvoker)(() => lblMessageCount.Text = "0"));
            lblStatus.Invoke((MethodInvoker)(() => lblStatus.Text = "Connected"));
            lblStatus.Invoke((MethodInvoker)(() => lblStatus.ForeColor = Color.Green));
            int PollingDelay = Variables.GetPollingDelay(INIFile);
            lblPollingFrequency.Invoke((MethodInvoker)(() => lblPollingFrequency.Text = string.Format("{0} Hz ({1} ms per measurement)", 1000 / PollingDelay, PollingDelay)));
            lblServerIP.Invoke((MethodInvoker)(() => lblServerIP.Text = string.Format("{0}:{1}", Receiver.ClientIP, Variables.GetDisplayPort(INIFile))));
        }

        private void InitStateHistoryFile()
        {
            StreamWriter Writer = new StreamWriter(string.Format("{0}/{1}", FolderPath, STATE_HISTORY_FILENAME));
            Writer.WriteLine("# This file was initially created on {0}", DateTime.Now.ToString());
            Writer.WriteLine("# The format is as follows:");
            Writer.WriteLine("# r (rawData) or s (stateEstimate), Curr Image ID, PositionX, PositionY, VelocityX, VelocityY");
            Writer.Close();
        }
    }
}
