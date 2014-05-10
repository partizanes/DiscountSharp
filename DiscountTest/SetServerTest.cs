using DiscountSharp.dump;
using DiscountSharp.net;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MySql.Data.MySqlClient;

namespace DiscountTest
{
    [TestClass]
    public class SetServerTest
    {
        int idShop;
        string ipSetServer;
        int portSetServer;
        string dbName;
        string lastTotalSync;
        string lastSync;
        int frequencyDump;                          // Сутки
        int frequencyDailyDump;                     // Час
        int type;
        int status;

        [TestMethod]
        public void CreateObjectSetServer()
        {
            GetDbParameters();

            SetServer setServer = new SetServer(idShop, ipSetServer, portSetServer, dbName, lastTotalSync, lastSync, frequencyDump, frequencyDailyDump);
        }

        [TestMethod]
        public void DetermineTheShopStatusSet()
        {
            GetDbParameters();

            SetServer setServer = new SetServer(idShop, ipSetServer, portSetServer, dbName, lastTotalSync, lastSync, frequencyDump, frequencyDailyDump);

            setServer.determineTheShopStatus();
        }

        public void GetDbParameters()
        {
            using (MySqlConnection conn = new MySqlConnection(Connector.DiscountStringConnecting))
            {
                conn.Open();

                MySqlCommand cmd = new MySqlCommand(@"SELECT * FROM `mag_status` WHERE `id` = '12'", conn);

                cmd.CommandTimeout = Connector.commandTimeout;

                using (MySqlDataReader dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        idShop = dr.GetInt32(0);
                        ipSetServer = dr.GetString(1);
                        portSetServer = dr.GetInt32(2);
                        dbName = dr.GetString(3);
                        lastTotalSync = dr.GetDateTime(4).ToString("yyyy-MM-dd,HH:mm:ss"); ;
                        lastSync = dr.GetDateTime(5).ToString("yyyy-MM-dd,HH:mm:ss"); ;
                        frequencyDump = dr.GetInt32(6);
                        frequencyDailyDump = dr.GetInt32(7);
                        type = dr.GetInt32(8);
                        status = dr.GetInt32(9);
                    }
                }
            }
        }
    }
}
