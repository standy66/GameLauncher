using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;
using System.Threading;
using System.Xml;

namespace GameLauncher
{
    enum UpdateType
    {
        Regular = 0,
        InstallatorBased = 1
    }

    sealed class UpdateInfo : IDisposable
    {
        public string PatchFile { get; private set; }
        public long FileSize { get; private set; }
        public string ChangLog { get; private set; }
        public ManualResetEvent State { get; private set; }
        public int LastPatch { get; private set; }
        public UpdateType Type { get; private set; }

        Thread asyncWorker;


        
        string patchServer;
        GameInfo gameInfo;

        public UpdateInfo(GameInfo gameInfo)
        {
            State = new ManualResetEvent(false);
            this.patchServer = gameInfo.PatchServer;
            this.gameInfo = gameInfo;

            asyncWorker = new Thread(AsyncWorker);
            asyncWorker.Start();          
        }

        public void Dispose()
        {
            if (asyncWorker.ThreadState != ThreadState.Aborted || asyncWorker.ThreadState != ThreadState.Stopped || asyncWorker.ThreadState != ThreadState.StopRequested)
                asyncWorker.Abort();
        }


        
        void AsyncWorker()
        {
            NetworkCredential credentials = gameInfo.Credentials;

            try
            {
                Stream responseStream = Extensions.GetFtpResponse(patchServer + "/" + gameInfo.GameId + "/", credentials, WebRequestMethods.Ftp.ListDirectory).GetResponseStream();
                StreamReader sr = new StreamReader(responseStream);

                List<int> patches = new List<int>();
                while (!sr.EndOfStream)
                {
                    string s = sr.ReadLine();
                    patches.Add(int.Parse(s));
                }
                sr.Close();
                LastPatch = patches.Max();

                if (gameInfo.Version < LastPatch)
                {
                    string patchFolder = patchServer + "/" + gameInfo.GameId + "/" + LastPatch + "/files/";
                    PatchFile = patchFolder + "patch.zip";

                    responseStream = Extensions.GetFtpResponse(patchFolder + "changelog.txt", credentials, WebRequestMethods.Ftp.DownloadFile).GetResponseStream();                    
                    sr = new StreamReader(responseStream);
                    ChangLog = sr.ReadToEnd();
                    sr.Close();

                    responseStream = Extensions.GetFtpResponse(patchFolder + "type.txt", credentials, WebRequestMethods.Ftp.DownloadFile).GetResponseStream();
                    sr = new StreamReader(responseStream);
                    Type = (UpdateType)(int.Parse(sr.ReadToEnd()));
                    sr.Close();

                    FileSize = Extensions.GetFtpResponse(PatchFile, credentials, WebRequestMethods.Ftp.GetFileSize).ContentLength;

                }
                else
                {
                    PatchFile = "-1";
                    FileSize = 0;
                }
                State.Set();

            }
            catch
            {
                PatchFile = "-1";
                FileSize = 0;
                State.Set();
                return;
            }
        }
    }
}
