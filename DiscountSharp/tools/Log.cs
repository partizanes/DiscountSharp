using System;
using System.IO;

namespace DiscountSharp.tools
{
    class Log
    {
        public static void Write(string str, string reason)
        {
            string LogName = "DiscountSharp";
            string EntryTime = DateTime.Now.ToLongTimeString();
            string EntryDate = DateTime.Today.ToShortDateString();
            string FileName = "log/" + LogName + ".log";

            try
            {
                if (!Directory.Exists(Environment.CurrentDirectory + "/log/"))
                    Directory.CreateDirectory((Environment.CurrentDirectory + "/log/"));

                StreamWriter sw = new StreamWriter(FileName, true, System.Text.Encoding.UTF8);
                sw.WriteLine("[" + EntryDate + "][" + EntryTime + "][" + reason + "]" + " " + str);

                sw.Close();
                sw.Dispose();
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
