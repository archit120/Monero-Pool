using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MoneroPool
{
    public static class Logger
    {
        public enum LogLevel
        {
            Debug=3,Verbose=2,General=1,Special=0,Error=-1
        }

        public static LogLevel AppLogLevel = LogLevel.General;
        public static bool Colours = true;

        public static void Log( LogLevel logLevel, string format, params object[] args)
        {
            if (logLevel<=AppLogLevel)
            {
                if (Colours)
                {
                    switch (logLevel)
                    {
                        case LogLevel.Debug:
                            Console.ForegroundColor = ConsoleColor.DarkGray;
                            break;
                        case LogLevel.Verbose:
                            Console.ForegroundColor = ConsoleColor.Gray;
                            break;
                        case LogLevel.General:
                            Console.ForegroundColor = ConsoleColor.White;
                            break;
                        case LogLevel.Special:
                            Console.ForegroundColor = ConsoleColor.Green;
                            break;
                        case LogLevel.Error:
                            Console.ForegroundColor = ConsoleColor.Red;
                            break;
                    }
                }
                Console.WriteLine("[{0}] [Thread Count : {1}]" + string.Format(format, args), DateTime.Now, System.Diagnostics.Process.GetCurrentProcess().Threads.Count);     
            }
        }
    }
}
