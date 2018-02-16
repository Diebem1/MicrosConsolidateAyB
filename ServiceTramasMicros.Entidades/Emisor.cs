using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ServiceTramasMicros.Entidades
{
    public class Emisor
    {
        public Emisor()
        {
            this.rfc = "";
            this.mapeoIDsList = new Dictionary<string, string>();
        }
        public string rfc { get; set; }
        /// <summary>
        /// Listado de Identificadores de las tramas que vienen de Micros y su correspondiente numero de reemplazo.
        /// Los identificadores son unicos por Emisor (Key) mientras que los numeros de reemplazo pueden repetirse por Emisor
        /// </summary>
        public Dictionary<string, string> mapeoIDsList { get; set; }
    }
}
