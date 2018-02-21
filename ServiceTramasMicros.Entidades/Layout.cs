using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ServiceTramasMicros.Entidades
{
    public class Layout
    {
        public List<string> ltsTender { get; set; }
        public List<string> ltsPayment { get; set; }
        public List<string> ltsMenu { get; set; }
        public List<string> ltsDiscount { get; set; }
        public List<string> ltsServicesCharges { get; set; }
        public List<string> ltsImpuestos { get; set; }

        public string nombreArchivo { get; set; }
        public void IniciarListas()
        {
            ltsTender = new List<string>();
            ltsPayment = new List<string>();
            ltsMenu = new List<string>();
            ltsDiscount = new List<string>();
            ltsServicesCharges = new List<string>();
            ltsImpuestos = new List<string>();
        }
    }
}
