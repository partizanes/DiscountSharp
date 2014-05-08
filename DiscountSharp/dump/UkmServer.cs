using DiscountSharp.net;
using MySql.Data.MySqlClient;
using System;
using System.Net;
using System.Net.NetworkInformation;
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
                + lastTotalSync + " дата последней синхронизации: " + lastSync, ConsoleColor.Yellow);

            determineTheShopStatus();
        }

        private void determineTheShopStatus()
        {
            updateStatus(2, idShop);

            int tryCount = 0;

            while (!checkAvailability()){
                Color.WriteLineColor(" Shop [" + idShop + "] не доступен [" + tryCount + "] . Следующая попытка через 30 секунд.", ConsoleColor.Red);
                Log.Write(" Shop [" + idShop + "] не доступен.", "[checkAvailability]");

                if (tryCount < 10){
                    tryCount++;
                    Thread.Sleep(30000);
                }
                else
                {
                    Color.WriteLineColor(" Shop [" + idShop + "] Подключиться не удалось.", ConsoleColor.Red);
                    Log.Write(" Shop [" + idShop + "] Подключиться не удалось.Отмена проверки.", "[checkAvailability]");
                    updateStatus(4, idShop);
                    return;
                }
            }

            checkAvailabilityDumpDC();
        }

        //Проверка доступности кассового сервера с помощью пинга
        private bool checkAvailability()
        {
            Ping Pinger = new Ping();
            PingReply Reply = Pinger.Send(IPAddress.Parse(ipUkmServer));

            if (Reply.Status == IPStatus.Success)
                return true;
            else
                return false;
        }

        //Проверка наличия дампа дисконтных карт
        private void checkAvailabilityDumpDC()
        {
            if (lastTotalSync == "0000-00-00 00:00:00")
            {
                Color.WriteLineColor(" Shop " + idShop + " необходим дамп дисконтных карт.", ConsoleColor.Red);

                totalDiscountDump();
            }
            else if((DateTime.Now - DateTime.Parse(lastTotalSync)).TotalDays >= frequencyDump)
            {
                //не запускаем интервальный дамп до очистки для избежания дубликатов
                while (!frequencyDiscountClean())
                    Thread.Sleep(10000);

                frequencyDiscountDump();
            }
            else if ((DateTime.Now - DateTime.Parse(lastSync)).TotalHours >= frequencyDailyDump)
                discountDumpLastSync();
        }

        //Метод дампа суммы дисконтых карт за весь период до определенной даты
        private int totalDiscountDump()
        {
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
                            Color.WriteLineColor("Shop [" + idShop + "] сделан дамп сумм дисконтных кард за весь период.Количество записей: " + queryCount, ConsoleColor.Red);

                            updateStatus(1, idShop, dateTimeDump, dateTimeDump);
                            return 1;
                        }
                    }
                    catch(Exception exc)
                    {
                        Color.WriteLineColor("Shop [" + idShop + "] Произошло исключение вставки данных дисконтных карт .", ConsoleColor.Red);
                        Log.Write("[" + idShop + "] " + exc.Message, "queryException");

                        updateStatus(3, idShop);
                        return 0;
                    }
                }
            }

            return 0;
        }

        //Метод дампа сумм дисконтных карт от даты последнего дампа до текущего момента
        private void frequencyDiscountDump()
        {
            string dateTimeNow = DateTime.Now.ToString("yyyy-MM-dd,HH:mm:ss");
            string ukmServerStrConnecting = string.Format("server={0};Port={1};uid={2};pwd={3};database={4};Connect Timeout=15;", ipUkmServer, portUkmServer, "partizanes", "***REMOVED***", dbName);

            updateStatus(2, idShop);

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
                                          " AND f.DATE BETWEEN '" + lastTotalSync + "' AND '" + dateTimeNow + "' " +
                                          " GROUP BY ds.card_number;", connUkmServer);

                    cmdUkmServer.CommandTimeout = Connector.commandTimeout;
                    
                    dr = cmdUkmServer.ExecuteReader();
                }
                catch (Exception exc)
                {
                    Color.WriteLineColor("[" + idShop + "] [frequencyDiscountDump] " + exc.Message, ConsoleColor.Red);
                    Log.Write("[" + idShop + "] " + exc.Message, "[frequencyDiscountDump]");
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
                            Color.WriteLineColor("Shop [" + idShop + "] сделан дамп сумм дисконтных карт с времени последней синхронизации" + lastSync + " - " + dateTimeNow + "  .Количество записей: " + queryCount, ConsoleColor.Green);

                            updateStatus(1, idShop, dateTimeNow);
                        }
                    }
                    catch (Exception exc)
                    {
                        Color.WriteLineColor("[" + idShop + "] Произошло исключение вставки данных дисконтных карт .", ConsoleColor.Red);
                        Log.Write("[" + idShop + "] " + exc.Message, "[frequencyDiscountDump]");
                        updateStatus(3, idShop);
                    }
                }
            }
        }

        //Метод очистки ежедневных дампов перед frequencyDiscountDump
        private bool frequencyDiscountClean()
        {
            using (MySqlConnection conn = new MySqlConnection(Connector.DiscountStringConnecting))
            {
                try
                {
                    conn.Open();

                    MySqlCommand cmd = new MySqlCommand(@"DELETE FROM `card_status` WHERE  `date_operation` > '" + lastTotalSync + "' AND `mag_id` = '" + idShop + "';", conn);

                    int queryRow = 0;

                    queryRow = cmd.ExecuteNonQuery();

                    if (queryRow > 0){
                        Color.WriteLineColor("Shop [" + idShop + "] удалено временных [ " + queryRow + " ] записей.",ConsoleColor.Yellow);
                        Log.Write("Shop [" + idShop + "] удалено [ " + queryRow + " ] временных записей.", "Delete");
                       
                        updateStatus(1, idShop);
                        return true;
                    }
                }
                catch (Exception exc)
                {
                    Color.WriteLineColor("[" + idShop + "] frequencyDiscountClean очистка неудачна.", ConsoleColor.Red);
                    Log.Write("[" + idShop + "] " + exc.Message, "frequencyDiscountClean");
                    
                    updateStatus(3, idShop);
                    return false;
                }
                return false;
            }
        }

        //Метод переодического(ежедневного дампа данных с последней синхронизации)
        private void discountDumpLastSync()
        {
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
                            Color.WriteLineColor("Shop [" + idShop + "] сделан дамп сумм дисконтных карт за период " + lastTotalSync + " - " + dateTimeNow + "  .Количество записей: " + queryCount, ConsoleColor.Green);

                            updateStatus(1, idShop, dateTimeNow);
                        }
                    }
                    catch (Exception exc)
                    {
                        Color.WriteLineColor("[" + idShop + "] [discountDumpLastSync] ." + exc.Message, ConsoleColor.Red);
                        Log.Write("[" + idShop + "] " + exc.Message, "queryException");
                        updateStatus(3, idShop);
                    }
                }
            }
        }

        public static string returnStatusText(int status)
        {
            switch (status)
            {
                case 0:
                    return status + @" [не активен]";
                case 1:
                    return status + @" [разблокирован]";
                case 2:
                    return status + @" [в процессе обновления]";
                case 3:
                    return status + @" [ошибка]";
                case 4:
                    return status + @" [ошибка подключения]";
                default:
                    return status + @" [неизвестно]";
            }
        }

        public static bool updateStatus(int status, int idShop)
        {
            using (MySqlConnection conn = new MySqlConnection(Connector.DiscountStringConnecting))
            {
                conn.Open();

                MySqlCommand cmd = new MySqlCommand("UPDATE mag_status SET `status` = @val1 WHERE id = @val2  ", conn);
                cmd.CommandTimeout = Connector.commandTimeout;
                cmd.Parameters.AddWithValue("@val1", status);
                cmd.Parameters.AddWithValue("@val2", idShop);
                cmd.Prepare();

                int i = cmd.ExecuteNonQuery();

                if (i == 1)
                {
                    Color.WriteLineColor("Shop [" + idShop + "] обновлен статус " + UkmServer.returnStatusText(status), ConsoleColor.Yellow);
                    return true;
                }
                else
                {
                    Color.WriteLineColor("Shop [" + idShop + "]  Не удалось обновить статус.", ConsoleColor.Red);
                    return false;
                }
            }
        }

        public static bool updateStatus(int status, int idShop, string lastSync)
        {
            using (MySqlConnection conn = new MySqlConnection(Connector.DiscountStringConnecting))
            {
                conn.Open();

                MySqlCommand cmd = new MySqlCommand("UPDATE mag_status SET `status` = @val1 , `last_sync` = @val3 WHERE id = @val2  ", conn);
                cmd.CommandTimeout = Connector.commandTimeout;
                cmd.Parameters.AddWithValue("@val1", status);
                cmd.Parameters.AddWithValue("@val2", idShop);
                cmd.Parameters.AddWithValue("@val3", lastSync);
                cmd.Prepare();

                int i = cmd.ExecuteNonQuery();

                if (i == 1)
                {
                    Color.WriteLineColor("Shop [" + idShop + "] обновлен статус  " + UkmServer.returnStatusText(status) + " и last_sync = " + lastSync, ConsoleColor.Yellow);
                    return true;
                }
                else
                {
                    Color.WriteLineColor("Shop [" + idShop + "]  Не удалось обновить статус и last_sync", ConsoleColor.Red);
                    return false;
                }
            }
        }

        public static bool updateStatus(int status, int idShop, string lastSync, string lastTotalSync)
        {
            using (MySqlConnection conn = new MySqlConnection(Connector.DiscountStringConnecting))
            {
                conn.Open();

                MySqlCommand cmd = new MySqlCommand("UPDATE mag_status SET `status` = @val1 , `last_Total_Sync` = @val3 , `last_sync` = @val4 WHERE id = @val2  ", conn);
                cmd.CommandTimeout = Connector.commandTimeout;
                cmd.Parameters.AddWithValue("@val1", status);
                cmd.Parameters.AddWithValue("@val2", idShop);
                cmd.Parameters.AddWithValue("@val3", lastTotalSync);
                cmd.Parameters.AddWithValue("@val4", lastSync);
                cmd.Prepare();

                int i = cmd.ExecuteNonQuery();

                if (i == 1)
                {
                    Color.WriteLineColor("Shop [" + idShop + "] обновлен статус  " + UkmServer.returnStatusText(status) + " и last_Total_Sync = " + lastTotalSync, ConsoleColor.Yellow);
                    return true;
                }
                else
                {
                    Color.WriteLineColor("Shop [" + idShop + "]  Не удалось обновить статус и last_Total_Sync", ConsoleColor.Red);
                    return false;
                }
            }
        }
    }
}
