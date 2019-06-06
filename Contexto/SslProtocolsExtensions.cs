//Clase para soportar HTTPS 
//Docuemntacion de referencia
//https://support.microsoft.com/en-us/help/3154518/support-for-tls-system-default-versions-included-in-the-net-framework 
//
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Authentication;
using System.Text;

namespace System.Security.Authentication
{
    class SslProtocolsExtensions
    {
        public const SslProtocols Tls12 = (SslProtocols)0x00000C00;
        public const SslProtocols Tls11 = (SslProtocols)0x00000300;
    }
}
