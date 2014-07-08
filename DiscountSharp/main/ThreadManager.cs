using System;
using System.Collections.Generic;
using System.Threading;
using DiscountSharp.net;
using MySql.Data.MySqlClient;
using DiscountSharp.tools;
using DiscountSharp.dump;

namespace DiscountSharp.main
{
    class ThreadManager
    {
        static List<Thread> threads = new List<Thread>();

        public void CheckCircle()
        {
            using (MySqlConnection conn = new MySqlConnection(Connector.DiscountStringConnecting))
            {
                try
                {
                    conn.Open();

                    MySqlCommand cmd = new MySqlCommand("SELECT * FROM `mag_status` WHERE `status` > 0", conn);

                    using (MySqlDataReader dr = cmd.ExecuteReader())
                    {
                        while(dr.Read())
                        {
                            Thread.Sleep(100);

                            var idShop = dr.GetInt32(0);
                            var ipServer = dr.GetString(1);
                            var portServer = dr.GetInt32(2);
                            var dbName = dr.GetString(3);
                            var lastTotalSync = dr.GetDateTime(4).ToString("yyyy-MM-dd,HH:mm:ss");
                            var lastSync = dr.GetDateTime(5).ToString("yyyy-MM-dd,HH:mm:ss");
                            var frequencyDump = dr.GetInt32(6);
                            var frequencyDailyDump = dr.GetInt32(7);
                            var type = dr.GetInt32(8);
                            var status = dr.GetInt32(9);

                            Thread thd = new Thread(delegate()
                            {
                                switch (type) //type
                                {
                                    case 1:
                                        UkmServer ukmserver = new UkmServer(idShop, ipServer, portServer, dbName, lastTotalSync, lastSync, frequencyDump, frequencyDailyDump);
                                        ukmserver.determineTheShopStatus();
                                        break;
                                    case 2:
                                        SetServer setServer = new SetServer(idShop, ipServer, portServer, dbName, lastTotalSync, lastSync, frequencyDump, frequencyDailyDump);
                                        setServer.determineTheShopStatus();
                                        break;
                                    case 3 :
                                        //TODO
                                        break;
                                    default:
                                        break;
                                }
                            });
                            thd.Name = "[" + idShop + "] Thread";
                            thd.Start();
                            threads.Add(thd);
                        }
                    }
                }
                catch(Exception exc)
                {
                    Color.WriteLineColor(exc.Message, ConsoleColor.Red);
                }
            }
        }
    }
}
