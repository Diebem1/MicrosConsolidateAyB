using System;
using System.Collections.Generic;
using System.Diagnostics;
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
                //Console.WriteLine("No se pudo escribir en el visor de eventos, se escribe aquí:\n"
                //    + "[" + sEvent + "]"
                //    + "{" + tipoEvento.ToString() + "}");
            }
        }
    }
}
