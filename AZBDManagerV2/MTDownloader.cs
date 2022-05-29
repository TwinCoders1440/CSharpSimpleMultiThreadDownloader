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
    class MTDownloader : Downloader
    {
        private static int countsOfSegments;
        private Thread[] threadsOfSegments;
        private long[,] fromTos;
        private long[] receivedOfSegments;
        private HttpWebRequest[] socketsOfClients;
        private HttpWebResponse[] socketsOfServer;
        private Stream[] networkStreams;
        private Stream[] fileStreams;
        private int[,] positions;
        private long[] totalSizeOfSegments;
        private long[] unitsForPercentage;
        private long[] tempOFUnitsForPercentage;
        private int[] percentageCounter;
        private bool whereAreYou;

//......properties.....................................................
        public static int CountsOfSegments
        {
            get
            {
                return countsOfSegments;
            }//end get
            set
            {
                if (value > 0 && value <= 32)
                    countsOfSegments = value;
                else
                    throw new ArgumentOutOfRangeException("countsOfSegments", value,
                        "The countsOfSegments must be > 0 and <= 32");
            }//end set

        }//end property ConutsOfSegments

//......Constructor....................................................
        public MTDownloader(Uri uri, string pathOfTemp, string pathOfSave, int segmentsConunt)
            : base(uri, pathOfTemp, pathOfSave)
        {
            CountsOfSegments = segmentsConunt;
            threadsOfSegments = new Thread[CountsOfSegments];
            fromTos = new long[CountsOfSegments, 2];
            receivedOfSegments = new long[CountsOfSegments];
            socketsOfClients = new HttpWebRequest[CountsOfSegments];
            socketsOfServer = new HttpWebResponse[CountsOfSegments];
            networkStreams = new Stream[CountsOfSegments];
            fileStreams = new Stream[CountsOfSegments];
            positions = new int[CountsOfSegments + 1, 2];
            totalSizeOfSegments = new long[CountsOfSegments];
            unitsForPercentage = new long[CountsOfSegments];
            tempOFUnitsForPercentage = new long[CountsOfSegments];
            percentageCounter = new int[CountsOfSegments];

        }//end MTDownloader()*

//......Methods.........................................................
        public override void Download()
        {
            Resume = CheckTempFolder();
            SetFromTos();

            for (int i = 0; i < CountsOfSegments; i++)
            {
                Console.Write("Segment[{0}]: ", i);
                positions[i, 0] = Console.CursorLeft;
                positions[i, 1] = Console.CursorTop;
                Console.WriteLine();
            }//end for
            positions[CountsOfSegments, 0] = Console.CursorLeft;
            positions[CountsOfSegments, 1] = Console.CursorTop;
            Console.CursorVisible = false;

            for (int i = 0; i < CountsOfSegments; i++)
            {
                threadsOfSegments[i] = new Thread(new ParameterizedThreadStart(Do));
                threadsOfSegments[i].Name = i.ToString();

                threadsOfSegments[i].Start(i);
            }//end for

            for (int i = 0; i < CountsOfSegments; i++)
            {
                threadsOfSegments[i].Join();
            }//end for

            Console.CursorLeft = positions[CountsOfSegments, 0];
            Console.CursorTop = positions[CountsOfSegments, 1];
        }
        private bool CheckTempFolder()
        {
            int count = 0;

            if (Directory.Exists(TempPath))
            {
                for (int i = 0; i < CountsOfSegments; i++)
                    if (File.Exists(TempPath + i.ToString() + ".azb"))
                        count++;
            }//end if

            if (count == CountsOfSegments)
            {
                if (File.Exists(TempPath + NameOfData + ".conf"))
                {
                    Console.Write("File existes, resume(y or n)? ");
                    if (Console.ReadLine() == "y" || Console.ReadLine() == "Y")
                        return true;
                    else
                    {
                        DeleteTempFiles();
                        return false;
                    }//end else
                }//end if
                else
                    return false;
            }//end if
            else
                return false;

        }//end CheckTempFolder()
        private void SetFromTos()
        {
            if (!Resume)
            {
                fromTos[0, 0] = 0;
                fromTos[0, 1] = (LenthOfData / CountsOfSegments) - 1;

                for (int i = 1; i < CountsOfSegments; i++)
                {
                    if (i == (CountsOfSegments - 1))
                    {
                        fromTos[i, 0] = fromTos[i - 1, 1] + 1;
                        fromTos[i, 1] = LenthOfData - 1;
                        continue;
                    }//end if

                    fromTos[i, 0] = fromTos[i - 1, 1] + 1;
                    fromTos[i, 1] = fromTos[i, 0] + ((LenthOfData / CountsOfSegments) - 1);
                }//end for
            }//end if
            else
                ReadFromTosFromDisc();

            for (int i = 0; i < CountsOfSegments; i++)
            {
                totalSizeOfSegments[i] = (fromTos[i, 1] - fromTos[i, 0]) + 1;
                unitsForPercentage[i] = totalSizeOfSegments[i] / 100;
                tempOFUnitsForPercentage[i] = totalSizeOfSegments[i] / 100;
            }//end for

        }//end SetTos()
        private void ReadFromTosFromDisc()
        {
            SetTos();

            FileStream fromToFileStream = new FileStream(TempPath + NameOfData + ".conf", FileMode.Open, FileAccess.Read);
            StreamReader streamReader = new StreamReader(fromToFileStream);
            char[] buffer = new char[50 * CountsOfSegments];

            streamReader.Read(buffer, 0, buffer.Length);
            string fromTosString = new string(buffer);

            for (int i = 0; i < CountsOfSegments; i++)
            {
                string sbuffer = fromTosString.Substring(fromTosString.IndexOf(":") + 1);
                fromTos[i, 0] += Convert.ToInt64(sbuffer.Substring(0, sbuffer.IndexOf("  "))) + 1;

            }//end for

            streamReader.Close();
            fromToFileStream.Close();

        }//end ReadSetFromsFromDisc()
        private void SetTos()
        {
            fromTos[0, 0] = 0;
            fromTos[0, 1] = (LenthOfData / CountsOfSegments) - 1;

            for (int i = 1; i < CountsOfSegments; i++)
            {
                if (i == (CountsOfSegments - 1))
                {
                    fromTos[i, 0] = fromTos[i - 1, 1] + 1;
                    fromTos[i, 1] = LenthOfData;
                    continue;
                }//end if

                fromTos[i, 0] = fromTos[i - 1, 1] + 1;
                fromTos[i, 1] = fromTos[i, 0] + ((LenthOfData / CountsOfSegments) - 1);
            }//end for

        }//end SetTos()

        private void Do(object n)
        {
            int thread = (int)n;

            CreateHeader(thread);
            SendRequest(thread);
            ReceiveData(thread);

            for (int i = 0; i < CountsOfSegments; i++)
            {
                if (i == thread)
                    continue;
                if (threadsOfSegments[i].ThreadState == ThreadState.Running)
                    threadsOfSegments[i].Join();

            }//end for
        }//end Do
        private void CreateHeader(int thread)
        {
            socketsOfClients[thread] = WebRequest.Create(UriOfData.ToString()) as HttpWebRequest;

            socketsOfClients[thread].Method = "GET";
            socketsOfClients[thread].UserAgent = "AZBDManager";
            socketsOfClients[thread].KeepAlive = false;
            socketsOfClients[thread].Accept = "gzip";
            //socketsOfClients[thread].Timeout = 20000000;
            if (base.LenthOfData != -2)
                socketsOfClients[thread].AddRange(fromTos[thread, 0], fromTos[thread, 1]);

        }//end CreateHeader()
        private void SendRequest(int thread)
        {
            socketsOfServer[thread] = socketsOfClients[thread].GetResponse() as HttpWebResponse;

        }//end SendRequest()
        private void ReceiveData(int thread)
        {
            byte[] buffer = new byte[1024];
            int received = 0;
            networkStreams[thread] = socketsOfServer[thread].GetResponseStream();

            CreateFolders();
            if (Resume == false)
                fileStreams[thread] = new FileStream(TempPath + thread + ".azb", FileMode.Create, FileAccess.Write);
            else
                fileStreams[thread] = new FileStream(TempPath + thread + ".azb", FileMode.Append, FileAccess.Write);

            do
            {
                try
                {
                    received = networkStreams[thread].Read(buffer, 0, buffer.Length);
                }
                catch (WebException ex)
                {
                    if (receivedOfSegments[thread] != 0)
                    {
                        Console.WriteLine("Thread {0} closed. Reconnection...", thread);
                        socketsOfServer[thread].Close();
                        Resume = true;
                        Do(thread as object);
                    }//end if
                }//end catch

                SaveBufferAndSaveChanges(thread, buffer, received);
                buffer.Initialize();
            } while (received != 0 && (base.LenthOfData != -2 ? ((receivedOfSegments[thread] < fromTos[thread, 1]) ? true : false) : true));

            fileStreams[thread].Dispose();
            networkStreams[thread].Dispose();
            socketsOfServer[thread].Dispose();

            CheckAmountOfDownloaded(thread);

        }//end ReceiveData()
        private void CreateFolders()
        {
            if (!Directory.Exists(TempPath))
               Directory.CreateDirectory(TempPath);

            if (!Directory.Exists(SavePath))
                Directory.CreateDirectory(SavePath);
        }//end CreateFolders()
        private void SaveBufferAndSaveChanges(int thread, byte[] buffer, int received)
        {
            fileStreams[thread].Write(buffer, 0, received);
            fileStreams[thread].Flush();

            fromTos[thread, 0] += received;
            receivedOfSegments[thread] += received;
            whereAreYou = true;

            ShowProgress(thread);

        }//end SaveBufferAndSaveChanges()
        private void CheckAmountOfDownloaded(int thread)
        {
            if (fromTos[thread, 0] < fromTos[thread, 1])
            {
                Resume = true;
                SendRequest(thread);
                ReceiveData(thread);
            }//end if

        }//end CheckAmountOfDownloaded()
        private void ShowProgress(int thread)
        {
            if (receivedOfSegments[thread] >= tempOFUnitsForPercentage[thread])
            {
                percentageCounter[thread]++;

                Console.CursorLeft = positions[thread, 0];
                Console.CursorTop = positions[thread, 1];

                Console.Write("{0}%", percentageCounter[thread]);

                tempOFUnitsForPercentage[thread] += unitsForPercentage[thread];
            }//end if

        }//end ShowProgress()

        public override void SaveChanges()
        {
            if (whereAreYou)
            {
                FileStream fromToFileStream = new FileStream(TempPath + NameOfData + ".conf", FileMode.Create, FileAccess.Write);
                StreamWriter streamWriter = new StreamWriter(fromToFileStream);
                string sBuffer = "";
                for (int i = 0; i < CountsOfSegments; i++)
                {
                    sBuffer += (i + ":" + fromTos[i, 0] + "  " + fromTos[i, 1] + "  ");

                }//end for

                streamWriter.Write(sBuffer);
                streamWriter.Flush();

                streamWriter.Close();
                fromToFileStream.Close();
            }//end if
        }
        public void IntegrateSegmentedFiles()
        {
            FileStream integratedFile = new FileStream(SavePath + NameOfData, FileMode.OpenOrCreate, FileAccess.Write);

            for (int i = 0; i < CountsOfSegments; i++)
            {
                FileStream segmentFile = new FileStream(TempPath + i + ".azb", FileMode.Open, FileAccess.Read);

                int readed = 0;
                byte[] buffer = new byte[1024];

                do
                {
                    readed = segmentFile.Read(buffer, 0, buffer.Length);

                    integratedFile.Write(buffer, 0, readed);
                    integratedFile.Flush();

                } while (readed != 0);

                segmentFile.Close();
            }//end for

            integratedFile.Close();
        }//end IntegrateFilesAndDelete()
        public static void DeleteTempFiles()
        {
            for (int i = 0; i < CountsOfSegments; i++)
                File.Delete(TempPath + i + ".azb");
            File.Delete(TempPath + NameOfData + ".conf");

            Directory.Delete(TempPath);

        }//end DeleteTempFiles()

    }//end class MTDownloader
}
