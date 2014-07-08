using DiscountSharp.dump;
using DiscountSharp.tools;
using MySql.Data.MySqlClient;
using System;
using System.Net;
using System.Net.NetworkInformation;

namespace DiscountSharp.net
{
    public static class Connector
    {
        private static string ipDiscountServer = Config.GetParametr("ipDiscountServer");
        private static string portDiscountServer = Config.GetParametr("PortDiscountServer");
        private static string discountSharpDb = Config.GetParametr("discountBdName");
        public static int commandTimeout = int.Parse(Config.GetParametr("commandTimeout"));
        private static int connectTimeout = int.Parse(Config.GetParametr("connectTimeout"));
        public static string DiscountStringConnecting = string.Format("server={0};Port={1};uid={2};pwd={3};database={4};Convert Zero Datetime=True;Connect Timeout="
            + connectTimeout + ";", ipDiscountServer, portDiscountServer, "partizanes", "***REMOVED***", discountSharpDb);

        public static bool updateStatus(int status,int idShop)
        {
            using (MySqlConnection conn = new MySqlConnection(DiscountStringConnecting))
            {
                try
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
                        Color.WriteLineColor("Shop [" + idShop + "] обновлен статус - " + Common.returnStatusText(status), ConsoleColor.Yellow);
                        return true;
                    }
                    else
                    {
                        Color.WriteLineColor("Shop [" + idShop + "]  Не удалось обновить статус.", ConsoleColor.Red);
                        return false;
                    }
                }
                catch (Exception exc)
                {
                    Color.WriteLineColor("[updateStatus]" + exc.Message, ConsoleColor.Red);
                    Log.Write(exc.Message, "[updateStatus]");
                    return false;
                }
            }
        }

        public static bool updateStatus(int status, int idShop, string lastSync)
        {
            using (MySqlConnection conn = new MySqlConnection(DiscountStringConnecting))
            {
                try 
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
                        Color.WriteLineColor("Shop [" + idShop + "] обновлен статус  " + Common.returnStatusText(status) + " и last_sync = " + lastSync, ConsoleColor.Yellow);
                        return true;
                    }
                    else
                    {
                        Color.WriteLineColor("Shop [" + idShop + "]  Не удалось обновить статус и last_sync", ConsoleColor.Red);
                        return false;
                    }
                }
                catch (Exception exc) 
                {
                    Color.WriteLineColor("[updateStatus]" + exc.Message, ConsoleColor.Red);
                    Log.Write(exc.Message, "[updateStatus]");
                    return false;
                }
            }
        }

        public static bool updateStatus(int status, int idShop, string lastSync, string lastTotalSync)
        {
            using (MySqlConnection conn = new MySqlConnection(DiscountStringConnecting))
            {
                try 
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
                        Color.WriteLineColor("Shop [" + idShop + "] обновлен статус  " + Common.returnStatusText(status) + " и last_Total_Sync = " + lastTotalSync, ConsoleColor.Yellow);
                        return true;
                    }
                    else
                    {
                        Color.WriteLineColor("Shop [" + idShop + "]  Не удалось обновить статус и last_Total_Sync", ConsoleColor.Red);
                        return false;
                    }
                }
                catch (Exception exc)
                {
                    Color.WriteLineColor("[updateStatus]" + exc.Message, ConsoleColor.Red);
                    Log.Write(exc.Message, "[updateStatus]");
                    return false;
                }
            }
        }

        //Проверка доступности кассового сервера с помощью пинга
        public static bool checkAvailability(String serverIp)
        {
            try 
            {
                Ping Pinger = new Ping();
                PingReply Reply = Pinger.Send(IPAddress.Parse(serverIp));

                return (Reply.Status == IPStatus.Success ? true : false);
            }
            catch(Exception exc)
            {
                Color.WriteLineColor("[checkAvailability]" + exc.Message, ConsoleColor.Red);
                Log.Write(exc.Message, "[checkAvailability]");
                return false;
            }
        }
    }
}
