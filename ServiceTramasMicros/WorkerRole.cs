using ServiceTramasMicros.Entidades;
using ServiceTramasMicros.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace ServiceTramasMicros
{
    public class WorkerRole
    {
        public ManualResetEvent _shutdownEvent = new ManualResetEvent(false);
        public Thread _thread;
        static public Config cfn = new Config();
        static public List<Emisor> emisorCfnList = new List<Emisor>();
        public DateTime fechaUltimoPing = DateTime.Now;
        public int TimeSleep = 10;
        public void WorkerThreadFunc()
        {
            try
            {
                this.TimeSleep = Convert.ToInt32(System.Configuration.ConfigurationSettings
                                           .AppSettings["TimeSleep"].ToString()) * 1000;
            }
            catch (Exception)
            {
                this.TimeSleep = 10000;
            }
            while (!_shutdownEvent.WaitOne(0))
            {
                EnviarPing();
                //Aquí va todo lo que se tiene que validar y configurar para que el servicio siga su ejecución
                LogWriter logGeneralEvents = null;
                try
                {
                    string carpetaLogsService = AppDomain.CurrentDomain.BaseDirectory.TrimEnd('\\') + @"\Logs\";
                    if (!Directory.Exists(carpetaLogsService))
                        Directory.CreateDirectory(carpetaLogsService);
                    string carpetaLogGeneral = carpetaLogsService + "LogEvent" + DateTime.Now.ToString("yyyy-MM-dd") + "_log.txt";
                    logGeneralEvents = new LogWriter("Log de Servicio General", carpetaLogGeneral
                                                    , "NA", "NA", carpetaLogGeneral, "NA");
                }
                catch (Exception ex)
                {
                    string msgEx = "Error al iniciar ";
                    Funciones.EscribeLog(msgEx
                                                + ex.Message
                                                , System.Diagnostics.EventLogEntryType.Error);
                    throw new Exception(msgEx);
                }
                #region Incializar
                ConfigClass cxt = new ConfigClass();
                cfn = new Config();
                emisorCfnList = new List<Emisor>();
                try
                {
                    logGeneralEvents.LogWrite("INICIO: Leer configuración (Emisor.XML)", LogWriter.EnumTipoError.Informative);
                    cfn = cxt.setConfig(ref cfn, ref emisorCfnList, AppDomain.CurrentDomain.BaseDirectory);
                }
                catch (Exception ex)
                {
                    string msgEx = "Error archivo Emisor.xml\n"
                        + ex.Message;
                    Funciones.EscribeLog(msgEx
                        , System.Diagnostics.EventLogEntryType.Error);
                    //_shutdownEvent.Set();
                    //if (!_thread.Join(3000))
                    //{ // give the thread 3 seconds to stop
                    //    _thread.Abort();
                    //}
                    throw new Exception(msgEx);
                }
                #endregion
                #region Validar para el ServicioMicros
                try
                {
                    logGeneralEvents.LogWrite("Validación de carpetas de control: Tramas, Error, Procesado, etc.", LogWriter.EnumTipoError.Informative);
                    ValidaCreaCarpetasControl();
                }
                catch (Exception ex)
                {
                    string msgEx = "Error carpetas de control\n"
                                        + ex.Message;
                    Funciones.EscribeLog(msgEx
                                        , System.Diagnostics.EventLogEntryType.Error);
                    throw new Exception(msgEx);
                }

                #endregion
                #region Validar y crear Arbol de Carpetas de la fecha actual
                logGeneralEvents.LogWrite("Validar y crear Arbol de Carpetas de la fecha actual", LogWriter.EnumTipoError.Informative);
                string currentAnioMesDia = AnioMesDiaFolder();
                string fullPathTramasHistoricas = cfn.TramasHistoricasFolder + currentAnioMesDia;//Historicas                
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
                    Funciones.EscribeLog(msgEx
                                                + ex.Message
                                                , System.Diagnostics.EventLogEntryType.Error);
                    throw new Exception(msgEx);
                }
                #endregion
                //Log Iniciar
                string logDelDiaFile = fullPathLogs + "LogDelDia_log.txt";
                LogWriter logEspecificoDia = new LogWriter("Log de Servicio especifico para fecha " + currentAnioMesDia, logDelDiaFile
                                                            , cfn.ClaveFacto, cfn.CentroConsumo, logDelDiaFile, "NA");
                //Aquí va todo el proceso que el servicio estará realizando
                #region Leer Directorio Tramas Sucias
                DirectoryInfo tramasSuciasFolder = null;
                logEspecificoDia.LogWrite("Leer Directorio Tramas Sucias", LogWriter.EnumTipoError.Informative);
                try
                {
                    tramasSuciasFolder = new DirectoryInfo(cfn.TramasSuciasFolder);
                }
                catch (Exception ex)
                {
                    string msgEx = "Error al leer directorio tramas sucias: ";
                    logEspecificoDia.LogWrite(msgEx + ex.Message, LogWriter.EnumTipoError.ErrTry);
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
                    logEspecificoDia.LogWrite(msgEx + ex.Message, LogWriter.EnumTipoError.ErrTry);
                    throw new Exception(msgEx);
                }
                #endregion

                #region Primero: Procesar Tramas Sucias
                logEspecificoDia.LogWrite("Primero: Procesar Tramas Sucias", LogWriter.EnumTipoError.Informative);
                foreach (FileInfo tramaTargetFile in tramasSuciasList)
                {
                    #region Limpiar trama (Layout)
                    string cadenaTramaBruta = "";
                    string currentNombreArchivo = tramaTargetFile.Name;
                    string currentTramaHistoricaFullPath = fullPathTramasHistoricas + currentNombreArchivo;
                    string currentTramaDefinirRVCFullPath = fullPathDefinirRVC + currentNombreArchivo;
                    string currentTramaErrorFullPath = fullPathError + currentNombreArchivo;
                    //AQUI LOG Debe Crearse para la trama Actual
                    string currentTramaLogFullPath = fullPathLogs + Path.GetFileNameWithoutExtension(tramaTargetFile.FullName) + "_log.txt";
                    LogWriter logObjectTrama = new LogWriter("Se crea log para trama con nombre de archivo [" + currentNombreArchivo + "]", currentTramaLogFullPath
                                                            , cfn.ClaveFacto, cfn.CentroConsumo, currentNombreArchivo, "NA");

                    ProcesaCadena procesar = new ProcesaCadena();
                    Mensaje currentMensaje = null;
                    Layout currentLayout = null;
                    string currentCadena = "";

                    try
                    {
                        cadenaTramaBruta = File.ReadAllText(tramaTargetFile.FullName);
                        //AQUI LOG Insert Trama Sucia Leída Correctamente
                        logObjectTrama.LogWrite("Trama Sucia Leída Correctamente", LogWriter.EnumTipoError.Informative);
                    }
                    catch (Exception)
                    {
                        //Aqui log Insert Error al leer trama para convertir a String
                        logObjectTrama.LogWrite("Error al leer trama para convertir a String", LogWriter.EnumTipoError.ErrTry);
                        string fileNameFinal = MoverArchivo(tramaTargetFile.FullName, currentTramaErrorFullPath);
                        logObjectTrama.LogWrite("La trama sucia se movió a carpeta errores, en esta ruta[" + fileNameFinal + "]", LogWriter.EnumTipoError.ErrTry);
                        continue;
                    }
                    try
                    {
                        logObjectTrama.LogWrite("Inicia ProcesarCadena trama bruta", LogWriter.EnumTipoError.Informative);
                        currentMensaje = procesar.ProcesarCadena(cadenaTramaBruta.Trim());
                        //AQUI LOG Insert Trama Sucia Procesada y se convierte en objeto Mensaje
                        logObjectTrama.LogWrite("Trama Sucia Procesada y se convierte en objeto Mensaje", LogWriter.EnumTipoError.Informative);
                    }
                    catch (Exception)
                    {
                        //Aquí Log No fue posible ProcesarCadena TramaSucia para convertirla a Mensaje
                        logObjectTrama.LogWrite("Error, no fue posible Procesar Cadena de TramaSucia para convertirla a Mensaje", LogWriter.EnumTipoError.ErrTry);
                        //Funciones.EscribeLog("No fue posible ProcesarCadena TramaSucia para convertirla a Mensaje", EventLogEntryType.Error);
                        string fileNameFinal = MoverArchivo(tramaTargetFile.FullName, currentTramaErrorFullPath);
                        logObjectTrama.LogWrite("La trama sucia se movió a carpeta errores, en esta ruta[" + fileNameFinal + "]", LogWriter.EnumTipoError.ErrTry);
                        continue;
                    }
                    if (currentMensaje.Ping.Length > 0 || currentMensaje.Version.Length > 0)
                    {
                        //AQUI LOG Insert Trama se mueve por ser Ping o Version
                        logObjectTrama.LogWrite("Trama se mueve por ser Ping o Version", LogWriter.EnumTipoError.Informative);
                        //Funciones.EscribeLog("Mover porque no es un Ticket", EventLogEntryType.Warning);
                        string fileNameFinal = MoverArchivo(tramaTargetFile.FullName, currentTramaErrorFullPath);
                        logObjectTrama.LogWrite("La trama sucia se movió a carpeta errores, en esta ruta[" + fileNameFinal + "]", LogWriter.EnumTipoError.Informative);
                        continue;
                    }

                    try
                    {
                        logObjectTrama.LogWrite("Procesar datos para obtener objeto Layout", LogWriter.EnumTipoError.Informative);
                        currentCadena = procesar.ProcesarDatos(currentMensaje.Data);
                        if (currentCadena == "")//Pudo haber venido de una trama limpia
                        {
                            logObjectTrama.LogWrite("La cadena que entró al metodo fue vacía;"
                                                  + " es probable que la trama ya era una trama limpia:"
                                                  + " se procede a generar el Layout sin limpiar caracteres extraños", LogWriter.EnumTipoError.Informative);
                            currentLayout = procesar.GeneraLayoutFromTramaLimpia(tramaTargetFile.FullName);
                        }
                        else
                        {
                            logObjectTrama.LogWrite("Proceso generar Objecto Layout", LogWriter.EnumTipoError.Informative);
                            currentLayout = procesar.GeneraLayout(currentCadena);
                        }
                        //AQUI LOG INSERT Trama procesada sin problemas para ser Layout
                        logObjectTrama.LogWrite("Trama procesada sin problemas para ser Layout", LogWriter.EnumTipoError.Informative);
                    }
                    catch (Exception ex)
                    {
                        //Aquí Log Insert No fue posible ProcesarDatos o Generar Layout| Se incluye StackTrace
                        logObjectTrama.LogWrite("No fue posible Procesar Datos o Generar Layout. " + ex.Message, LogWriter.EnumTipoError.ErrTry);
                        string fileNameFinal = WorkerRole.MoverArchivo(tramaTargetFile.FullName, currentTramaErrorFullPath);
                        logObjectTrama.LogWrite("La trama sucia se movió a carpeta errores, en esta ruta[" + fileNameFinal + "]", LogWriter.EnumTipoError.ErrTry);
                        continue;
                    }
                    string idMicrosTrama = "";
                    string identificador = "";
                    Emisor emisorObject = null;
                    try
                    {
                        idMicrosTrama = currentLayout.ltsTender[0].Split('|')[5].Trim();//Id Micros en la trama
                        //Buscar el RFC de acuerdo al IdMicrosTrama->Reemplazo Archivo Config
                        logObjectTrama.LogWrite("Buscar el RFC de acuerdo al IdMicrosTrama->Reemplazo Archivo Config", LogWriter.EnumTipoError.Informative);
                        emisorObject = emisorCfnList.Where(x => x.mapeoIDsList.TryGetValue(idMicrosTrama, out identificador)).FirstOrDefault();

                        if (String.IsNullOrEmpty(identificador))
                        {
                            logObjectTrama.LogWrite("Error: " + "El id de la trama [" + idMicrosTrama + "]"
                                              + " no fue localizado en el archivo de configuración; el TXT Trama Limpia o XML no será generado", LogWriter.EnumTipoError.Err);
                            string fileNameFinal = MoverArchivo(tramaTargetFile.FullName, currentTramaDefinirRVCFullPath);//Se cambia la trama historica a DefinirRVC
                            logObjectTrama.LogWrite("La trama sucia se movió a carpeta DefinirRVC, en esta ruta[" + fileNameFinal + "]", LogWriter.EnumTipoError.Err);
                            continue;
                        }

                        string fullPathTramaLimpia = EscribirLayoutLimpio(currentLayout, emisorObject.rfc);
                        //AQUI LOG INSERT Trama Limpia Escrita con Nombre nombreTramaLimpia
                        logObjectTrama.LogWrite("Trama Limpia escrita en ruta [" + fullPathTramaLimpia + "]", LogWriter.EnumTipoError.Informative);
                        string fileNameFinal2 = MoverArchivo(tramaTargetFile.FullName, currentTramaHistoricaFullPath);
                        //AQUI LOG INSERT Trama Sucia se mueve a Historicos
                        logObjectTrama.LogWrite("Trama Sucia se mueve a Historicos [" + fileNameFinal2 + "]", LogWriter.EnumTipoError.Informative);
                    }
                    catch (Exception ex)
                    {
                        //Aquí Log Insert No fue posible Generar Layout Limpio o mover TramaSucia despues de haber creado el Layout limpio| Se incluye StackTrace
                        logObjectTrama.LogWrite("Error: No fue posible Generar Layout Limpio o mover TramaSucia despues de haber creado el Layout limpio;"
                                              + " Se incluye Detalle y StackTrace: "
                                                 + ex.Message + "-" + ex.StackTrace, LogWriter.EnumTipoError.ErrTry);
                        string fileNameFinal = WorkerRole.MoverArchivo(tramaTargetFile.FullName, currentTramaErrorFullPath);
                        continue;
                    }
                    #endregion
                    #region Generar Doc Fiscal si se requiere
                    if (cfn.FactoVersion == "DOCUMENTO_FISCAL")
                    {
                        logObjectTrama.LogWrite("La version facto indica: " + cfn.FactoVersion + ", se procede a crear XML", LogWriter.EnumTipoError.Informative);
                        ServiceTramasMicros.DocumentXml.DocumentoFiscalv11.documentoFiscal DocfiscalObject = null;
                        try
                        {
                            logObjectTrama.LogWrite("Poblar datos del XML Doc. Fiscal con base a objeto Layout, en memoria", LogWriter.EnumTipoError.Informative);
                            DocfiscalObject = LLenarXml(currentLayout);

                            if (DocfiscalObject != null)
                            {
                                DocfiscalObject.emisor.rfc = emisorObject.rfc;
                                DocfiscalObject.sucursal.numero = identificador;
                                logObjectTrama.LogWrite("Datos RFC["
                                                        + emisorObject.rfc + "] e Identificador["
                                                        + identificador + "] para sucursal, fueron asignados al XML Doc. Fiscal, en memoria", LogWriter.EnumTipoError.Informative);
                                logObjectTrama.LogWrite("Generando XML Doc. Fiscal", LogWriter.EnumTipoError.Informative);
                                string fileNameFinal = GenerarXml(DocfiscalObject);
                                logObjectTrama.LogWrite("XML Doc. Fiscal generado con ruta completa [" + fileNameFinal + "]", LogWriter.EnumTipoError.Informative);
                            }
                            else
                            {
                                throw new Exception("El XML no pudo ser generado con los datos de la trama");
                            }
                        }
                        catch (Exception ex)
                        {
                            //AQUI LOG INSERT Error al generar objecto DocFiscal con los datos de la trama; el XML no será generado
                            logObjectTrama.LogWrite("Error al generar objecto DocFiscal con los datos de la trama; el XML no será generado."
                                                     + "\nMás detalle: " + ex.Message, LogWriter.EnumTipoError.ErrTry);
                            string fileNameFinal = WorkerRole.MoverArchivo(tramaTargetFile.FullName, currentTramaErrorFullPath);
                            logObjectTrama.LogWrite("La trama sucia se movió a carpeta errores, en esta ruta[" + fileNameFinal + "]", LogWriter.EnumTipoError.ErrTry);
                            continue;
                        }
                    }
                    #endregion
                }
                #endregion
                logEspecificoDia.LogWrite("Numero de archivos Tramas Sucias iterados [" + tramasSuciasList.Count + "]", LogWriter.EnumTipoError.Informative);
                #region Leer Directorio Tramas Limpias
                logEspecificoDia.LogWrite("Leer directorio Tramas Limpias", LogWriter.EnumTipoError.Informative);
                DirectoryInfo archivosFolder = null;
                try
                {
                    if (cfn.FactoVersion == "DOCUMENTO_FISCAL")
                    {
                        logEspecificoDia.LogWrite("La version facto a utilizar es [" + cfn.FactoVersion + "]: Se establece lectura para XML", LogWriter.EnumTipoError.Informative);
                        archivosFolder = new DirectoryInfo(cfn.XmlFolder);
                    }
                    else
                    {
                        logEspecificoDia.LogWrite("La version facto a utilizar es [" + cfn.FactoVersion + "]: Se establece lectura para Tramas", LogWriter.EnumTipoError.Informative);
                        archivosFolder = new DirectoryInfo(cfn.TramasFolder);
                    }
                }
                catch (Exception ex)
                {
                    logEspecificoDia.LogWrite("Error al leer directorio tramas: " + ex.Message, LogWriter.EnumTipoError.ErrTry);
                    continue;
                }

                List<FileInfo> archivosList = null;
                try
                {
                    if (cfn.FactoVersion == "DOCUMENTO_FISCAL")
                    {
                        logEspecificoDia.LogWrite("La version facto a utilizar es [" + cfn.FactoVersion + "]: Se establece busqueda para archivos XML", LogWriter.EnumTipoError.Informative);
                        archivosList = archivosFolder.GetFiles("*.xml").ToList();
                    }
                    else
                    {
                        logEspecificoDia.LogWrite("La version facto a utilizar es [" + cfn.FactoVersion + "]: Se establece busqueda para archivos TXT", LogWriter.EnumTipoError.Informative);
                        archivosList = archivosFolder.GetFiles("*.txt").ToList();
                    }
                }
                catch (Exception ex)
                {
                    logEspecificoDia.LogWrite("Error al leer lista de tramas: " + ex.Message, LogWriter.EnumTipoError.ErrTry);
                    continue;
                }
                #endregion
                #region Segundo: Leer tramas Limpias para enviar a Facto
                foreach (FileInfo tramaTargetFile in archivosList)
                {
                    //AQUI LOG CREAR Para trama Actual
                    #region Carpetas a donde puede irse la trama
                    string currentNombreArchivo = tramaTargetFile.Name;
                    string currentNombreArchivoNoExtension = Path.GetFileNameWithoutExtension(tramaTargetFile.Name);
                    string currentTramaProcesado = fullPathProcesado + currentNombreArchivo;
                    string currentTramaDuplicado = fullPathDuplicado + currentNombreArchivo;
                    string currentTramaDefinirRVC = fullPathDefinirRVC + currentNombreArchivo;
                    string currentTramaNoFacturable = fullPathNoFacturable + currentNombreArchivo;
                    string currentTramaError = fullPathError + currentNombreArchivo;
                    string destinoTramaClon = "";
                    string origenTramaClon = "";

                    if (!String.IsNullOrEmpty(cfn.ClonarTramaFolder))
                    {
                        destinoTramaClon = cfn.ClonarTramaFolder + currentNombreArchivoNoExtension + ".txt";
                        origenTramaClon = cfn.TramasFolder + currentNombreArchivoNoExtension + ".txt";
                    }

                    #endregion
                    //AQUI LOG Debe Crearse para la trama Actual
                    string currentTramaLogFullPath = fullPathLogs + currentNombreArchivoNoExtension + "_log.txt";
                    string referenciaToCloud_CI_CC = referenciaGetReferencia(currentNombreArchivoNoExtension)
                                                   + "_" + cfn.ClaveFacto + "_" + cfn.CentroConsumo;
                    LogWriter logObjectTrama = new LogWriter("Inicializar Log archivo [" + currentNombreArchivo + "]", currentTramaLogFullPath
                                                            , cfn.ClaveFacto, cfn.CentroConsumo, currentNombreArchivo
                                                            , referenciaToCloud_CI_CC);
                    logObjectTrama.LogWrite("Se crea log para trama con nombre de archivo [" + currentNombreArchivo + "]"
                                           , LogWriter.EnumTipoError.Important);

                    Facto.endPointIntegracionResponse Respuesta = new Facto.endPointIntegracionResponse();
                    logObjectTrama.LogWrite("Se enviará a Facto", LogWriter.EnumTipoError.Important);
                    Respuesta = EnviarDatosFacto(tramaTargetFile.FullName, logObjectTrama);
                    string mensajeFacto = (Respuesta != null && Respuesta.mensaje != null ? Respuesta.mensaje : "");
                    if (Respuesta.codigo == 100)
                    {
                        logObjectTrama.LogWrite("La respuesta fue 100 Exito: " + mensajeFacto, LogWriter.EnumTipoError.Important);
                        #region Procesados
                        string fileNameFinal = MoverArchivo(tramaTargetFile.FullName, currentTramaProcesado, destinoTramaClon, logObjectTrama, origenTramaClon);
                        //AQUÍ LOG INSERT Trama Procesada con exito; Indicar el nombre con el que se guardó
                        logObjectTrama.LogWrite("Archivo Trama se mueve a ruta completa [" + fileNameFinal + "]", LogWriter.EnumTipoError.Important);
                        EnviarTramaANube(fileNameFinal, logObjectTrama);
                        continue;
                        #endregion
                    }
                    else if (Respuesta.codigo == 200)
                    {
                        #region Duplicados
                        logObjectTrama.LogWrite("La respuesta fue 200 Duplicado: " + mensajeFacto, LogWriter.EnumTipoError.Err);
                        string fileNameFinal = MoverArchivo(tramaTargetFile.FullName, currentTramaDuplicado, "", logObjectTrama);
                        //AQUÍ LOG INSERT Trama Duplicada; Indicar el nombre con el que se guardó
                        logObjectTrama.LogWrite("Archivo Trama se mueve a ruta completa [" + fileNameFinal + "]", LogWriter.EnumTipoError.Err);
                        continue;
                        #endregion
                    }
                    else if (Respuesta.codigo == 400)
                    {
                        #region Error comunicación con Facto
                        logObjectTrama.LogWrite("La respuesta fue 400 Fallo Comunicación Facto, la trama no será movida."
                                              + " Detalle: " + mensajeFacto, LogWriter.EnumTipoError.Err);
                        continue;
                        #endregion
                    }
                    else if (!String.IsNullOrEmpty(mensajeFacto))
                    {
                        logObjectTrama.LogWrite("La respuesta fue 300 Error: " + mensajeFacto, LogWriter.EnumTipoError.Err);
                        #region DefinirRVC
                        if (Respuesta.mensaje.ToUpper().Contains(("No se encontró un Identificador").ToUpper())
                            || Respuesta.mensaje.ToUpper().Contains(("RVC").ToUpper())
                            || Respuesta.mensaje.ToUpper().Contains(("RFC Emisor").ToUpper())
                            )
                        {
                            string fileNameFinal = MoverArchivo(tramaTargetFile.FullName, currentTramaDefinirRVC, "", logObjectTrama);
                            //AQUÍ LOG INSERT Trama DefinirRVC
                            logObjectTrama.LogWrite("DefinirRVC: Archivo Trama se mueve a ruta completa [" + fileNameFinal + "]", LogWriter.EnumTipoError.Err);
                            continue;
                        }
                        #endregion
                        #region NoFacturable
                        else if (Respuesta.mensaje.ToUpper().Contains(("Cancelad").ToUpper()))//Se omite la 'a' intencionalmente
                        {
                            string fileNameFinal = MoverArchivo(tramaTargetFile.FullName, currentTramaNoFacturable, "", logObjectTrama);
                            //AQUÍ LOG INSERT Trama No facturable
                            logObjectTrama.LogWrite("NoFacturable: Archivo Trama se mueve a ruta completa [" + fileNameFinal + "]", LogWriter.EnumTipoError.Err);
                            continue;
                        }
                        #endregion
                        #region Error
                        else
                        {
                            string fileNameFinal = MoverArchivo(tramaTargetFile.FullName, currentTramaError, "", logObjectTrama);
                            //AQUÍ LOG INSERT Error
                            logObjectTrama.LogWrite("Archivo Trama se mueve a ruta completa [" + fileNameFinal + "]", LogWriter.EnumTipoError.Err);
                            continue;
                        }
                        #endregion
                    }
                    #region Error
                    else
                    {
                        string fileNameFinal = MoverArchivo(tramaTargetFile.FullName, currentTramaError, "", logObjectTrama);
                        //AQUÍ LOG INSERT Error
                        logObjectTrama.LogWrite("Archivo Trama se mueve a ruta completa [" + fileNameFinal + "]", LogWriter.EnumTipoError.Err);
                        continue;
                    }
                    #endregion
                }
                #endregion
                if (cfn.FactoVersion == "DOCUMENTO_FISCAL")
                    logEspecificoDia.LogWrite("Numero de archivos XML iterados [" + archivosList.Count + "]", LogWriter.EnumTipoError.Informative);
                else
                    logEspecificoDia.LogWrite("Numero de archivos TXT iterados [" + archivosList.Count + "]", LogWriter.EnumTipoError.Informative);
                Thread.Sleep(TimeSleep);//Esperar antes de volver a iniciar
            }
        }
        /// <summary>
        /// Escribe en el Visor de eventos de Windows con un tipo de evento asociado para esta aplicación
        /// </summary>
        /// <param name="sEvent"></param>
        /// <param name="tipoEvento"></param>
        //public static void EscribeLog(string sEvent, EventLogEntryType tipoEvento)
        //{
        //    string sSource;
        //    string sLog;
        //    try
        //    {
        //        sSource = "ServiceTramasMicros";
        //        sLog = "Application";
        //        if (!EventLog.SourceExists(sSource))
        //            EventLog.CreateEventSource(sSource, sLog);
        //        EventLog.WriteEntry(sSource, sEvent, tipoEvento);
        //    }
        //    catch (Exception)
        //    {
        //        Console.WriteLine("No se pudo escribir en el visor de eventos, se escribe aquí:\n"
        //            + "[" + sEvent + "]"
        //            + "{" + tipoEvento.ToString() + "}");
        //    }
        //}
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
                Funciones.EscribeLog("Error al crear carpetas de control: "
                            + ex.Message
                            , System.Diagnostics.EventLogEntryType.Error);
            }
        }
        public string EscribirLayoutLimpio(Layout layout, string rfc)
        {
            string nombreTramaLimpia = "";
            try
            {
                string prefijo = rfc + "-";
                if (File.Exists(cfn.TramasFolder + prefijo + layout.nombreArchivo + ".txt"))
                {
                    prefijo += DateTime.Now.ToString("yyyyMMddHHmmssfff") + "_";
                }
                nombreTramaLimpia = cfn.TramasFolder + prefijo + layout.nombreArchivo + ".txt";

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
                    //AQUI LOG ERROR
                    throw new Exception("El objeto Layout fue Nulo");
                }
            }
            catch (Exception ex)
            {
                //AQUI LOG
                string bodyMail = "Se ha generado un error al construir la trama,  con fecha " +
                                   DateTime.Now.ToString() + @"Detalle del error" +
                                   @"El error es:" +
                                   ex.Message + @" stack" + ex.StackTrace;
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
        /// <param name="logInclude">Si se desea dejar log para el movimiento, se pasa un objeto de este tipo</param>
        /// <param name="sourceClon">Para plan B): Ruta completa de archivo dónde tomar la trama (TXT).
        ///  Para plan A): se toma la ruta final que tuvo la trama (TXT)</param>
        /// <returns>Regresa el nombre de archivo completo en destino</returns>
        public static string MoverArchivo(string origen, string destino, string reCopiar = "", LogWriter logInclude = null, string sourceClon = "")
        {
            try
            {
                string extension = destino.ToUpper()
                                          .Substring(destino.LastIndexOf("."), 3);
                if (File.Exists(destino))
                {
                    destino = destino.ToUpper()
                              .Replace(extension, "-" + DateTime.Now.ToString("yyyyMMddHHmmssfff") + extension);
                }

                if (logInclude != null)
                    logInclude.LogWrite("Se intentará mover archivo origen [" + origen + "]", LogWriter.EnumTipoError.Informative);

                File.Move(origen, destino);

                if (logInclude != null)
                    logInclude.LogWrite("El archivo se movió al destino [" + destino + "]", LogWriter.EnumTipoError.Informative);

                if (reCopiar != "" && sourceClon != "")
                {
                    if (logInclude != null)
                        logInclude.LogWrite("La configuración indica clonar la trama a ruta [" + reCopiar + "]", LogWriter.EnumTipoError.Important);

                    try
                    {
                        if (cfn.FactoVersion == "DOCUMENTO_FISCAL")
                            File.Copy(sourceClon, reCopiar);
                        else
                            File.Copy(destino, reCopiar);

                        if (logInclude != null)
                            logInclude.LogWrite("La trama fue clonada a ruta [" + reCopiar + "]", LogWriter.EnumTipoError.Informative);
                    }
                    catch (Exception ex)
                    {
                        if (logInclude != null)
                            logInclude.LogWrite("Error al intentar copiar el archivo:\n" + ex.Message
                                                , LogWriter.EnumTipoError.ErrTry);
                    }
                }
            }
            catch (Exception ex)
            {
                if (logInclude != null)
                    logInclude.LogWrite("Error al mover archivo desde "
                                        + "\n Origen [" + origen + "]"
                                        + "\n hasta destino [" + destino + "]"
                                        + "\n - " + ex.Message, LogWriter.EnumTipoError.ErrTry);
            }
            return destino;
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
        private string GenerarXml(DocumentXml.DocumentoFiscalv11.documentoFiscal documento)
        {
            string fileName = "";
            try
            {
                XmlSerializer xmlSerializer = new XmlSerializer(typeof(DocumentXml.DocumentoFiscalv11.documentoFiscal));
                StreamWriter objXmlFile;
                string prefijo;
                if (documento.emisor.rfc != "")
                {
                    prefijo = documento.emisor.rfc + "-";
                }
                else
                {
                    prefijo = "";
                }

                if (File.Exists(cfn.XmlFolder + prefijo + documento.numeroTransaccion + ".xml"))
                {
                    prefijo += DateTime.Now.ToString("yyyyMMddHHmmssfff") + "_";
                }
                fileName = cfn.XmlFolder + prefijo + documento.numeroTransaccion + ".xml";
                objXmlFile = new StreamWriter(fileName);
                xmlSerializer.Serialize(objXmlFile, documento);
                objXmlFile.Close();
                return fileName;
            }
            catch (Exception ex)
            {
                //AQUI LOG ERROR
                throw new Exception("Error al crear el archivo XML [" + ex.Message + "]");
            }
        }
        /// <summary>
        /// Enviar datos al WS
        /// </summary>
        /// <param name="nombreArchivo">Ruta completa del archivo</param>
        /// <returns></returns>
        public Facto.endPointIntegracionResponse EnviarDatosFacto(string nombreArchivo, LogWriter logObjectTrama)
        {
            Facto.endPointIntegracionResponse respuesta = new Facto.endPointIntegracionResponse();
            try
            {
                using (Facto.FactoEndPointsService servicio = new Facto.FactoEndPointsService())
                {
                    Facto.endPointIntegracionRequest request = new Facto.endPointIntegracionRequest();
                    Emisor em = null;
                    Facto.enumVersionCfdiIntegracion versionCfdi = (Facto.enumVersionCfdiIntegracion)System.Enum.Parse(typeof(Facto.enumVersionCfdiIntegracion), cfn.FactoVersion);

                    if (versionCfdi == Facto.enumVersionCfdiIntegracion.DOCUMENTO_FISCAL)
                    {
                        logObjectTrama.LogWrite("La versión facto configurada es [" + versionCfdi + "]; Se intentará recuperar datos del emisor a partir del XML", LogWriter.EnumTipoError.Important);
                        em = GetEmisorFromXMLTrama(nombreArchivo);
                    }
                    else
                    {
                        logObjectTrama.LogWrite("La versión facto configurada es [" + versionCfdi + "]; Se intentará recuperar datos del emisor a partir del TXT", LogWriter.EnumTipoError.Important);
                        em = GetEmisorFromTXTTrama(nombreArchivo);
                    }
                    logObjectTrama.LogWrite("Preparar Request", LogWriter.EnumTipoError.Important);
                    #region Información Emisor Facto
                    request.emisor = new Facto.emisor();
                    request.emisor.rfc = em.rfc;
                    #endregion
                    #region Información para Facto
                    request.informacionFacto = new Facto.informacionFacto();
                    request.informacionFacto.identificadorIntegracion = em.mapeoIDsList.FirstOrDefault().Value;
                    request.informacionFacto.foliosFacto = false;
                    request.informacionFacto.integracion = Facto.enumIntegracion.MICROS;
                    request.informacionFacto.integracionSpecified = true;
                    request.informacionFacto.versionCfdi = versionCfdi;
                    request.informacionFacto.versionCfdiSpecified = true;
                    #endregion
                    logObjectTrama.LogWrite("Convertir a Bytes archivo", LogWriter.EnumTipoError.Important);
                    request.file = ConvertFileToByteArray(nombreArchivo);
                    string requestString = "Identificador Integracion: " + request.informacionFacto.identificadorIntegracion
                                         + "; foliosFacto: false"
                                         + "; integracion: Facto.enumIntegracion.MICROS"
                                         + "; versionCfdi: " + versionCfdi;

                    logObjectTrama.LogWrite("Petición para WebService (request) será: " + requestString, LogWriter.EnumTipoError.Important);
                    respuesta = servicio.procesarIntegracion(request, cfn.Llave);
                    logObjectTrama.LogWrite("Respuesta recibida de Facto (response): "
                                          + "Codigo: " + respuesta.codigo
                                          + "; Mensaje: " + respuesta.mensaje
                                          , LogWriter.EnumTipoError.Important);
                }
                return respuesta;
            }
            #region Control Excepciones especificas
            catch (TimeoutException exTime)
            {
                string mensajeException = "TimeOut con Facto"
                                        + "; Detalle: " + exTime.Message;
                respuesta.codigo = 400;//Codigo de error ajeno a facto
                respuesta.mensaje = mensajeException;
                //logObjectTrama.LogWrite("Error: " + mensajeException, LogWriter.EnumTipoError.ErrTry);
                return respuesta;
            }
            catch (System.Net.WebException exNetWeb)
            {
                string netWebStatus = (exNetWeb.Status != null ? exNetWeb.Status.ToString() : "null");
                string netWebInner = (exNetWeb.InnerException != null ? exNetWeb.InnerException.Message : "null");
                string mensajeException = "Fallo de comunicación con Facto"
                                        + "; Detalle: " + exNetWeb.Message
                                        + "; InnerException: " + netWebInner
                                        + "; Status: " + netWebStatus;
                respuesta.codigo = 400;//Codigo de error ajeno a facto
                respuesta.mensaje = mensajeException;
                //logObjectTrama.LogWrite("Error: " + mensajeException, LogWriter.EnumTipoError.ErrTry);
                return respuesta;
            }
            catch (Exception exGeneral)
            {
                string innerEx = exGeneral.InnerException != null ? "-" + exGeneral.InnerException.Message : "";
                string mensajeException = "Error general envío a Facto: " + exGeneral.Message + innerEx;
                respuesta.codigo = 300;//Codigo de error general
                respuesta.mensaje = mensajeException;
                //logObjectTrama.LogWrite("Error: " + mensajeException, LogWriter.EnumTipoError.ErrTry);
                return respuesta;
            }
            #endregion
        }
        /// <summary>
        /// Convierte un archivo a Bytes
        /// </summary>
        /// <param name="fileName">Ruta completa del archivo</param>
        /// <returns></returns>
        private byte[] ConvertFileToByteArray(string fileName)
        {
            return File.ReadAllBytes(fileName);
            /*codigo viejo
            FileInfo fi = new FileInfo(fileName);
            FileStream fs = fi.OpenRead();
            long numByte = fs.Length;
            byte[] byteArrayFile = new byte[numByte - 1];

            string xx = Encoding.ASCII.GetString(byteArrayFile);
            if (numByte > 0)
            {
                fs.Read(byteArrayFile, 0, byteArrayFile.Length);
                fs.Close();
            }
            return byteArrayFile;
            */
        }
        /// <summary>
        /// Regresa objeto emisor con datos necesarios
        /// Importante: El XML en el Nodo 'sucursal' elemento 'numero' ya viene el ID de reemplazo; en el nodo 'emisor' elemento 'rfc' viene el RFC
        /// </summary>
        /// <param name="nombreArchivo"></param>
        /// <returns></returns>
        private Emisor GetEmisorFromXMLTrama(string nombreArchivo)
        {
            Emisor emisor = new Emisor();
            try
            {
                XDocument xmlDocFiscal = XDocument.Load(nombreArchivo);
                XNamespace currentNameSpace = xmlDocFiscal.Root.Name.Namespace;
                XElement emisorRfcNodo = xmlDocFiscal.Root.Elements(currentNameSpace + "emisor").Elements(currentNameSpace + "rfc").FirstOrDefault();
                XElement sucursalNumeroNodo = xmlDocFiscal.Root.Elements(currentNameSpace + "sucursal").Elements(currentNameSpace + "numero").FirstOrDefault();
                emisor.rfc = emisorRfcNodo.Value;
                emisor.mapeoIDsList.Add("X", sucursalNumeroNodo.Value);
            }
            catch (Exception ex)
            {
                string msgEx = ex.Message + (ex.InnerException != null ? "-" + ex.InnerException.Message : "");
                throw new Exception("No fue posible recuperar datos de emisor en método 'GetEmisorFromXMLTrama'; Detalles: " + msgEx);
            }
            return emisor;
        }
        /// <summary>
        /// Regresa objeto emisor con datos necesarios
        /// </summary>
        /// <param name="nombreArchivo"></param>
        /// <returns></returns>
        private Emisor GetEmisorFromTXTTrama(string nombreArchivo)
        {
            Emisor emisor = new Emisor();
            try
            {
                string idMicrosTrama = "";//IdMicros
                string identificador = "";//IdReemplazo
                //Primera linea de la trama
                string primerTrama = File.ReadAllLines(nombreArchivo)[0];
                if (primerTrama.StartsWith("T"))
                {
                    idMicrosTrama = primerTrama.Split('|')[5].Trim();//Id Micros en la trama
                    var emisorConfig = emisorCfnList.Where(x => x.mapeoIDsList.TryGetValue(idMicrosTrama, out identificador)).FirstOrDefault();
                    if (!String.IsNullOrEmpty(identificador))
                    {
                        emisor.rfc = emisorConfig.rfc;
                        emisor.mapeoIDsList.Add("X", identificador);
                    }
                }
            }
            catch (Exception ex)
            {
                string msgEx = ex.Message + (ex.InnerException != null ? "-" + ex.InnerException.Message : "");
                throw new Exception("No fue posible recuperar datos de emisor en método 'GetEmisorFromTXTTrama'; Detalles: " + msgEx);
            }
            return emisor;
        }
        /// <summary>
        /// Obtiene el numero de referencia del nombre de archivo.
        /// Puede ser de la forma 'RFC-REFERENCIA' de la cual sólo se tomará la parte de REFERENCIA,
        /// O bien puede ser de la forma 'RFC-yyyMMddHHmmssfff_REFERENCIA' de la cual sólo se tomará la parte de REFERENCIA
        /// </summary>
        /// <param name="nombreArchivo">nombre de archivo sin ruta de acceso</param>
        /// <returns>numero de referencia</returns>
        private string referenciaGetReferencia(string nombreArchivo)
        {
            string referencia = string.Empty;
            try
            {
                if (!string.IsNullOrEmpty(nombreArchivo))
                {
                    var split = nombreArchivo.Split('-');

                    if (split.Length > 1)
                    {
                        referencia = split[split.Length - 1].Split('_').Length > 1 ? split[split.Length - 1].Split('_')[split[split.Length - 1].Split('_').Length - 1] : split[1];
                    }
                    else
                    {
                        referencia = split[0].Split('_').Length > 1 ? split[0].Split('_')[split[0].Split('_').Length - 1] : split[0];
                    }
                }
            }
            catch (Exception)
            {
            }

            return referencia;
        }
        private void EnviarTramaANube(string fileName, LogWriter logTramaTarget)
        {
            #region Enviar trama a la nube
            try
            {
                if (cfn.FactoVersion == "DOCUMENTO_FISCAL")
                {
                    string xmlTargetString = File.ReadAllText(fileName);
                    logTramaTarget.LogWrite("Se enviará el contenido XML a la nube", LogWriter.EnumTipoError.Important);
                    logTramaTarget.SendTramaToCloud(fileName, null, xmlTargetString);
                }
                else
                {
                    ProcesaCadena procesar = new ProcesaCadena();
                    Layout layoutTarget = procesar.GeneraLayoutFromTramaLimpia(fileName);
                    logTramaTarget.LogWrite("Se enviará objeto Layout (trama) a la nube", LogWriter.EnumTipoError.Important);
                    logTramaTarget.SendTramaToCloud(fileName, layoutTarget, "");
                }
            }
            catch (Exception)
            {
                logTramaTarget.LogWrite("Error al preparar archivos para enviar trama a la nube con ruta [" + fileName + "]"
                                        , LogWriter.EnumTipoError.ErrTry);
            }
            #endregion
        }
        private void EnviarPing()
        {
            int minutosDiferencia = 5;
            try
            {
                minutosDiferencia = Convert.ToInt32(System.Configuration.ConfigurationSettings
                                           .AppSettings["PingToCloud"]);
            }
            catch (Exception)
            {
                minutosDiferencia = 5;
            }

            DateTime horaInicio = DateTime.Now;
            //Lleva más de cinco ( o N minutos) desde ultimo Ping
            if (fechaUltimoPing <= horaInicio.AddMinutes(-minutosDiferencia))
            {
                LogWriter.SendPingToCloud(cfn.ClaveFacto, cfn.CentroConsumo);
                fechaUltimoPing = DateTime.Now;
            }
        }
    }
}