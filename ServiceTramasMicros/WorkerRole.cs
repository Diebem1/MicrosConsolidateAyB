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
        static public Config cfn = new Config();
        public void WorkerThreadFunc()
        {
            //Aquí va todo lo que se tiene que validar y configurar para que el servicio siga su ejecución
            #region Incializar
            Contexto cxt = new Contexto();
            try
            {
                cfn = cxt.setConfig(cfn, AppDomain.CurrentDomain.BaseDirectory);
            }
            catch (Exception ex)
            {
                WorkerRole.EscribeLog("Error archivo Emisor.xml\n"
                    + ex.Message
                    , System.Diagnostics.EventLogEntryType.Error);
                return;
            }
            #endregion
            #region Validar para el ServicioMicros
            try
            {
                ValidaCreaCarpetasControl();
            }
            catch (Exception ex)
            {
                WorkerRole.EscribeLog("Error carpetas de control\n"
                                    + ex.Message
                                    , System.Diagnostics.EventLogEntryType.Error);
                return;
            }

            #endregion
            while (!_shutdownEvent.WaitOne(0))
            {
                //Aquí va todo lo que se va a Procesar
                #region Leer Directorio
                DirectoryInfo tramasFolder = null;
                try
                {
                    tramasFolder = new DirectoryInfo(cfn.TramasFolder);
                }
                catch (Exception ex)
                {
                    WorkerRole.EscribeLog("Error al leer directorio tramas: "
                                                + ex.Message
                                                , System.Diagnostics.EventLogEntryType.Error);
                }
                List<FileInfo> tramasList = null;
                try
                {
                    tramasList = tramasFolder.GetFiles("*.txt").ToList();
                }
                catch (Exception ex)
                {
                    WorkerRole.EscribeLog("Error al leer lista de tramas: "
                                                + ex.Message
                                                , System.Diagnostics.EventLogEntryType.Error);
                } 
                #endregion
                foreach (FileInfo tramaTargetFile in tramasList)
                {
                    #region Limpiar trama (Layout)
                    string cadenaTramaBruta = "";
                    ProcesaCadena procesar = new ProcesaCadena();
                    Mensaje currentMensaje = null;
                    Layout currentLayout = null;
                    string currentCadena = "";

                    cadenaTramaBruta = File.ReadAllText(tramaTargetFile.FullName);
                    currentMensaje = procesar.ProcesarCadena(cadenaTramaBruta.Trim());
                    currentCadena = procesar.ProcesarDatos(currentMensaje.Data);                    
                    currentLayout = procesar.GeneraLayout(currentCadena);
                    EscribirLayoutLimpio(currentLayout);
                    #endregion
                }
                Thread.Sleep(10000);//Esperar antes de volver a iniciar
            }
        }
        /// <summary>
        /// Escribe en el Visor de eventos de Windows con un tipo de evento asociado para esta aplicación
        /// </summary>
        /// <param name="sEvent"></param>
        /// <param name="tipoEvento"></param>
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
        /// <summary>
        /// Valida las carpetas que Micros utilizará en el proceso; si no existen las crea
        /// </summary>
        private void ValidaCreaCarpetasControl()
        {
            try
            {
                if (!Directory.Exists(cfn.FolderRoot))
                    Directory.CreateDirectory(cfn.FolderRoot);

                if (!Directory.Exists(cfn.XmlFolder))
                    Directory.CreateDirectory(cfn.XmlFolder);

                if (!Directory.Exists(cfn.TramasFolder))
                    Directory.CreateDirectory(cfn.TramasFolder);

                if (!Directory.Exists(cfn.ProcesadoFolder))
                    Directory.CreateDirectory(cfn.ProcesadoFolder);

                if (!Directory.Exists(cfn.DuplicadoFolder))
                    Directory.CreateDirectory(cfn.DuplicadoFolder);

                if (!Directory.Exists(cfn.ErrorFolder))
                    Directory.CreateDirectory(cfn.ErrorFolder);

                if (!Directory.Exists(cfn.LogsFolder))
                    Directory.CreateDirectory(cfn.LogsFolder);

                if (!Directory.Exists(cfn.NoFacturableFolder))
                    Directory.CreateDirectory(cfn.NoFacturableFolder);

                if (!Directory.Exists(cfn.DefinirRVCFolder))
                    Directory.CreateDirectory(cfn.DefinirRVCFolder);

                if (!string.IsNullOrEmpty(cfn.ClonarTramaFolder) && !Directory.Exists(cfn.ClonarTramaFolder))
                    Directory.CreateDirectory(cfn.ClonarTramaFolder);
            }
            catch (Exception ex)
            {
                WorkerRole.EscribeLog("Error al crear carpetas de control: "
                            + ex.Message
                            , System.Diagnostics.EventLogEntryType.Error);
            }
        }
        public void EscribirLayoutLimpio(Layout layout)
        {
            try
            {
                if (layout != null)
                {
                    StreamWriter archivoLog = new StreamWriter(cfn.TramasFolder + layout.nombreArchivo + ".txt");
                    layout.ltsTender.ForEach(x =>
                    {
                        archivoLog.WriteLine(x);
                        archivoLog.Flush();
                    });
                    layout.ltsPayment.ForEach(x =>
                    {
                        archivoLog.WriteLine(x);
                        archivoLog.Flush();
                    });
                    layout.ltsMenu.ForEach(x =>
                    {

                        archivoLog.WriteLine(x);
                        archivoLog.Flush();
                    });

                    layout.ltsDiscount.ForEach(x =>
                    {
                        archivoLog.WriteLine(x);
                        archivoLog.Flush();
                    });

                    layout.ltsServicesCharges.ForEach(x =>
                    {
                        archivoLog.WriteLine(x);
                        archivoLog.Flush();
                    });
                    layout.ltsImpuestos.ForEach(x =>
                    {
                        archivoLog.WriteLine(x);
                        archivoLog.Flush();
                    });
                    archivoLog.Close();
                }
            }
            catch (Exception ex)
            {
                string bodyMail = "Se ha generado un error al construir la trama,  con fecha " +
                                   DateTime.Now.ToString() + @"<br><h3>Detalle del error </h3>" +
                                   @"<br>El error es: <br>" +
                                   ex.Message + @"<br><h3> stack</h3><br>" + ex.StackTrace;
                WorkerRole.EscribeLog(bodyMail, EventLogEntryType.Error);
            }
        }
    }
}