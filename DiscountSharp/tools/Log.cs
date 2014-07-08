using System;
using System.IO;
using System.Threading;

namespace DiscountSharp.tools
{
    class Log
    {
        private static string LogName = "DiscountSharp";
        private static string FileName = "log/" + LogName + ".log";
        private static readonly object syncRoot = new object();

        public static void Write(string str, string reason)
        {
            lock (syncRoot)
            {
                string EntryTime = DateTime.Now.ToLongTimeString();
                string EntryDate = DateTime.Today.ToShortDateString();

                try
                {
                    if (!Directory.Exists(Environment.CurrentDirectory + "/log/"))
                        Directory.CreateDirectory((Environment.CurrentDirectory + "/log/"));

                    StreamWriter sw = new StreamWriter(FileName, true, System.Text.Encoding.UTF8);
                    sw.WriteLine("[" + EntryDate + "][" + EntryTime + "][" + reason + "]" + " " + str);

                    sw.Close();
                }
                catch (Exception exc)
                {
                    Console.WriteLine(exc.StackTrace);
                    Console.WriteLine();
                    Console.WriteLine(exc.Message);
                }
            }
        }
    }
}
