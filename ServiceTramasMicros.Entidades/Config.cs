using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ServiceTramasMicros.Entidades
{
    public class Config
    {
        public string Puerto { get; set; }
        public string Folder { get; set; }
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
    }
}