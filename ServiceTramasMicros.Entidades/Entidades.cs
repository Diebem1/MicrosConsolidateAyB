using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace ServiceTramasMicros.Entidades
{
    public class Mensaje
    {
        public string Header { get; set; }
        public string Data { get; set; }
        public string Trailer { get; set; }
        public string Ping { get; set; }
        public string Version { get; set; }
    }
    public class Entidades
    {
        public List<Mensaje> ltsMensaje { get; set; }
    }
    /// <summary>
    /// InicioEncabezado, representa al caracter SOH
    /// InicioTexto, representa al caracter STX
    /// SepararArchivos, representa al caracter FS
    /// FinDelTexto, representa al caracter ETX
    /// FinDeLaTransmision, representa al caracter EOT
    /// </summary>
    public enum CodeHaxadecimal
    {
        InicioEncabezado = '\x01',
        InicioTexto = '\x02',
        SepararArchivos = '\x1C',
        FinDelTexto = '\x03',
        FinDeLaTransmision = '\x04'
    }
    /// <summary>
    /// HEADER, inicio del mensaje
    /// DATA, cuerpo del mensaje
    /// TRAILER, final del mensaje
    /// </summary>
    public enum ArquitecturaMensaje
    {
        [Description("HEADER")]
        HEADER = 1,
        [Description("DATA")]
        DATA = 2,
        [Description("TRAILER")]
        TRAILER = 3
    }
}
