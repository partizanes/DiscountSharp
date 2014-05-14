using System;
using System.Data;
using System.Data.OleDb;

namespace DiscountSharp.dump
{
    class PcTradeServer
    {
        public void TestConnection()
        {
            string connectionString, sql;
            OleDbConnection conn;
            OleDbDataReader rdr;
            OleDbCommand cmd;
            connectionString =
            "Provider=Sybase ASE OLE DB Provider;Datasourcce=sydev;" + "User ID=tiraspr;Password=tiraspr";
            conn = new OleDbConnection(connectionString);
            conn.Open();

            sql = "Select * from user_tree_start";
            cmd = new OleDbCommand(sql, conn);
            cmd.CommandType = CommandType.Text;
            rdr = cmd.ExecuteReader();

            while (rdr.Read())
                Console.WriteLine(rdr["user_id"].ToString() + " " + rdr["tree_start"] + " " + rdr["strategy_group"]);

            Console.WriteLine("DONE");
            Console.Read();
        }
    }
}
