/**
 * GameInfo class
 * Contains information about game, eg version, gameId
 * Owner: Standy
 * */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Net;
using System.Web.Script.Serialization;

namespace GameLauncher
{
    sealed class GameInfo
    {
        public string GameId { get; private set; }
        public int Version { get; private set; }
        public string GameExe { get; private set; }
        public string PatchServer { get; private set; }
        public NetworkCredential Credentials { get; private set; }
        public string SetupExe { get; private set; }


        bool useBuiltinCredentials;

        private void Load(string gameIdPath, string versionPath, string gameExePath, string credentialsPath)
        {
            
            using (StreamReader sr = new StreamReader(Extensions.GetAbsolutePath(gameIdPath)))
            {
                GameId = sr.ReadLine();
            }

            using (StreamReader sr = new StreamReader(Extensions.GetAbsolutePath(versionPath)))
            {
                Version = int.Parse(sr.ReadLine());
            }

            using (StreamReader sr = new StreamReader(Extensions.GetAbsolutePath(gameExePath)))
            {
                GameExe = Extensions.GetAbsolutePath(sr.ReadLine());
            }

            using (StreamReader sr = new StreamReader(Extensions.GetAbsolutePath(credentialsPath)))
            {
                bool use = bool.Parse(sr.ReadLine());
                PatchServer = sr.ReadLine();
                string login = sr.ReadLine();
                string pass = sr.ReadLine();
                if (use)
                    Credentials = new NetworkCredential(login, pass);
                else
                    Credentials = new NetworkCredential("login", "pass");
            }
        }

        private void LoadJSON(string configFile, string versionFile)
        {
            JavaScriptSerializer c = new JavaScriptSerializer();
            using (StreamReader sr = new StreamReader(configFile))
            {
                var result = c.DeserializeObject(sr.ReadToEnd()) as Dictionary<string, object>;
                GameId = result["gameId"] as string;
                GameExe = result["gameExe"] as string;
                PatchServer = result["patchServer"] as string;
                SetupExe = result["patchSetupExe"] as string;
                useBuiltinCredentials = (bool)result["useBuiltInCredentials"];
                
                if (useBuiltinCredentials)
                    Credentials = new NetworkCredential("login", "pass");
                else
                {
                    var creds = result["credentials"] as Dictionary<string, object>;
                    Credentials = new NetworkCredential(creds["login"] as string, creds["password"] as string);
                }
            }

            using (StreamReader sr = new StreamReader(versionFile))
            {
                Version = int.Parse(sr.ReadLine());
            }
        }


        public GameInfo(string configFile = "GameLauncher.json", string versionFile = "version.txt")
        {
            if (File.Exists(configFile) && File.Exists(versionFile))
                LoadJSON(configFile, versionFile);
            else
                throw new ArgumentException("Some necessery files are missing");
        }
        
    }
}
