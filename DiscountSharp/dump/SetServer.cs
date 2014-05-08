using DiscountSharp.net;
using DiscountSharp.tools;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading;

namespace DiscountSharp.dump
{
    class SetServer
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

            determineTheShopStatus();
        }

        private void determineTheShopStatus()
        {
            Connector.updateStatus(2, idShop);

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

            //checkAvailabilityDumpDC();
        }

        private static void CreateCommand(string queryString,string connectionString)
        {
            using (SqlConnection connection = new SqlConnection(
                       connectionString))
            {
                SqlCommand command = new SqlCommand(queryString, connection);
                command.Connection.Open();
                command.ExecuteNonQuery();
            }
        }
    }
}
