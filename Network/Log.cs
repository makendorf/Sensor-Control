using System;
using System.Drawing;
using System.Globalization;
//using System.Windows.Forms;

namespace Network
{
    public enum LogLevel
    {
        None = 0,
        Info,
        Success,
        Warning,
        Error
    }

    public class Log
    {
        public delegate void    LogDelegate(LogLevel Level, string Message);
        public static   event   LogDelegate OnLog;

        public static Color GetLevelColor(LogLevel level)
        {
            switch (level)
            {
                case LogLevel.None:
                    return Color.Black;
                case LogLevel.Info:
                    return Color.White;
                case LogLevel.Success:
                    return Color.Lime;
                case LogLevel.Warning:
                    return Color.Yellow;
                case LogLevel.Error:
                    return Color.Red;
            }
            return Color.Black;
        }

        private static string MakeLogMessage(string format)
        {
            DateTime dt = DateTime.Now;
            string Result;
            string timeformat = "T";
            CultureInfo culture = CultureInfo.CreateSpecificCulture("de-DE");
            Result = "[" + dt.ToString(timeformat, culture) + "] ";
            Result += format;
            return Result;
        }
       
        public static void Debug(string format, params object[] arg)
        {
            string Message = MakeLogMessage(format);

            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine(Message);
            Console.ForegroundColor = ConsoleColor.White;
            OnLog?.Invoke(LogLevel.Info, Message);
        }
        public static void OK(string format, params object[] arg)
        {
            string Message = String.Format(format, arg);

            Console.ForegroundColor = ConsoleColor.Green;
            Console.SetCursorPosition(Console.CursorLeft + 80, Console.CursorTop - 1);
            Console.WriteLine(Message);
            Console.ForegroundColor = ConsoleColor.White;
            OnLog?.Invoke(LogLevel.Info, Message);
        }
        public static void Info(string format, params object[] arg)
        {
            string Message = MakeLogMessage(format);

            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(Message);
            Console.ForegroundColor = ConsoleColor.White;
            OnLog?.Invoke(LogLevel.Info, Message);
        }

        public static void Warning(string format, params object[] arg)
        {
            string Message = MakeLogMessage(format);

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(Message);
            Console.ForegroundColor = ConsoleColor.White;
            OnLog?.Invoke(LogLevel.Warning, Message);

        }
        public static void Error(string format, params object[] arg)
        {
            string Message = MakeLogMessage(format);

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(Message);
            Console.ForegroundColor = ConsoleColor.White;
            OnLog?.Invoke(LogLevel.Error, Message);
        }

        public static void Success(string format, params object[] arg)
        {
            string Message = MakeLogMessage(format);

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(Message);
            Console.ForegroundColor = ConsoleColor.White;
            OnLog?.Invoke(LogLevel.Success, Message);
        }
    }
}
