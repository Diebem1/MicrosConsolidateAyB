using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace ServiceTramasMicros.Model
{
    public class Funciones
    {
        public static void EscribeLog(string sEvent, EventLogEntryType tipoEvento)
        {
            string sSource;
            string sLog;
            try
            {
                sSource = "ServiceTramasMicros";
                sLog = "Application";
                if (!EventLog.SourceExists(sSource))
                    EventLog.CreateEventSource(sSource, sLog);
                EventLog.WriteEntry(sSource, sEvent, tipoEvento);
            }
            catch (Exception)
            {
            }

            try
            {
                string carpetaLogs = AppDomain.CurrentDomain.BaseDirectory.TrimEnd('\\') + @"\Logs\";
                if (!Directory.Exists(carpetaLogs))
                    Directory.CreateDirectory(carpetaLogs);

                string carpetaLogEvent = carpetaLogs + "LogEvent" + DateTime.Now.ToString("yyyy-MM-dd") + "_log.txt";
                LogWriter logEventsObject = new LogWriter(sEvent, carpetaLogEvent
                                                        , "NA", "NA", carpetaLogEvent, "NA");
            }
            catch (Exception)
            {
            }
        }
    }
}
