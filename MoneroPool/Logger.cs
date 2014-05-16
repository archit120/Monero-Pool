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
            Verbose=2,General=1,Error=0
        }

        public static LogLevel AppLogLevel = LogLevel.General;

        public static void Log( LogLevel logLevel, string format, params object[] args)
        {
            if (logLevel<=AppLogLevel)
            {
                Console.WriteLine("{0}|" + string.Format(format, args), DateTime.Now);     
            }
        }
    }
}
