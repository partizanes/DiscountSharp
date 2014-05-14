using DiscountSharp.dump;
using DiscountSharp.net;
using MySql.Data.MySqlClient;
using System;
using System.Threading;

namespace DiscountSharp.tools
{
    public class UkmServer
    {
        /*Создаем экземпляр класса в новом потоке , 
         * в конструкторе передаем параметры 
         * idShop идентификатор магазина
         * ipUkmServer адрес кассового сервера во внутренней сети
         * portUkmServer порт кассового сервера
         * dbName имя используемой базы данных
         * lastTotalSync время последнего дампа сумм за период
         * lastSync время последней синхронизации
         * frequencyDump интервал между дампами сумм в днях (групировка временных данных)
         * frequencyDailyDump интервал между дампами в часах (временные дампы)
         */
        public int idShop { get; private set; }
        public string ipUkmServer { get; private set; }
        public int portUkmServer { get; private set; }
        public string dbName { get; private set;}
        public string lastTotalSync { get; private set; }
        public string lastSync { get; private set; }
        public int frequencyDump { get; private set; }
        public int frequencyDailyDump { get; private set; }

        public UkmServer(int idShop, string ipUkmServer, int portUkmServer, string dbName, string lastTotalSync, string lastSync, int frequencyDump, int frequencyDailyDump)
        {
            this.idShop             = idShop;
            this.ipUkmServer        = ipUkmServer;
            this.portUkmServer      = portUkmServer;
            this.dbName             = dbName;
            this.lastTotalSync      = lastTotalSync;
            this.lastSync           = lastSync;
            this.frequencyDump      = frequencyDump;
            this.frequencyDailyDump = frequencyDailyDump;

            Color.WriteLineColor("Инициализирован объект: Shop: [" + idShop + "] ip: " + ipUkmServer + ":" + portUkmServer + " база данных: " + dbName + " дата глобальной синхронизации: "
                + lastTotalSync + " дата последней синхронизации: " + lastSync, ConsoleColor.DarkYellow);
        }

        public void determineTheShopStatus()
        {
            Color.WriteLineColor("Shop [" + idShop + "] проверка доступности...", ConsoleColor.Green);

            int tryCount = 0;

            while (!Connector.checkAvailability(ipUkmServer))
            {
                Color.WriteLineColor("Shop [" + idShop + "] не доступен [" + tryCount + "] . Следующая попытка через 30 секунд.", ConsoleColor.DarkYellow);
                Log.Write("Shop [" + idShop + "] не доступен.", "[checkAvailability]");

                if (tryCount < 10){
                    tryCount++;
                    Thread.Sleep(30000);
                }
                else
                {
                    Color.WriteLineColor("Shop [" + idShop + "] подключиться не удалось.", ConsoleColor.Red);
                    Log.Write("Shop [" + idShop + "] подключиться не удалось.Отмена проверки.", "[checkAvailability]");
                    Connector.updateStatus(4, idShop);
                    return;
                }
            }

            Color.WriteLineColor("Shop [" + idShop + "] проверка актуальности данных...", ConsoleColor.Green);

            checkAvailabilityDumpDC();
        }

        //Проверка наличия дампа дисконтных карт
        private void checkAvailabilityDumpDC()
        {
            if (lastTotalSync == "0001-01-01,00:00:00")
            {
                Color.WriteLineColor("Shop [" + idShop + "] необходим общий(весь период) дамп дисконтных карт.", ConsoleColor.DarkYellow);

                totalDiscountDump();
            }

            if ((DateTime.Now - DateTime.Parse(lastSync)).TotalHours >= frequencyDailyDump)
            {
                Color.WriteLineColor("Shop [" + idShop + "] производиться временый дамп дисконтных карт.", ConsoleColor.Green);

                discountDumpLastSync();
            }

            if((DateTime.Now - DateTime.Parse(lastTotalSync)).TotalDays >= frequencyDump)
            {
                Color.WriteLineColor("Shop [" + idShop + "] необходимо объединение дампов дисконтных карт.", ConsoleColor.Green);

                string lastSyncPlusOneSecond = DateTime.Parse(lastSync).AddSeconds(1).ToString("yyyy-MM-dd,HH:mm:ss");
                string lastSyncPlusTwoSecond = DateTime.Parse(lastSync).AddSeconds(2).ToString("yyyy-MM-dd,HH:mm:ss");

                Common.totalAndFrequencyDiscountDumpAndClean(idShop, lastSyncPlusOneSecond, lastSyncPlusTwoSecond);

                lastSync = lastSyncPlusTwoSecond;
                lastTotalSync = lastSyncPlusTwoSecond;
            }

            Color.WriteLineColor("[" + idShop + "] Завершено.", ConsoleColor.Magenta);
        }

        //Метод дампа суммы дисконтых карт за весь период до определенной даты
        private int totalDiscountDump()
        {
            Connector.updateStatus(2, idShop);

            string dateTimeDump = DateTime.Now.ToString("yyyy-MM-dd,HH:mm:ss");
            string ukmServerStrConnecting = string.Format("server={0};Port={1};uid={2};pwd={3};database={4};Connect Timeout=15;", ipUkmServer, portUkmServer, "partizanes", "***REMOVED***", dbName);
            MySqlDataReader dr = null;

            using (MySqlConnection connUkmServer = new MySqlConnection(ukmServerStrConnecting))
            {
                try
                {
                    connUkmServer.Open();

                    MySqlCommand cmdUkmServer = new MySqlCommand(@"SELECT ds.card_number, SUM(IF(h.TYPE IN (1, 4, 9, 10), -d.amount, d.amount)), '" + idShop + "' ,'" + dateTimeDump + "'" +
                                          " FROM ukmserver.trm_out_receipt_header h" +
                                          " INNER JOIN ukmserver.trm_out_receipt_subtotal d" +
                                          " ON h.cash_id = d.cash_id AND h.id = d.id" +
                                          " INNER JOIN ukmserver.trm_out_receipt_footer f" +
                                          " ON f.cash_id = d.cash_id AND f.id = d.id" +
                                          " INNER JOIN ukmserver.trm_out_receipt_discounts ds" +
                                          " ON ds.cash_id = h.cash_id AND ds.receipt_header = h.id" +
                                          " WHERE ds.deleted = 0 AND ds.card_number IS NOT NULL" +
                                          " AND f.RESULT = 0 AND h.TYPE IN (0, 5, 1, 4, 8, 9, 10)" +
                                          " GROUP BY ds.card_number;", connUkmServer);

                    cmdUkmServer.CommandTimeout = Connector.commandTimeout;
                    dr = cmdUkmServer.ExecuteReader();
                }
                catch(Exception exc)
                {
                    Color.WriteLineColor("Shop [" + idShop + "] Произошло исключение во время запроса данных дисконтных карт .", ConsoleColor.Red);
                    Log.Write("[" + idShop + "] " + exc.Message,"queryException");
                    return 0;
                }
                
                using (MySqlConnection connDiscountSystem = new MySqlConnection(Connector.DiscountStringConnecting))
                {
                    try
                    {
                        connDiscountSystem.Open();

                        MySqlCommand cmdDiscountSystem = new MySqlCommand(@"INSERT INTO `card_status` VALUES ( @val1 , @val2 , '" + idShop + "' , '" + dateTimeDump + "' );", connDiscountSystem);

                        cmdDiscountSystem.Prepare();

                        cmdDiscountSystem.Parameters.AddWithValue("@val1", "");
                        cmdDiscountSystem.Parameters.AddWithValue("@val2", "");

                        int queryCount = 0;

                        while(dr.Read())
                        {
                            cmdDiscountSystem.Parameters["@val1"].Value = dr.GetValue(0);
                            cmdDiscountSystem.Parameters["@val2"].Value = dr.GetValue(1);

                            if (cmdDiscountSystem.ExecuteNonQuery() > 0)
                                queryCount++;
                        }
                        if (queryCount > 0)
                        {
                            Color.WriteLineColor("Shop [" + idShop + "] сделан дамп сумм дисконтных кард за весь период.Количество записей: " + queryCount, ConsoleColor.DarkYellow);

                            Connector.updateStatus(1, idShop, dateTimeDump, dateTimeDump);
                            lastSync = dateTimeDump;                                                 //Обновляя в базе ,объязательно обновлеям и локальные переменные
                            lastTotalSync = dateTimeDump;

                            return 1;
                        }
                    }
                    catch(Exception exc)
                    {
                        Color.WriteLineColor("Shop [" + idShop + "] Произошло исключение вставки данных дисконтных карт .", ConsoleColor.Red);
                        Log.Write("[" + idShop + "] " + exc.Message, "queryException");

                        Connector.updateStatus(3, idShop);
                        return 0;
                    }
                }
            }

            return 0;
        }

        //Метод переодического(ежедневного дампа данных с последней синхронизации)
        private void discountDumpLastSync()
        {
            Connector.updateStatus(2, idShop);

            string dateTimeNow = DateTime.Now.ToString("yyyy-MM-dd,HH:mm:ss");
            string ukmServerStrConnecting = string.Format("server={0};Port={1};uid={2};pwd={3};database={4};Connect Timeout=15;", ipUkmServer, portUkmServer, "partizanes", "***REMOVED***", dbName);
            MySqlDataReader dr = null;

            using (MySqlConnection connUkmServer = new MySqlConnection(ukmServerStrConnecting))
            {
                try
                {
                    connUkmServer.Open();

                    MySqlCommand cmdUkmServer = new MySqlCommand(@"SELECT ds.card_number, SUM(IF(h.TYPE IN (1, 4, 9, 10), -d.amount, d.amount)), '" + idShop + "' ,'" + dateTimeNow + "'" +
                                          " FROM ukmserver.trm_out_receipt_header h" +
                                          " INNER JOIN ukmserver.trm_out_receipt_subtotal d" +
                                          " ON h.cash_id = d.cash_id AND h.id = d.id" +
                                          " INNER JOIN ukmserver.trm_out_receipt_footer f" +
                                          " ON f.cash_id = d.cash_id AND f.id = d.id" +
                                          " INNER JOIN ukmserver.trm_out_receipt_discounts ds" +
                                          " ON ds.cash_id = h.cash_id AND ds.receipt_header = h.id" +
                                          " WHERE ds.deleted = 0 AND ds.card_number IS NOT NULL" +
                                          " AND f.RESULT = 0 AND h.TYPE IN (0, 5, 1, 4, 8, 9, 10)" +
                                          " AND f.DATE BETWEEN '" + lastSync + "' AND '" + dateTimeNow + "' " +
                                          " GROUP BY ds.card_number;", connUkmServer);

                    cmdUkmServer.CommandTimeout = Connector.commandTimeout;
                    dr = cmdUkmServer.ExecuteReader();
                }
                catch (Exception exc)
                {
                    Color.WriteLineColor("[" + idShop + "] Произошло исключение во время запроса данных дисконтных карт за интервал времени .", ConsoleColor.Red);
                    Log.Write("[" + idShop + "] " + exc.Message, "queryException");

                    Connector.updateStatus(3, idShop);

                    return;
                }

                using (MySqlConnection connDiscountSystem = new MySqlConnection(Connector.DiscountStringConnecting))
                {
                    try
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

                            lastSync = dateTimeNow;
                        }
                        else
                            Color.WriteLineColor("Shop [" + idShop + "] [WARNING] Дамп сумм дисконтных карт за период " + lastSync + " - " + dateTimeNow + "  .Количество записей: " + queryCount, ConsoleColor.Red);

                    }
                    catch (Exception exc)
                    {
                        Color.WriteLineColor("[" + idShop + "] [discountDumpLastSync] ." + exc.Message, ConsoleColor.Red);
                        Log.Write("[" + idShop + "] " + exc.Message, "queryException");
                        Connector.updateStatus(3, idShop);
                    }
                }
            }
        }
    }
}
