//In the name of Allah.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Net.Sockets;
using System.IO;
using System.IO.Compression;
using System.Net;

namespace AZBDManagerV2
{
    abstract class Downloader
    {
        //private Uri uriOfData;
        private static string nameOfData;
        private long lenthOfData;
        private static string tempPath;
        private static string savePath;

//......properties.....................................................
        public Uri UriOfData { get; set; }
        public static string NameOfData
        {
            get
            {
                return nameOfData;
            }//end get
            set
            {
                if (value.Contains("\\") || value.Contains("/") || value.Contains(":") || value.Contains("?") || value.Contains("*") || value.Contains("\"") || value.Contains("<") || value.Contains(">") || value.Contains("|"))
                    throw new Exception("Name cann't contain \\, /, ?, *, :, \", <, >, |.");
                else
                    nameOfData = value;
            }//end set
        }//end property NameOfData
        public long LenthOfData
        {
            get
            {
                return lenthOfData;
            }//end get
            set
            {
                if (value > 0 || value == -2)
                    lenthOfData = value;
                else
                    throw new ArgumentOutOfRangeException("lengthOFData", value,
                        "lengthOfData must be > 0");
            }//end set
        }//end property LenthOfData
        public static string TempPath
        {
            get
            {
                return tempPath;
            }//end get
            set
            {
                if (!value.Contains("/"))
                {
                    if (value[value.Length - 1] == '\\')
                        tempPath = value + NameOfData + "\\";
                    else
                        tempPath = value + "\\" + NameOfData + "\\";
                }//end if
                else
                    throw new Exception("tempPath can not contain /");
            }//end set
        }//end property TempPath
        public static string SavePath
        {
            get
            {
                return savePath;
            }//end get
            set
            {
                if (!value.Contains("/"))
                {
                    if (value[value.Length - 1] == '\\')
                        savePath = value;
                    else
                        savePath = value + "\\";
                }//end if
                else
                    throw new Exception("savePath can not contain /");
            }//end set
        }//end property TempPath
        public bool Resume { get; set; }

//......Constructor....................................................
        public Downloader(Uri uri, string pathOfTemp, string pathOfSave)
        {
            UriOfData = uri;
            NameOfData = GetNameOfDataFromUri();
            LenthOfData = GetLengthOfData();
            TempPath = pathOfTemp;
            SavePath = pathOfSave;

        }//end Downloader()*

//......Methods.........................................................
        private string GetNameOfDataFromUri()
        {
            string address = UriOfData.AbsolutePath;

            while (address.Contains("/"))
            {
                address = address.Substring(address.IndexOf("/") + 1);

            }//end while

            return address;
        }//end GetNameOfDataFromUri()
        private long GetLengthOfData()
        {
            HttpWebRequest clientSocket;
            HttpWebResponse serverSocket;
            WebHeaderCollection responseHeader;

            clientSocket = WebRequest.Create(UriOfData.OriginalString) as HttpWebRequest;
            clientSocket.Method = "GET";
            clientSocket.UserAgent = "AZBDManager";
            clientSocket.KeepAlive = false;
            clientSocket.Accept = "gzip";
            clientSocket.Timeout = 10000000;

            serverSocket = (HttpWebResponse)clientSocket.GetResponse();

            responseHeader = serverSocket.Headers;
            serverSocket.Close();

            if (responseHeader.Get("Transfer-Encoding") == "chunked")
                return -2;
            else
                return Convert.ToInt64(responseHeader.Get("Content-Length"));
            
        }//end GetLengthOfData()

        public abstract void Download();
        public abstract void SaveChanges();

    }//end class Downloader
}
