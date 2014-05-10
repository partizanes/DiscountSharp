using DiscountSharp.net;
using MySql.Data.MySqlClient;
using System;
using System.Threading;

namespace DiscountSharp.main
{
    class ThreadManager
    {
        public ThreadManager()
        {
            using (MySqlConnection conn = new MySqlConnection(Connector.DiscountStringConnecting))
            {
                conn.Open();

                MySqlCommand cmd = new MySqlCommand(@"SELECT * FROM `mag_status`", conn);

                cmd.CommandTimeout = 0;

                using (MySqlDataReader dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        int magId = dr.GetInt32(0);
                        string ipAddress = dr.GetString(1);
                        DateTime? timeTotalSync = dr.GetDateTime(2);
                        DateTime? lastSync = dr.GetDateTime(3);
                        int codeStatus = dr.GetInt32(4);
                        int form = dr.GetInt32(5);
                        string comment = dr.GetString(6);


                        try {
                            Thread th = new Thread(delegate()
                            {
                       //         MagStatus magstatus = new MagStatus(magId, ipAddress, ipPort codeStatus, form, comment);
                            }); ;
                            th.Name = "CreateThread";
                            th.Start();
                        }
                        catch (Exception exc) {
                            Console.WriteLine(exc.Message);
                        }
                    }
                }
            }
        }
    }
}
