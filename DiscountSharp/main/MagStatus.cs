using DiscountSharp.net;
using DiscountSharp.tools;
using MySql.Data.MySqlClient;
using System;
using System.Net;

namespace DiscountSharp.main
{
    class MagStatus
    {
        private int magId { get; set; }
        private string ipAddress { get; set; }
        private string port { get; set; }
        private DateTime? timeTotalSync { get; set; }
        private DateTime? lastSync { get; set; }
        private int codeStatus { get; set; }
        private int form { get; set; }
        private string comment { get; set; }

        public MagStatus(int magId, string ipAddress, int port, int codeStatus, int form, string comment)
        {
            this.magId = magId;
            this.ipAddress = ipAddress;
            this.timeTotalSync = timeTotalSync;
            this.lastSync = lastSync;
            this.codeStatus = codeStatus;
            this.form = form;
            this.comment = comment;

            switch(form)
            {
                case 1: //ukm
                    DumpTotalUkm();
                    break;
                case 2: //set
                    break;
            }
        }

        public void DumpTotalUkm()
        {
   
        }
    }
}
