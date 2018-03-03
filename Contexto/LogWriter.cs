using ServiceTramasMicros.Entidades;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;

namespace ServiceTramasMicros.Model
{
    public class LogWriter
    {
        private string fullFileNamePath = string.Empty;
        private string claveFacto = string.Empty;
        private string centroConsumo = string.Empty;
        private string nombreFile = string.Empty;
        //private string errorTry = string.Empty;
        //private string error = string.Empty;
        private string referencia_CI_CC = string.Empty;
        private bool allLogsEnabled = false;
        public enum EnumTipoError
        {
            Err = 0,
            ErrTry = 1,
            Warning = 3,
            Informative = 4,
            Important = 5
        }

        /// <summary>
        /// Constructor: Inicializa un nuevo archivo de Log en la ruta completa especificada
        /// </summary>
        /// <param name="logMessage">Mensaje inicial de creación</param>
        /// <param name="logPath">Ruta completa donde se guardará el log</param>
        public LogWriter(string logMessage, string logPath
                        , string claveFacto, string centroConsumo, string nombreFile
                        , string referencia_CI_CC)
        {
            fullFileNamePath = logPath;
            this.claveFacto = claveFacto;
            this.centroConsumo = centroConsumo;
            this.nombreFile = nombreFile;
            this.referencia_CI_CC = referencia_CI_CC;

            try
            {
                string configLogsEnabled = System.Configuration.ConfigurationSettings
                                           .AppSettings["AllLogsEnabled"].ToString();
                allLogsEnabled = (configLogsEnabled == "1" ? true : false);
            }
            catch (Exception)
            {
                allLogsEnabled = false;
            }

            LogWrite(logMessage, EnumTipoError.Informative);
        }
        /// <summary>
        /// Escribir una nueva entrada en el Log
        /// </summary>
        /// <param name="logMessage">Mensaje</param>
        public void LogWrite(string logMessage, EnumTipoError type
                            , string txtCadena = "", string xmlCadena = "", Layout layoutObjeto = null)
        {
            if (type == EnumTipoError.Informative && !this.allLogsEnabled)
                return;
            try
            {
                using (StreamWriter w = File.AppendText(fullFileNamePath))
                {
                    Log(logMessage, w, type);
                    if (type == EnumTipoError.Err || type == EnumTipoError.ErrTry)
                        SendLogToCloud(logMessage, type, txtCadena, xmlCadena, layoutObjeto);
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
        /// Agrega una nueva entrada al archivo log de la instancia actual
        /// </summary>
        /// <param name="logMessage">Mensaje</param>
        /// <param name="txtWriter">TextWriter para escribir en el archivo Log</param>
        private void Log(string logMessage, TextWriter txtWriter, EnumTipoError type)
        {
            try
            {
                txtWriter.Write("\r\nLog Entry : ");
                txtWriter.WriteLine("{0} {1}", DateTime.Now.ToLongTimeString(),
                                    DateTime.Now.ToLongDateString());
                txtWriter.WriteLine("Type    :{0}", type.ToString());
                txtWriter.WriteLine("Detail  :{0}", logMessage);
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
        /// <summary>
        /// Envia a un servicio Web un mensaje de Log
        /// </summary>
        private void SendLogToCloud(string errorMensaje, EnumTipoError type
                                    , string txtCadena = "", string xmlCadena = "", Layout layoutObjeto = null)
        {
            try
            {
                WSLogMicros.Service1Client logCliente = new WSLogMicros.Service1Client();
                WSLogMicros.EnviarLogTrama infoTramaWS = null;
                #region llenar Layout del WS si aplica
                if (layoutObjeto != null)
                {
                    infoTramaWS = new WSLogMicros.EnviarLogTrama();
                    infoTramaWS.ltsTender = layoutObjeto.ltsTender.ToArray();
                    infoTramaWS.ltsPayment = layoutObjeto.ltsPayment.ToArray();
                    infoTramaWS.ltsMenu = layoutObjeto.ltsMenu.ToArray();
                    infoTramaWS.ltsDiscount = layoutObjeto.ltsDiscount.ToArray();
                    infoTramaWS.ltsServicesCharges = layoutObjeto.ltsServicesCharges.ToArray();
                    infoTramaWS.ltsImpuestos = layoutObjeto.ltsImpuestos.ToArray();
                    infoTramaWS.nombreArchivo = layoutObjeto.nombreArchivo;
                }
                #endregion
                logCliente.InsertaLogTramaAsync(this.claveFacto, this.centroConsumo, this.nombreFile
                                    , (type == EnumTipoError.ErrTry ? errorMensaje : "")
                                    , (type != EnumTipoError.ErrTry ? errorMensaje : "")
                                    , DateTime.Now
                                    , this.referencia_CI_CC
                                    , txtCadena, xmlCadena, infoTramaWS);
            }
            catch (Exception ex)
            {
                Funciones.EscribeLog("Error al enviar Log a la nube"
                                     + "\nDetalle: " + ex.Message
                                     + "\nInner: " + (ex.InnerException != null ? ex.InnerException.Message : "")
                                     + "\nPila de llamadas: " + ex.StackTrace
                                     , System.Diagnostics.EventLogEntryType.Error);
            }
        }
        /// <summary>
        /// Envia información a la nube, de la trama
        /// </summary>
        /// <param name="fileNameTrama">Ruta completa del archivo</param>
        /// <param name="layout">Objeto Layout; si las tramas provienen de una versión reciente de Micros</param>
        /// <param name="xmlString">El texto del contenido del XML; si las tramas provienen de una versión Micros donde se requería un Doc. Fiscal de por medio para llegar a Facto</param>
        public void SendTramaToCloud(string fileNameTrama, Layout layout, string xmlString)
        {
            this.LogWrite("Se intentará enviar trama a la nube", EnumTipoError.Important);
            try
            {
                WSLogMicros.Service1Client clienteTramaWS = new WSLogMicros.Service1Client();
                WSLogMicros.EnviarLogTrama infoTramaWS = null;
                #region llenar Layout del WS si aplica
                if (layout != null)
                {
                    infoTramaWS = new WSLogMicros.EnviarLogTrama();
                    infoTramaWS.ltsTender = layout.ltsTender.ToArray();
                    infoTramaWS.ltsPayment = layout.ltsPayment.ToArray();
                    infoTramaWS.ltsMenu = layout.ltsMenu.ToArray();
                    infoTramaWS.ltsDiscount = layout.ltsDiscount.ToArray();
                    infoTramaWS.ltsServicesCharges = layout.ltsServicesCharges.ToArray();
                    infoTramaWS.ltsImpuestos = layout.ltsImpuestos.ToArray();
                    infoTramaWS.nombreArchivo = layout.nombreArchivo;
                }
                #endregion
                clienteTramaWS.InsertarTrama(this.claveFacto, this.centroConsumo, fileNameTrama, infoTramaWS, DateTime.Now, this.referencia_CI_CC, xmlString);
                this.LogWrite("Trama enviado a la nube", EnumTipoError.Important);
            }
            #region Control Excepciones especificas
            catch (TimeoutException exTime)
            {
                string mensajeException = "TimeOut con Servicio Tramas, método 'InsertarTrama'"
                                        + "; Detalle: " + exTime.Message;

                this.LogWrite("Warning en envío trama a la nube: " + mensajeException, LogWriter.EnumTipoError.ErrTry);
            }
            catch (Exception exGeneral)
            {
                string innerEx = exGeneral.InnerException != null ? "-" + exGeneral.InnerException.Message : "";
                string mensajeException = "Error general con Servicio Tramas, método 'InsertarTrama': " + exGeneral.Message + innerEx;
                this.LogWrite("Warning en envío trama a la nube: " + mensajeException, LogWriter.EnumTipoError.ErrTry);
            }
            #endregion
        }
        public static void SendPingToCloud(string claveFacto, string centroConsumo)
        {
            string macAddr = GetMACPC();
            try
            {
                WSLogMicros.Service1Client clientePingWS = new WSLogMicros.Service1Client();
                clientePingWS.InsertaPingAsync(macAddr + "_" + claveFacto + "_" + centroConsumo);
            }
            catch (Exception)
            {
            }
            return;
        }
        /// <summary>
        /// Intenta obtener una dirección de una interfaz de Red activa, diferente a Loopback, de la maquina donde se ejecuta la aplicación
        /// </summary>
        /// <returns>Regresa un string con la dirección fisica. Si ocurre un error o no se localiza una dirección, se regresa por default 'No_MAC'</returns>
        public static string GetMACPC()
        {
            string macAddr = "No_MAC";
            try
            {
                var MAC = (from nic in NetworkInterface.GetAllNetworkInterfaces()
                           where nic.OperationalStatus == OperationalStatus.Up
                           && nic.NetworkInterfaceType != NetworkInterfaceType.Loopback
                           select nic).FirstOrDefault();
                macAddr = MAC.GetPhysicalAddress().ToString();
            }
            catch (Exception)
            {
                macAddr = "No_MAC";
            }
            return macAddr;
        }
        ///// <summary>
        ///// Enviar las tramas erroneas a la nube
        ///// </summary>
        ///// <param name="error"></param>
        ///// <param name="fileNameTrama">nombre de archivo de trama (TXT)</param>
        ///// <param name="layout">Objeto Layout de la trama</param>
        ///// <param name="xmlString">Cadena de caracteres del XML de la trama (Doc. Fiscal)</param>
        ///// <param name="txtString">Cadena de caracteres de la trama</param>
        ///// <param name="factoVersion">Version de Facto utilizada</param>
        //public void SendErrorTramaToCloud(string error, string fileNameTrama, Layout layout, string xmlString, string txtString, string factoVersion)
        //{
        //    this.LogWrite("Se intentará enviar trama que marcó error a la nube", EnumTipoError.Important);
        //    try
        //    {
        //        WSLogMicros.Service1Client clienteTramaWS = new WSLogMicros.Service1Client();
        //        WSLogMicros.EnviarLogTrama infoTramaWS = null;
        //        #region llenar Layout del WS si aplica
        //        if (layout != null)
        //        {
        //            infoTramaWS = new WSLogMicros.EnviarLogTrama();
        //            infoTramaWS.ltsTender = layout.ltsTender.ToArray();
        //            infoTramaWS.ltsPayment = layout.ltsPayment.ToArray();
        //            infoTramaWS.ltsMenu = layout.ltsMenu.ToArray();
        //            infoTramaWS.ltsDiscount = layout.ltsDiscount.ToArray();
        //            infoTramaWS.ltsServicesCharges = layout.ltsServicesCharges.ToArray();
        //            infoTramaWS.ltsImpuestos = layout.ltsImpuestos.ToArray();
        //            infoTramaWS.nombreArchivo = layout.nombreArchivo;
        //        }
        //        #endregion
        //        //clienteTramaWS.InsertarTrama(this.claveFacto, this.centroConsumo, fileNameTrama, infoTramaWS
        //        //                            , DateTime.Now, this.referencia_CI_CC, xmlString
        //        //                            , txtString);
        //        this.LogWrite("Trama Erronea enviado a la nube", EnumTipoError.Important);
        //    }
        //    #region Control Excepciones especificas
        //    catch (TimeoutException exTime)
        //    {
        //        string mensajeException = "TimeOut con Servicio Tramas, método 'InsertarTramaError'"
        //                                + "; Detalle: " + exTime.Message;

        //        this.LogWrite("Warning en envío trama erronea a la nube: " + mensajeException, LogWriter.EnumTipoError.ErrTry);
        //    }
        //    catch (Exception exGeneral)
        //    {
        //        string innerEx = exGeneral.InnerException != null ? "-" + exGeneral.InnerException.Message : "";
        //        string mensajeException = "Error general con Servicio Tramas, método 'InsertarTramaError': " + exGeneral.Message + innerEx;
        //        this.LogWrite("Warning en envío trama erronea a la nube: " + mensajeException, LogWriter.EnumTipoError.ErrTry);
        //    }
        //    #endregion
        //}
    }
}