using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Net;

namespace GameLauncher
{
    static class Extensions
    {
        public static string GetAbsolutePath(string relativePath)
        {
            return Directory.GetCurrentDirectory() + "\\" + relativePath;
        }

        public static FtpWebResponse GetFtpResponse(string requestPath, NetworkCredential credentials, string method)
        {
            FtpWebRequest request = (FtpWebRequest)WebRequest.Create(requestPath);
            request.Credentials = credentials;
            request.Method = method;

            return (FtpWebResponse)request.GetResponse();
        }


    }
}
