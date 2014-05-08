using DiscountSharp.tools;
using MySql.Data.MySqlClient;
using System;

namespace DiscountSharp.net
{
    public static class Connector
    {
        private static string ipDiscountServer = Config.GetParametr("ipDiscountServer");
        private static string portDiscountServer = Config.GetParametr("PortDiscountServer");
        private static string discountSharpDb = Config.GetParametr("discountBdName");
        public static int commandTimeout = int.Parse(Config.GetParametr("commandTimeout"));
        private static int connectTimeout = int.Parse(Config.GetParametr("connectTimeout"));
        public static string DiscountStringConnecting = string.Format("server={0};Port={1};uid={2};pwd={3};database={4};Connect Timeout="
            + connectTimeout + ";", ipDiscountServer, portDiscountServer, "partizanes", "***REMOVED***", discountSharpDb);
    }
}
