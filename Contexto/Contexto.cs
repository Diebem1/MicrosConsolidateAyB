using ServiceTramasMicros.Entidades;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace ServiceTramasMicros.Model
{
    public class Contexto
    {
        public Config setConfig(Config cfn, string path)
        {
            try
            {
                XDocument documentXml = XDocument.Load(path + @"\Emisor.xml");
                XElement nodoConfig = documentXml.Elements("Configuracion").Elements("config").FirstOrDefault();
                cfn.Puerto = nodoConfig.Element("Puerto").Value.Trim();
                cfn.FolderRoot = nodoConfig.Element("Folder").Value.Trim().TrimEnd('\\') + @"\";
                cfn.MailNotificacion = nodoConfig.Element("MailNotificacion").Value.Trim();
                cfn.PasswordMailNotificacion = nodoConfig.Element("PasswordMailNotificacion").Value.Trim();
                cfn.HostMail = nodoConfig.Element("HostMail").Value.Trim();
                cfn.PuertoMail = nodoConfig.Element("PuertoMail").Value.Trim();
                cfn.TituloMail = nodoConfig.Element("TituloMail").Value.Trim();
                cfn.MailSoporte = nodoConfig.Element("MailSoporte").Value.Trim();
                cfn.Sucursal = nodoConfig.Element("Sucursal").Value.Trim();
                cfn.Version = nodoConfig.Element("Version").Value.Trim();
                cfn.Llave = nodoConfig.Element("Llave").Value.Trim();
                cfn.LocalIp = nodoConfig.Element("LocalIp").Value.Trim();
                cfn.TramasFolder = nodoConfig.Element("Tramas").Value.Trim().TrimEnd('\\') + @"\";
                cfn.FactoVersion = nodoConfig.Element("FactoVersion").Value.Trim();
                cfn.ClonarTramaFolder = nodoConfig.Element("ClonarTrama").Value.Trim();
                if (!String.IsNullOrEmpty(cfn.ClonarTramaFolder))
                {
                    cfn.ClonarTramaFolder = cfn.ClonarTramaFolder.Trim().TrimEnd('\\') + @"\";
                }
                #region Valida Config (lo requerido)

                if (String.IsNullOrEmpty(cfn.FolderRoot))
                {
                    throw new Exception("Es requerido campo 'Folder' en configuración");
                }

                if (String.IsNullOrEmpty(cfn.FactoVersion))
                {
                    throw new Exception("Es requerido campo 'FactoVersion' en configuración");
                }

                string[] factoVersionList = new string[] { "VERSION_32", "VERSION_33", "DOCUMENTO_FISCAL", "MICROS_ROSEWOOD" };
                if (!factoVersionList.Contains(cfn.FactoVersion))
                {
                    throw new Exception("El valor '" + cfn.FactoVersion + "' no es válido para campo FactoVersion");
                }
                #endregion
                return cfn;
            }
            catch (Exception ex)
            {
                string MSG = ex.Message
                    + (ex.InnerException != null ? " - " + ex.InnerException.Message : "");
                throw new Exception("Error al cargar configuración: " + MSG);
            }
        }
        /*
        public Config updateConfig(Config cf, string path)
        {

            System.IO.File.Delete(path + "..\\cnf.xml");
            // Objeto utilizado para almacenar el resultado
            MemoryStream stringwriter = new MemoryStream();
            //Inicializa el serializador con el tipo Notificación
            XmlSerializer x = new XmlSerializer(cf.GetType());
            //Convierte a XML y lo almacena en un StringWriter
            x.Serialize(stringwriter, cf);
            System.IO.File.WriteAllBytes(path + "\\cnf.xml", stringwriter.ToArray());
            return cf;
        }
        */
        private static T DeserializeXMLFileToObject<T>(Stream xmlStream)
        {
            T returnObject = default(T);
            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(T));
                returnObject = (T)serializer.Deserialize(xmlStream);
            }
            catch (Exception ex)
            {
                var y = ex.Message;
            }
            return returnObject;
        }
    }
}