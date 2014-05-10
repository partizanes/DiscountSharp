using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using DiscountSharp.tools;
using DiscountSharp.net;
using MySql.Data.MySqlClient;

namespace DiscountTest
{
    [TestClass]
    public class UkmServerTest
    {
        int idShop;
        string ipUkmServer;
        int portUkmServer;
        string dbName;
        string lastTotalSync;
        string lastSync;
        int frequencyDump;                          // Сутки
        int frequencyDailyDump;                     // Час
        int type;
        int status;

        [TestMethod]
        public void CreateObjectUkmServer()
        {
            GetDbParameters();

            UkmServer ukmserver = new UkmServer(idShop, ipUkmServer, portUkmServer, dbName, lastTotalSync, lastSync, frequencyDump, frequencyDailyDump);
        }

        [TestMethod]
        public void DetermineTheShopStatus()
        {
            GetDbParameters();

            UkmServer ukmserver = new UkmServer(idShop, ipUkmServer, portUkmServer, dbName, lastTotalSync, lastSync, frequencyDump, frequencyDailyDump);

            ukmserver.determineTheShopStatus();
        }

        public void GetDbParameters()
        {
            using (MySqlConnection conn = new MySqlConnection(Connector.DiscountStringConnecting))
            {
                conn.Open();

                MySqlCommand cmd = new MySqlCommand(@"SELECT * FROM `mag_status` WHERE `id` = '41'", conn);

                cmd.CommandTimeout = Connector.commandTimeout;

                using (MySqlDataReader dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        idShop              = dr.GetInt32(0);
                        ipUkmServer         = dr.GetString(1);
                        portUkmServer       = dr.GetInt32(2);
                        dbName              = dr.GetString(3);
                        lastTotalSync       = dr.GetDateTime(4).ToString("yyyy-MM-dd,HH:mm:ss");;
                        lastSync            = dr.GetDateTime(5).ToString("yyyy-MM-dd,HH:mm:ss");;
                        frequencyDump       = dr.GetInt32(6);
                        frequencyDailyDump  = dr.GetInt32(7);
                        type                = dr.GetInt32(8);
                        status              = dr.GetInt32(9);
                    }
                }
            }
        }
    }
}
