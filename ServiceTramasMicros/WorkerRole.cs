using ServiceTramasMicros.Entidades;
using ServiceTramasMicros.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace ServiceTramasMicros
{
    public class WorkerRole
    {
        public ManualResetEvent _shutdownEvent = new ManualResetEvent(false);
        public Thread _thread;
        Config cfn = new Config();
        public void WorkerThreadFunc()
        {
            //Aquí va todo lo que se tiene que validar y configurar
            #region Incializar
            Contexto cxt = new Contexto();
            try
            {
                cfn = cxt.setConfig(cfn, AppDomain.CurrentDomain.BaseDirectory);
                IniciarRequerimientosMicros();
            }
            catch (Exception ex)
            {
                WorkerRole.EscribeLog("Error Archivo Emisor.xml: "
                    + ex.Message
                    , System.Diagnostics.EventLogEntryType.Error);
                return;
            }
            #endregion
            while (!_shutdownEvent.WaitOne(0))
            {
                //Aquí va todo lo que se va a Procesar
                Thread.Sleep(10000);
            }
        }
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
                Console.WriteLine("No se pudo escribir en el visor de eventos, se escribe aquí:\n"
                    + "[" + sEvent + "]"
                    + "{" + tipoEvento.ToString() + "}");
            }
        }
        private void IniciarRequerimientosMicros()
        {
            string path = cfn.Folder;
            try
            {
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
                if (!Directory.Exists(path + "Xml"))
                {
                    Directory.CreateDirectory(path + "Xml");
                }
                if (!Directory.Exists(path + "Tramas"))
                {
                    Directory.CreateDirectory(path + "Tramas");
                }
                if (!Directory.Exists(path + "Procesado"))
                {
                    Directory.CreateDirectory(path + "Procesado");
                }
                if (!Directory.Exists(path + "Duplicado"))
                {
                    Directory.CreateDirectory(path + "Duplicado");
                }
                if (!Directory.Exists(path + "Error"))
                {
                    Directory.CreateDirectory(path + "Error");
                }
                if (!Directory.Exists(path + "Logs"))
                {
                    Directory.CreateDirectory(path + "Logs");
                }
                if (!Directory.Exists(path + "NoFacturable"))
                {
                    Directory.CreateDirectory(path + "NoFacturable");
                }
                if (!Directory.Exists(path + "DefinirRVC"))
                {
                    Directory.CreateDirectory(path + "DefinirRVC");
                }
            }
            catch (Exception ex)
            {
                WorkerRole.EscribeLog("Error al crear carpetas de control: "
                            + ex.Message
                            , System.Diagnostics.EventLogEntryType.Error);
            }

        }
    }
}