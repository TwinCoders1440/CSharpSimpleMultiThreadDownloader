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
    class Program
    {
        static private Uri uri;
        static private string templatePath;
        static private string filePath;
        static private int segmentsNumber;
        static private MTDownloader mTDownloader;

        static void Main(string[] args)
        {
            Console.WriteLine("In the name of Allah.\nWelcome to AZBDManager!\n\nFor exit press Ctrl+c\n");
            Console.CancelKeyPress += Console_CancelKeyPress;

            SetParameters();

            mTDownloader = new MTDownloader(uri, templatePath, filePath, segmentsNumber);

            mTDownloader.Download();
            Console.WriteLine("\nDownload Finished. Integrating files...");

            mTDownloader.IntegrateSegmentedFiles();
            Console.WriteLine("Integrated.");

            MTDownloader.DeleteTempFiles();

            Console.ReadKey();
        }

        private static void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            if (mTDownloader != null)
                mTDownloader.SaveChanges();
        }//end Main()
        private static void SetParameters()
        {
            Console.Write("Enter The address: ");
            string address = Console.ReadLine();
            uri = new Uri(address);
            

            Console.Write("Enter the filePath: ");
            filePath = Console.ReadLine();

            templatePath = @"C:\ProgramData\AZBDManager";
            //Console.Write("Enter the templatePath: ");
            //templatePath = Console.ReadLine();

            Console.Write("Enter the segmentsNumber: ");
            segmentsNumber = Int16.Parse(Console.ReadLine());

        }//end SetParameters()

    }//end class Program
}
