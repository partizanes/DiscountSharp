using System;
using System.Runtime.InteropServices;
using System.Text;

namespace DiscountSharp.tools
{
    class Config
    {
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern uint GetPrivateProfileString(string lpAppName, string lpKeyName, string lpDefault, StringBuilder lpReturnedString, uint nSize, string lpFileName);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool WritePrivateProfileString(string lpAppName, string lpKeyName, string lpString, string lpFileName);

        public static void Set(string section, string key, string value)
        {
            try { WritePrivateProfileString(section, key, value, Environment.CurrentDirectory + "\\config.ini"); }
            catch (Exception exc) { Console.WriteLine("[WritePrivateProfileString]" + exc.Message); }
        }

        public static string GetParametr(string par)
        {
            StringBuilder buffer = new StringBuilder(100, 350);

            GetPrivateProfileString("SETTINGS", par, "null", buffer, 100, Environment.CurrentDirectory + "\\config.ini");

            if (buffer.Equals("null"))
            {
                Console.WriteLine("[GetPrivateProfileString] Внимание в конфигурационом файле не найден параметр " + par + "\n Для продолжения нажмите любую клавишу.");
                Console.ReadKey(true);
                return "";
            }

            return buffer.ToString();
        }
    }
}
