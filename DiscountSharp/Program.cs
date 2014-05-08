using DiscountSharp.main;
using DiscountSharp.net;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Threading;

namespace DiscountSharp
{
    class Program
    {
        static List<Thread> threads = new List<Thread>();

        static void Main(string[] args)
        {
            //CheckFirstStart();
        }

        private static void CheckFirstStart()
        {
            using (MySqlConnection conn = new MySqlConnection(Connector.DiscountStringConnecting))
            {
                conn.Open();

                MySqlCommand cmd = new MySqlCommand(@"SELECT * FROM `mag_status` WHERE `date_total_sync` IS NULL", conn);

                cmd.CommandTimeout = Connector.commandTimeout;

                using (MySqlDataReader dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        int magId = dr.GetInt32(0);
                        string ipAddress = dr.GetString(1);
                        //DateTime? timeTotalSync = dr.GetDateTime(2);
                       // DateTime? lastSync = dr.GetDateTime(3);
                        int codeStatus = dr.GetInt32(4);
                        int form = dr.GetInt32(5);
                        string comment = dr.GetString(6);

                        GetDumpDateThread(magId, ipAddress, codeStatus, form, comment);
                    }
                }
            }
        }

        private static void GetDumpDateThread(int magId, string ipAddress, int codeStatus, int form, string comment)
        {
            try
            {
                Thread th = new Thread(delegate()
                {
                    //MagStatus magstatus = new MagStatus(magId, ipAddress, codeStatus, form, comment);
                }); ;
                th.Name = "GetDumpDateThread" + magId;
                th.Start();
                threads.Add(th);
            }
            catch (Exception exc)
            {
                Console.WriteLine(exc.Message);
            }
        }
    }
}
