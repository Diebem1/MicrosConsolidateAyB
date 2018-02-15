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
                cfn.Puerto = nodoConfig.Element("Puerto").Value;

                cfn.FolderRoot = nodoConfig.Element("Folder") != null && !String.IsNullOrEmpty(nodoConfig.Element("Folder").Value) ?
                        nodoConfig.Element("Folder").Value.Trim().TrimEnd('\\') + @"\" : "";

                cfn.MailNotificacion = nodoConfig.Element("MailNotificacion").Value;
                cfn.PasswordMailNotificacion = nodoConfig.Element("PasswordMailNotificacion").Value;
                cfn.HostMail = nodoConfig.Element("HostMail").Value;
                cfn.PuertoMail = nodoConfig.Element("PuertoMail").Value;
                cfn.TituloMail = nodoConfig.Element("TituloMail").Value;
                cfn.MailSoporte = nodoConfig.Element("MailSoporte").Value;
                cfn.Sucursal = nodoConfig.Element("Sucursal").Value;

                cfn.Version = nodoConfig.Element("Version") != null && !String.IsNullOrEmpty(nodoConfig.Element("Version").Value) ?
                        nodoConfig.Element("Version").Value.Trim() : "";

                cfn.Llave = nodoConfig.Element("Llave").Value;
                cfn.LocalIp = nodoConfig.Element("LocalIp").Value;

                cfn.TramasFolder = nodoConfig.Element("Tramas") != null && !String.IsNullOrEmpty(nodoConfig.Element("Tramas").Value) ?
                        nodoConfig.Element("Tramas").Value.Trim().TrimEnd('\\') + @"\" : "";

                cfn.TramasSuciasFolder = nodoConfig.Element("TramasSucias") != null && !String.IsNullOrEmpty(nodoConfig.Element("TramasSucias").Value) ?
                        nodoConfig.Element("TramasSucias").Value.Trim().TrimEnd('\\') + @"\" : "";

                cfn.TramasHistoricasFolder = nodoConfig.Element("TramasHistoricas") != null && !String.IsNullOrEmpty(nodoConfig.Element("TramasHistoricas").Value) ?
                        nodoConfig.Element("TramasHistoricas").Value.Trim().TrimEnd('\\') + @"\" : "";

                cfn.FactoVersion = nodoConfig.Element("FactoVersion") != null && !String.IsNullOrEmpty(nodoConfig.Element("FactoVersion").Value) ?
                        nodoConfig.Element("FactoVersion").Value : "";

                cfn.ClonarTramaFolder = nodoConfig.Element("ClonarTrama") != null && !String.IsNullOrEmpty(nodoConfig.Element("ClonarTrama").Value) ?
                        nodoConfig.Element("ClonarTrama").Value : "";

                cfn.ClonarTramaFolder = cfn.ClonarTramaFolder.Trim();

                if (!String.IsNullOrEmpty(cfn.ClonarTramaFolder))
                    cfn.ClonarTramaFolder = cfn.ClonarTramaFolder.Trim().TrimEnd('\\') + @"\";

                #region Valida Config (lo requerido)
                if (String.IsNullOrEmpty(cfn.Version))
                    throw new Exception("Es requerido campo 'Version' en configuración");

                if (String.IsNullOrEmpty(cfn.FolderRoot))
                    throw new Exception("Es requerido campo 'Folder' en configuración");

                if (String.IsNullOrEmpty(cfn.TramasFolder))
                    throw new Exception("Es requerido campo 'Tramas' en configuración");

                if (String.IsNullOrEmpty(cfn.TramasSuciasFolder))
                    throw new Exception("Es requerido campo 'TramasSucias' en configuración");

                if (String.IsNullOrEmpty(cfn.TramasHistoricasFolder))
                    throw new Exception("Es requerido campo 'TramasHistoricas' en configuración");

                if (String.IsNullOrEmpty(cfn.FactoVersion))
                    throw new Exception("Es requerido campo 'FactoVersion' en configuración");

                #region Validar que no se repitan las rutas
                List<string> joinFolders = new List<string> { cfn.TramasFolder, cfn.TramasSuciasFolder, cfn.TramasHistoricasFolder, cfn.FolderRoot };

                if (!String.IsNullOrEmpty(cfn.ClonarTramaFolder))
                    joinFolders.Add(cfn.ClonarTramaFolder);

                int carpetasComparar = joinFolders.Count();

                if (joinFolders.Distinct().Count() != carpetasComparar)
                    throw new Exception("Las rutas de carpetas para Folder, Tramas, ClonarTramas no pueden ser las mismas");
                #endregion

                string[] factoVersionList = new string[] { "VERSION_32", "VERSION_33", "DOCUMENTO_FISCAL", "MICROS_ROSEWOOD" };
                if (!factoVersionList.Contains(cfn.FactoVersion))
                    throw new Exception("El valor '" + cfn.FactoVersion + "' no es válido para campo FactoVersion");

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