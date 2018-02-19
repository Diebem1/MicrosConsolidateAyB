using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ServiceTramasMicros.Model
{
    public class LogWriter
    {
        private string fullFileNamePath = string.Empty;
        /// <summary>
        /// Inicializa un nuevo archivo de Log en la ruta completa especificada
        /// </summary>
        /// <param name="logMessage">Mensaje inicial de creación</param>
        /// <param name="logPath">Ruta completa donde se guardará el log</param>
        public LogWriter(string logMessage, string logPath)
        {
            fullFileNamePath = logPath;
            LogWrite(logMessage);
        }
        /// <summary>
        /// Escribir una nueva entrada en el Log
        /// </summary>
        /// <param name="logMessage">Mensaje</param>
        public void LogWrite(string logMessage)
        {
            try
            {
                using (StreamWriter w = File.AppendText(fullFileNamePath))
                {
                    Log(logMessage, w);
                }
            }
            catch (Exception ex)
            {
                Funciones.EscribeLog("Error al escribir en el Log"
                                     + "\nDetalle: " + ex.Message
                                     + "\nInner: " + (ex.InnerException != null ? ex.InnerException.Message : "")
                                     + "\nPila de llamadas: " + ex.StackTrace
                                     , System.Diagnostics.EventLogEntryType.Error);
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="logMessage">Mensaje</param>
        /// <param name="txtWriter">TextWriter para escribir en el archivo Log</param>
        private void Log(string logMessage, TextWriter txtWriter)
        {
            try
            {
                txtWriter.Write("\r\nLog Entry : ");
                txtWriter.WriteLine("{0} {1}", DateTime.Now.ToLongTimeString(),
                                    DateTime.Now.ToLongDateString());
                txtWriter.WriteLine("  :");
                txtWriter.WriteLine("  :{0}", logMessage);
                txtWriter.WriteLine("-------------------------------");
            }
            catch (Exception ex)
            {
                Funciones.EscribeLog("Error al escribir en el Log"
                     + "\nDetalle: " + ex.Message
                     + "\nInner: " + (ex.InnerException != null ? ex.InnerException.Message : "")
                     + "\nPila de llamadas: " + ex.StackTrace
                     , System.Diagnostics.EventLogEntryType.Error);
            }
        }
    }
}