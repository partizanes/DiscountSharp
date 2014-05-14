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

                Thread.Sleep(600000);
            }

            Console.ReadKey();
        }
    }
}
