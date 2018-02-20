using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ServiceTramasMicros.Entidades
{
    public class Config
    {
        public string Puerto { get; set; }
        /// <summary>
        /// Carpeta Raiz donde irán las demas carpetas de control; ya incluye al final del string la diagonal \
        /// </summary>
        public string FolderRoot { get; set; }
        public string MailNotificacion { get; set; }
        public string PasswordMailNotificacion { get; set; }
        public string HostMail { get; set; }
        public string PuertoMail { get; set; }
        public string TituloMail { get; set; }
        public string MailSoporte { get; set; }
        public string Llave { get; set; }
        public string Version { get; set; }
        public string Sucursal { get; set; }
        public string LocalIp { get; set; }
        /// <summary>
        /// Ruta completa de las tramas; ya incluye al final del string la diagonal \
        /// </summary>
        public string TramasFolder { get; set; }
        /// <summary>
        /// Ruta completa donde este cliente debe recuperar tramas sucias generadas por el Socket; ya incluye al final del string la diagonal \
        /// </summary>
        public string TramasSuciasFolder { get; set; }
        /// <summary>
        /// Ruta completa donde este cliente debe enviar las tramas sucias despues de haberlas ocupado; ya incluye al final del string la diagonal \
        /// </summary>
        public string TramasHistoricasFolder { get; set; }
        /// <summary>
        /// Valores posibles para FactoVersion[VERSION_32|VERSION_33|DOCUMENTO_FISCAL|MICROS_ROSEWOOD]
        /// </summary>
        public string FactoVersion { get; set; }
        /// <summary>
        /// Ruta completa a donde una trama deberá ser clonada; ya incluye al final del string la diagonal \
        /// </summary>
        public string ClonarTramaFolder { get; set; }
        /// <summary>
        /// Nombre de carpeta donde se generaría un XML con base a datos de la trama.
        /// Es de solo lectura y regresa la ruta completa con base al directorio configurado en Folder
        /// ; ya incluye al final del string la diagonal \
        /// </summary>
        public string XmlFolder { get { return this.FolderRoot + @"Xml\"; } }
        /// <summary>
        /// Nombre de carpeta donde se mueven las tramas procesadas correctamente por Facto
        /// Es de solo lectura y regresa la ruta completa con base al directorio configurado en Folder
        /// ; ya incluye al final del string la diagonal \
        /// </summary>
        public string ProcesadoFolder { get { return this.FolderRoot + @"Procesado\"; } }
        /// <summary>
        /// Nombre de carpeta donde se mueven las tramas duplicadas según indica Facto
        /// Es de solo lectura y regresa la ruta completa con base al directorio configurado en Folder
        /// ; ya incluye al final del string la diagonal \
        /// </summary>
        public string DuplicadoFolder { get { return this.FolderRoot + @"Duplicado\"; } }
        /// <summary>
        /// Nombre de carpeta donde se mueven las tramas que Facto marque con codigo 300 (error) y Descripcion no contemplada por este cliente
        /// Es de solo lectura y regresa la ruta completa con base al directorio configurado en Folder
        /// ; ya incluye al final del string la diagonal \
        /// </summary>
        public string ErrorFolder { get { return this.FolderRoot + @"Error\"; } }
        /// <summary>
        /// Nombre de carpeta donde se generan los Logs por trama, independientemente del código regresado por Facto
        /// Es de solo lectura y regresa la ruta completa con base al directorio configurado en Folder
        /// ; ya incluye al final del string la diagonal \
        /// </summary>
        public string LogsFolder { get { return this.FolderRoot + @"Logs\"; } }
        /// <summary>
        /// Nombre de carpeta donde mueven las tramas de tickets no facturables, normalmente por que Facto indica que están canceladas
        /// Es de solo lectura y regresa la ruta completa con base al directorio configurado en Folder
        /// ; ya incluye al final del string la diagonal \
        /// </summary>
        public string NoFacturableFolder { get { return this.FolderRoot + @"NoFacturable\"; } }
        /// <summary>
        /// Nombre de carpeta donde se mueven las tramas cuyo identificador no está definido en Facto
        /// Es de solo lectura y regresa la ruta completa con base al directorio configurado en Folder
        /// ; ya incluye al final del string la diagonal \
        /// </summary>
        public string DefinirRVCFolder { get { return this.FolderRoot + @"DefinirRVC\"; } }
        /// <summary>
        /// Clave facto, utilizado para WS de Logs; Es requerido
        /// </summary>
        public string ClaveFacto { get; set; }
        /// <summary>
        /// Centro de consumo, utilizado para WS de Logs; Es requerido
        /// </summary>
        public string CentroConsumo { get; set; }
    }
}