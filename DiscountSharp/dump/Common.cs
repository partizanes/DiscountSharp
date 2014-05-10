using DiscountSharp.net;
using DiscountSharp.tools;
using MySql.Data.MySqlClient;
using System;

namespace DiscountSharp.dump
{
    class Common
    {

        // метод передической групировки временных дампов и объединение с глобальными
        public static void totalAndFrequencyDiscountDumpAndClean(int idShop, string lastSyncPlusOneSecond, string lastSyncPlusTwoSecond)
        {
            Connector.updateStatus(2, idShop);

            string query = "INSERT INTO `card_status` SELECT `id_card`, SUM(`sum_card`),'" + idShop + "','" + lastSyncPlusOneSecond + "' FROM `card_status`" +      // Выборка и вставка (групировка) всех данных с момента
                " WHERE `date_operation` > (SELECT `last_total_sync` FROM `mag_status` WHERE `id` = '" + idShop + "')" +                                            // последней глобальной синхронизации по магазину .
                " AND `mag_id` = '" + idShop + "' GROUP BY `id_card`;" +                                                                                            // с датой последней синхронизации + 1 секунда
                " DELETE FROM `card_status` WHERE `mag_id` = '" + idShop + "' AND `date_operation` >  " +                                                           // Удаление дубликатов записей между 
                " (SELECT `last_total_sync` FROM `mag_status` WHERE `id` = '" + idShop + "') " +                                                                    // last_total_sync и last_sync
                " AND `date_operation` <= (SELECT `last_sync` FROM `mag_status` WHERE `id` = '" + idShop + "');" +                                                  // так как они сгрупированы в первом запросе
                " UPDATE `mag_status` SET `last_sync` = '" + lastSyncPlusOneSecond + "' WHERE `id` = '" + idShop + "';" +                                           // Обновление даты последней синхронизации в mag_status
                " INSERT INTO `card_status` SELECT `id_card`, SUM(`sum_card`),'" + idShop + "','" + lastSyncPlusTwoSecond +                                         // Выборка и вставка (групировка) данных 
                "' FROM `card_status` WHERE `mag_id` = '" + idShop + "' GROUP BY `id_card`;" +                                                                      // last_total_sync + все остальное
                " DELETE FROM `card_status` WHERE `mag_id` = '" + idShop + "' AND `date_operation` < '" + lastSyncPlusTwoSecond + "';" +                            // Удаление дубликатов после групировки по признаку < lastSyncPlusTwoSecond
                " UPDATE `mag_status` SET `last_sync` = '" + lastSyncPlusTwoSecond + "' , `last_total_sync` = '" + lastSyncPlusTwoSecond +                          // Обновление даты последней синхронизации в mag_status
                "' WHERE `id` = '" + idShop + "';";

            if(CreateCommand(query))
                Connector.updateStatus(1, idShop);
            else
                Connector.updateStatus(3, idShop);
        }

        private static bool CreateCommand(string queryString)
        {
            using (MySqlConnection conn = new MySqlConnection(Connector.DiscountStringConnecting))
            {
                try
                {
                    MySqlCommand cmd = new MySqlCommand(queryString, conn);
                    cmd.Connection.Open();
                    cmd.ExecuteNonQuery();

                    return true;
                }
                catch (Exception exc) {
                    Color.WriteLineColor("[CreateCommand] " + exc.Message, ConsoleColor.Red);
                    return false;
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

    }
}
