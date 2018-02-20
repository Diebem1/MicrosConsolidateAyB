﻿using ServiceTramasMicros.Entidades;
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
        private string claveFacto = string.Empty;
        private string centroConsumo = string.Empty;
        private string nombreFile = string.Empty;
        //private string errorTry = string.Empty;
        //private string error = string.Empty;
        private string referencia_CI_CC = string.Empty;
        public enum EnumTipoError
        {
            Err = 0,
            ErrTry = 1,
            Warning = 3,
            Informative = 4
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
            LogWrite(logMessage, EnumTipoError.Informative);
        }
        /// <summary>
        /// Escribir una nueva entrada en el Log
        /// </summary>
        /// <param name="logMessage">Mensaje</param>
        public void LogWrite(string logMessage, EnumTipoError type)
        {
            try
            {
                using (StreamWriter w = File.AppendText(fullFileNamePath))
                {
                    Log(logMessage, w, type);
                    if (type == EnumTipoError.Err || type == EnumTipoError.ErrTry)
                        SendLogToCloud(logMessage, type);
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
        private void SendLogToCloud(string errorMensaje, EnumTipoError type)
        {
            try
            {
                WSLogMicros.Service1Client logCliente = new WSLogMicros.Service1Client();
                logCliente.InsertarLog(this.claveFacto, this.centroConsumo, this.nombreFile
                                    , (type == EnumTipoError.ErrTry ? errorMensaje : "")
                                    , (type != EnumTipoError.ErrTry ? errorMensaje : "")
                                    , DateTime.Now
                                    , this.referencia_CI_CC);
            }
            catch (Exception)
            {
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
            this.LogWrite("Se intentará enviar trama a la nube", EnumTipoError.Informative);
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
    }
}