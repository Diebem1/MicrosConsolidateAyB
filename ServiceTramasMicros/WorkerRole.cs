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
                _shutdownEvent.Set();
                if (!_thread.Join(3000))
                { // give the thread 3 seconds to stop
                    _thread.Abort();
                }

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
                //Aquí va todo el proceso que el servicio estará realizando
                #region Leer Directorio Tramas Sucias
                DirectoryInfo tramasSuciasFolder = null;
                try
                {
                    tramasSuciasFolder = new DirectoryInfo(cfn.TramasSuciasFolder);
                }
                catch (Exception ex)
                {
                    string msgEx = "Error al leer directorio tramas sucias: ";
                    WorkerRole.EscribeLog(msgEx
                                                + ex.Message
                                                , System.Diagnostics.EventLogEntryType.Error);
                    throw new Exception(msgEx);
                }
                List<FileInfo> tramasSuciasList = null;
                try
                {
                    tramasSuciasList = tramasSuciasFolder.GetFiles("*.txt").ToList();
                }
                catch (Exception ex)
                {
                    string msgEx = "Error al leer lista de tramas sucias: ";
                    WorkerRole.EscribeLog(msgEx
                                                + ex.Message
                                                , System.Diagnostics.EventLogEntryType.Error);
                    throw new Exception(msgEx);
                }
                #endregion
                #region Validar y crear Arbol de Carpetas de la fecha actual
                string currentAnioMesDia = AnioMesDiaFolder();
                string fullPathTramasHistoricas = cfn.TramasHistoricasFolder + currentAnioMesDia;//Historicas
                string fullPathXml = cfn.XmlFolder + currentAnioMesDia;//Xml
                string fullPathProcesado = cfn.ProcesadoFolder + currentAnioMesDia;//Procesado
                string fullPathDuplicado = cfn.DuplicadoFolder + currentAnioMesDia;//Duplicado
                string fullPathError = cfn.ErrorFolder + currentAnioMesDia;//Error
                string fullPathLogs = cfn.LogsFolder + currentAnioMesDia;//Logs
                string fullPathNoFacturable = cfn.NoFacturableFolder + currentAnioMesDia;//NoFacturable
                string fullPathDefinirRVC = cfn.DefinirRVCFolder + currentAnioMesDia;//DefinirRVC

                try
                {
                    if (!Directory.Exists(fullPathTramasHistoricas))//Historicas
                        Directory.CreateDirectory(fullPathTramasHistoricas);

                    if (!Directory.Exists(fullPathXml))//Xml
                        Directory.CreateDirectory(fullPathXml);

                    if (!Directory.Exists(fullPathProcesado))//Procesado
                        Directory.CreateDirectory(fullPathProcesado);

                    if (!Directory.Exists(fullPathDuplicado))//Duplicado
                        Directory.CreateDirectory(fullPathDuplicado);

                    if (!Directory.Exists(fullPathError))//Error
                        Directory.CreateDirectory(fullPathError);

                    if (!Directory.Exists(fullPathLogs))//Logs
                        Directory.CreateDirectory(fullPathLogs);

                    if (!Directory.Exists(fullPathNoFacturable))//NoFacturable
                        Directory.CreateDirectory(fullPathNoFacturable);

                    if (!Directory.Exists(fullPathDefinirRVC))//DefinirRVC
                        Directory.CreateDirectory(fullPathDefinirRVC);
                }
                catch (Exception ex)
                {
                    string msgEx = "Error al crear arbol de directorio para AnioMesDia actual [" + currentAnioMesDia + "]: ";
                    WorkerRole.EscribeLog(msgEx
                                                + ex.Message
                                                , System.Diagnostics.EventLogEntryType.Error);
                    throw new Exception(msgEx);
                }
                #endregion
                #region Primero Procesar Tramas Sucias
                foreach (FileInfo tramaTargetFile in tramasSuciasList)
                {
                    #region Limpiar trama (Layout)
                    //AQUI LOG Debe Crearse para la trama Actual
                    string cadenaTramaBruta = "";
                    string currentNombreArchivo = tramaTargetFile.Name;
                    string currentTramaHistoricaFullPath = fullPathTramasHistoricas + currentNombreArchivo;
                    ProcesaCadena procesar = new ProcesaCadena();
                    Mensaje currentMensaje = null;
                    Layout currentLayout = null;
                    string currentCadena = "";

                    try
                    {
                        cadenaTramaBruta = File.ReadAllText(tramaTargetFile.FullName);
                        //AQUI LOG Insert Trama Sucia Leída Correctamente
                    }
                    catch (Exception)
                    {
                        //Aqui log Insert Error al leer trama para convertir a String
                        string mensaje = WorkerRole.MoverArchivo(tramaTargetFile.FullName, currentTramaHistoricaFullPath);
                        continue;
                    }
                    try
                    {
                        currentMensaje = procesar.ProcesarCadena(cadenaTramaBruta.Trim());
                        //AQUI LOG Insert Trama Sucia Procesada y se convierte en objeto Mensaje
                    }
                    catch (Exception)
                    {
                        //Aquí Log No fue posible ProcesarCadena TramaSucia para convertirla a Mensaje
                        string mensaje = WorkerRole.MoverArchivo(tramaTargetFile.FullName, currentTramaHistoricaFullPath);
                        continue;
                    }
                    if (currentMensaje.Ping.Length > 0 || currentMensaje.Version.Length > 0)
                    {
                        //AQUI LOG Insert Trama se mueve por ser Ping o Version
                        //Mover porque no es un Ticket
                        WorkerRole.MoverArchivo(tramaTargetFile.FullName, currentTramaHistoricaFullPath);
                        continue;
                    }

                    try
                    {
                        currentCadena = procesar.ProcesarDatos(currentMensaje.Data);
                        currentLayout = procesar.GeneraLayout(currentCadena);
                        //AQUI LOG INSERT Trama procesada sin problemas para ser Layout 
                    }
                    catch (Exception)
                    {
                        //Aquí Log Insert No fue posible ProcesarDatos o Generar Layout| Se incluye StackTrace
                        string mensaje = WorkerRole.MoverArchivo(tramaTargetFile.FullName, currentTramaHistoricaFullPath);
                        continue;
                    }
                    try
                    {
                        string fullPathTramaLimpia = EscribirLayoutLimpio(currentLayout);
                        //AQUI LOG INSERT Trama Limpia Escrita con Nombre nombreTramaLimpia
                        WorkerRole.MoverArchivo(tramaTargetFile.FullName, currentTramaHistoricaFullPath);
                        //AQUI LOG INSERT Trama Sucia se mueve a Historicos                        
                    }
                    catch (Exception)
                    {
                        //Aquí Log Insert No fue posible Generar Layout Limpio o mover TramaSucia despues de haber creado el Layout limpio| Se incluye StackTrace
                        string mensaje = WorkerRole.MoverArchivo(tramaTargetFile.FullName, currentTramaHistoricaFullPath);
                        continue;
                    }
                    //General Doc Fiscal si se requiere
                    if (cfn.FactoVersion == "DOCUMENTO_FISCAL")
                    {
                        ServiceTramasMicros.DocumentXml.DocumentoFiscalv11.documentoFiscal docf = LLenarXml(currentLayout);
                        if (docf != null)
                        {
                            //Emisor emi = new Emisor();
                            //ConfigurarEmisor cf = new ConfigurarEmisor();
                            //emi = cf.GetEmisor(layout.ltsTender[0].Split('|')[5].Trim(), AppDomain.CurrentDomain.BaseDirectory + @"\Emisor.xml");
                            //string identificador = emi.identificador;
                            //docf.sucursal.numero = identificador;
                            //GenerarXml(docf);
                        }
                    }
                    #endregion
                }
                #endregion

                #region Leer Directorio Tramas Limpias
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

                if (!Directory.Exists(cfn.TramasSuciasFolder))
                    Directory.CreateDirectory(cfn.TramasSuciasFolder);

                if (!Directory.Exists(cfn.TramasHistoricasFolder))
                    Directory.CreateDirectory(cfn.TramasHistoricasFolder);

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
        public string EscribirLayoutLimpio(Layout layout)
        {
            string nombreTramaLimpia = cfn.TramasFolder + layout.nombreArchivo + ".txt";
            try
            {
                if (File.Exists(nombreTramaLimpia))
                {
                    nombreTramaLimpia = nombreTramaLimpia.ToUpper()
                              .Replace(".TXT", "-" + DateTime.Now.ToString("yyyyMMddHHmmssfff") + ".txt");
                }
                if (layout != null)
                {
                    StreamWriter archivoLog = new StreamWriter(nombreTramaLimpia);
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
                    return nombreTramaLimpia;
                }
                else
                {
                    throw new Exception("El objeto Layout fue Nulo");
                }
            }
            catch (Exception ex)
            {
                string bodyMail = "Se ha generado un error al construir la trama,  con fecha " +
                                   DateTime.Now.ToString() + @"<br><h3>Detalle del error </h3>" +
                                   @"<br>El error es: <br>" +
                                   ex.Message + @"<br><h3> stack</h3><br>" + ex.StackTrace;
                throw new Exception(bodyMail);
            }
        }
        /// <summary>
        /// Mueve un archivo de una ruta origen a un destino. El archivo al llegar al destino se puede copiar a otra carpeta
        /// Si el archivo destino ya existe, se agrega al nombre año mes dia segundos milisegundos
        /// </summary>
        /// <param name="origen">Ruta completa del archivo a mover</param>
        /// <param name="destino">Ruta completa del destino del archivo</param>
        /// <param name="reCopiar">Ruta completa del archivo donde será copiado después de moverlo al destino</param>
        /// <returns></returns>
        public static string MoverArchivo(string origen, string destino, string reCopiar = "")
        {
            string mensaje = "";
            try
            {
                string extension = destino.ToUpper()
                                          .Substring(destino.LastIndexOf("."), 3);
                if (File.Exists(destino))
                {
                    destino = destino.ToUpper()
                              .Replace(extension, DateTime.Now.ToString("yyyyMMddHHmmssfff") + extension);
                }

                File.Move(origen, destino);
                if (reCopiar != "")
                {
                    try
                    {
                        File.Copy(destino, reCopiar);
                    }
                    catch (Exception)
                    {
                    }
                }
            }
            catch (Exception ex)
            {
                mensaje = ("Error al mover archivo desde "
                                    + "\n Origen [" + origen + "]"
                                    + "\n hasta destino [" + destino + "]"
                                    + "\n - " + ex.Message);
            }
            return mensaje;
        }
        /// <summary>
        /// Devuelve un string sin diagonal inicial y con diagonal final con una estructura de carpetas por Año, Mes y Día con base a la fecha del sistema
        /// </summary>
        /// <returns></returns>
        public static string AnioMesDiaFolder()
        {
            return DateTime.Now.Year.ToString()
                    + @"\" + DateTime.Now.Month.ToString()
                    + @"\" + DateTime.Now.Day.ToString() + @"\";
        }
        private DocumentXml.DocumentoFiscalv11.documentoFiscal LLenarXml(Layout layout)
        {
            try
            {
                ReadElements elemets = new ReadElements();
                DocumentXml.DocumentoFiscalv11.documentoFiscal docFiscal = new DocumentXml.DocumentoFiscalv11.documentoFiscal();
                docFiscal = elemets.GenerarDocumento(layout);
                return docFiscal;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
    }
}