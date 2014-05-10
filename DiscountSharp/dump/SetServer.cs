using DiscountSharp.net;
using DiscountSharp.tools;
using MySql.Data.MySqlClient;
using System;
using System.Data.SqlClient;
using System.Threading;

namespace DiscountSharp.dump
{
    public class SetServer
    {
        public int idShop { get; private set; }
        public string ipSetServer { get; private set; }
        public int portSetServer { get; private set; }
        public string dbName { get; private set; }
        public string lastTotalSync { get; private set; }
        public string lastSync { get; private set; }
        public int frequencyDump { get; private set; }
        public int frequencyDailyDump { get; private set; }

        public SetServer(int idShop, string ipSetServer, int portSetServer, string dbName, string lastTotalSync, string lastSync, int frequencyDump, int frequencyDailyDump)
        {
            this.idShop             = idShop;
            this.ipSetServer        = ipSetServer;
            this.portSetServer      = portSetServer;
            this.dbName             = dbName;
            this.lastTotalSync      = lastTotalSync;
            this.lastSync           = lastSync;
            this.frequencyDump      = frequencyDump;
            this.frequencyDailyDump = frequencyDailyDump;

            Color.WriteLineColor("Инициализирован объект: Shop: [" + idShop + "] ip: " + ipSetServer + ":" + portSetServer + " база данных: " + dbName + " дата глобальной синхронизации: "
                + lastTotalSync + " дата последней синхронизации: " + lastSync, ConsoleColor.Yellow);
        }

        public void determineTheShopStatus()
        {
            int tryCount = 0;

            while (!Connector.checkAvailability(ipSetServer))
            {
                Color.WriteLineColor(" Shop [" + idShop + "] не доступен [" + tryCount + "] . Следующая попытка через 30 секунд.", ConsoleColor.Red);
                Log.Write(" Shop [" + idShop + "] не доступен.", "[checkAvailability]");

                if (tryCount < 10)
                {
                    tryCount++;
                    Thread.Sleep(30000);
                }
                else
                {
                    Color.WriteLineColor(" Shop [" + idShop + "] Подключиться не удалось.", ConsoleColor.Red);
                    Log.Write(" Shop [" + idShop + "] Подключиться не удалось.Отмена проверки.", "[checkAvailability]");
                    Connector.updateStatus(4, idShop);
                    return;
                }
            }

            Connector.updateStatus(2, idShop);

            checkAvailabilityDumpDC();
        }

        private void checkAvailabilityDumpDC()
         {
             if (lastTotalSync == "0001-01-01,00:00:00")
             {
                 Color.WriteLineColor(" Shop " + idShop + " необходим дамп дисконтных карт.", ConsoleColor.Red);

                 totalDiscountDump();
             }
             else if ((DateTime.Now - DateTime.Parse(lastTotalSync)).TotalDays >= frequencyDump)
             {
                 string lastSyncPlusOneSecond = DateTime.Parse(lastSync).AddSeconds(1).ToString("yyyy-MM-dd HH:mm:ss");
                 string lastSyncPlusTwoSecond = DateTime.Parse(lastSync).AddSeconds(2).ToString("yyyy-MM-dd HH:mm:ss");

                 Common.totalAndFrequencyDiscountDumpAndClean(idShop, lastSyncPlusOneSecond, lastSyncPlusTwoSecond);

                 lastSync = lastSyncPlusTwoSecond;
                 lastTotalSync = lastSyncPlusTwoSecond;
             }
             else if ((DateTime.Now - DateTime.Parse(lastSync)).TotalHours >= frequencyDailyDump)
                 discountDumpLastSync();
         }

        // Метод глобального дампа данных за все время
        private int totalDiscountDump()
        {
            string dateTimeDump = DateTime.Now.ToString("yyyy-MM-dd,HH:mm:ss");
            string setServerStrConnecting = string.Format("Server={0},{1};Database={2};User Id={3};Password={4};", ipSetServer, portSetServer, dbName, "partizanes", "***REMOVED***");

            try
            {
                using (SqlConnection connSetServer = new SqlConnection(setServerStrConnecting))
                {
                    string queryString = @"USE SES;SELECT DiscountCards.BarCode AS CardNumber, SUM(ChequePos.Price * ChequePos.Quant) AS summa 
                                           FROM ChequePos INNER JOIN ChequeDisc ON ChequePos.Id = ChequeDisc.PosId 
                                           INNER JOIN ChequeHead ON ChequePos.ChequeId = ChequeHead.Id 
                                           INNER JOIN DiscountCards ON ChequeDisc.DiscId = DiscountCards.Id 
                                           GROUP BY DiscountCards.Perc, DiscountCards.BarCode";

                    using (SqlCommand cmd = new SqlCommand(queryString, connSetServer))
                    {
                        connSetServer.Open();

                        using (SqlDataReader dr = cmd.ExecuteReader())
                        {
                            using (MySqlConnection connDiscountSystem = new MySqlConnection(Connector.DiscountStringConnecting))
                            {

                                connDiscountSystem.Open();

                                MySqlCommand cmdDiscountSystem = new MySqlCommand(@"INSERT INTO `card_status` VALUES ( @val1 , @val2 , '" + idShop + "' , '" + dateTimeDump + "' );", connDiscountSystem);

                                cmdDiscountSystem.Prepare();

                                cmdDiscountSystem.Parameters.AddWithValue("@val1", "");
                                cmdDiscountSystem.Parameters.AddWithValue("@val2", "");

                                int queryCount = 0;

                                while (dr.Read())
                                {
                                    cmdDiscountSystem.Parameters["@val1"].Value = dr.GetValue(0);
                                    cmdDiscountSystem.Parameters["@val2"].Value = dr.GetValue(1);

                                    if (cmdDiscountSystem.ExecuteNonQuery() > 0)
                                        queryCount++;
                                }

                                if (queryCount > 0)
                                {
                                    Color.WriteLineColor("Shop [" + idShop + "] сделан дамп сумм дисконтных кард за весь период.Количество записей: " + queryCount, ConsoleColor.Red);

                                    Connector.updateStatus(1, idShop, dateTimeDump, dateTimeDump);

                                    lastSync = dateTimeDump;                                                 //Обновляя в базе ,объязательно обновлеям и локальные переменные
                                    lastTotalSync = dateTimeDump;

                                    return 1;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception exc)
            {
                Color.WriteLineColor("Shop [" + idShop + "] [totalDiscountDump] [SET] Исключение во время запроса глобального дампа дисконтных карт .", ConsoleColor.Red);
                Log.Write("[" + idShop + "] " + exc.Message, "[totalDiscountDump] [SET]");

                Connector.updateStatus(3, idShop);
                return 0;
            }

            return 0;
        }

        // Дамп данных с даты последней синхронизации по текущее время
        private int discountDumpLastSync()
        {
            string dateTimeNow = DateTime.Now.ToString("yyyy-MM-dd,HH:mm:ss");
            string setServerStrConnecting = string.Format("Server={0},{1};Database={2};User Id={3};Password={4};", ipSetServer, portSetServer, dbName, "partizanes", "***REMOVED***");

            try
            {
                using (SqlConnection connSetServer = new SqlConnection(setServerStrConnecting))
                {
                    string queryString = @"USE SES;SELECT DiscountCards.BarCode AS CardNumber, (Cast(SUM(ChequePos.Price * ChequePos.Quant) AS Integer)) as summa" +
                                           " FROM ChequePos INNER JOIN ChequeDisc ON ChequePos.Id = ChequeDisc.PosId " +
                                           " INNER JOIN ChequeHead ON ChequePos.ChequeId = ChequeHead.Id " +
                                           " INNER JOIN DiscountCards ON ChequeDisc.DiscId = DiscountCards.Id " +
                                           " WHERE ChequeHead.DateOperation Between '" + lastSync.Replace(",", " ") + "' and '" + dateTimeNow.Replace(",", " ") + "' GROUP BY DiscountCards.Perc, DiscountCards.BarCode";

                    using (SqlCommand cmd = new SqlCommand(queryString, connSetServer))
                    {
                        connSetServer.Open();

                        using (SqlDataReader dr = cmd.ExecuteReader())
                        {
                            using (MySqlConnection connDiscountSystem = new MySqlConnection(Connector.DiscountStringConnecting))
                            {

                                connDiscountSystem.Open();

                                MySqlCommand cmdDiscountSystem = new MySqlCommand(@"INSERT INTO `card_status` VALUES ( @val1 , @val2 , '" + idShop + "' , '" + dateTimeNow + "' );", connDiscountSystem);

                                cmdDiscountSystem.Prepare();

                                cmdDiscountSystem.Parameters.AddWithValue("@val1", "");
                                cmdDiscountSystem.Parameters.AddWithValue("@val2", "");

                                int queryCount = 0;

                                while (dr.Read())
                                {
                                    cmdDiscountSystem.Parameters["@val1"].Value = dr.GetValue(0);
                                    cmdDiscountSystem.Parameters["@val2"].Value = dr.GetValue(1);

                                    if (cmdDiscountSystem.ExecuteNonQuery() > 0)
                                        queryCount++;
                                }

                                if (queryCount > 0)
                                {
                                    Color.WriteLineColor("Shop [" + idShop + "] сделан дамп сумм дисконтных карт за период " + lastSync + " - " + dateTimeNow + "  .Количество записей: " + queryCount, ConsoleColor.Green);

                                    Connector.updateStatus(1, idShop, dateTimeNow);

                                    lastSync = dateTimeNow;                                                 //Обновляя в базе ,объязательно обновляем и локальные переменные
                                    return 1;
                                }
                                else { Color.WriteLineColor("Shop [" + idShop + "] [WARNING] Дамп сумм дисконтных карт за период " + lastSync + " - " + dateTimeNow + "  .Количество записей: " + queryCount, ConsoleColor.Red); }
                            }
                        }
                    }
                }
            }
            catch (Exception exc)
            {
                Color.WriteLineColor("Shop [" + idShop + "] [totalDiscountDump] [SET] Исключение во время запроса  дисконтных карт за период " + lastTotalSync + " - " + dateTimeNow, ConsoleColor.Red);
                Log.Write("[" + idShop + "] " + exc.Message, "[totalDiscountDump] [SET]");

                Connector.updateStatus(3, idShop);
                return 0;
            }

            return 0;
        }

        public static void CreateCommand(string queryString,string connectionString)
        {
            connectionString = "Server=192.168.12.100;Database=SES;User Id=partizanes;Password=***REMOVED***;";

            using (SqlConnection connection = new SqlConnection(
                       connectionString))
            {
                SqlCommand command = new SqlCommand(queryString, connection);
                command.Connection.Open();
                int i = command.ExecuteNonQuery();
                Color.WriteLineColor(i.ToString(), ConsoleColor.Green);
            }
        }
    }
}
