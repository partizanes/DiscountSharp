using DiscountSharp.dump;
using DiscountSharp.tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace DiscountSharp.main
{
    class DiscountSharp
    {
        static void Main(string[] args)
        {
            Color.WriteLineColor("DiscountSharp", ConsoleColor.Magenta, " запущен", ConsoleColor.Green);

            ThreadManager tm = new ThreadManager();

            while(true)
            {
                tm.CheckCircle();

                Thread.Sleep(5000);

                Common.updateCardsToFivePercent();

                Thread.Sleep(600000);
            }
        }
    }
}
