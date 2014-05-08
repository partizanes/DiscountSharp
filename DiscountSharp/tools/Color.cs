using System;

namespace DiscountSharp.tools
{
    class Color
    {
        public static void WriteLineColor(string value, ConsoleColor color)
        {
            Console.ForegroundColor = color;

            Console.WriteLine(("[" + DateTime.Now.ToShortTimeString() + "] " + value).PadRight(Console.WindowWidth - 1));

            Console.ResetColor();

            Log.Write(value, "COLOR");
        }

        public static void WriteLineColor(string value, ConsoleColor color, string value2, ConsoleColor color2)
        {
            Console.ForegroundColor = color;

            Console.Write(("[" + DateTime.Now.ToShortTimeString() + "] " + value));

            Console.ForegroundColor = color2;

            Console.WriteLine(value2);

            Console.ResetColor();

            Log.Write(value + " " + value2, "COLOR");
        }
    }
}
