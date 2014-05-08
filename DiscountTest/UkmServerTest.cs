using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using DiscountSharp.tools;
using DiscountSharp.net;

namespace DiscountTest
{
    [TestClass]
    public class UkmServerTest
    {
        [TestMethod]
        public void CreateObjectUkmServer()
        {
            int idShop=41;
            string ipUkmServer="192.168.51.100";
            int portUkmServer=3306;
            string dbName="ukmserver";
            string lastTotalSync = "2014-05-06 23:47:41";
            string lastSync = "2014-05-08 12:41:26";
            int frequencyDump = 1;                          // Сутки
            int frequencyDailyDump = 1;                     // Час

            UkmServer ukmserver = new UkmServer(idShop, ipUkmServer, portUkmServer, dbName, lastTotalSync, lastSync, frequencyDump, frequencyDailyDump);
        }
    }
}
