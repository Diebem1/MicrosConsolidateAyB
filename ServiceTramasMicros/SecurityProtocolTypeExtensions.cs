//Clase para soportar HTTPS 
//Docuemntacion de referencia
//https://support.microsoft.com/en-us/help/3154518/support-for-tls-system-default-versions-included-in-the-net-framework 
//
//

using System;

namespace System.Net
{
    using System.Security.Authentication;
   
    public static class SecurityProtocolTypeExtensions
    {
        public const SecurityProtocolType Tls12 = (SecurityProtocolType)SslProtocolsExtensions.Tls12;
        public const SecurityProtocolType Tls11 = (SecurityProtocolType)SslProtocolsExtensions.Tls11;
        public const SecurityProtocolType SystemDefault = (SecurityProtocolType)0;
    }
}