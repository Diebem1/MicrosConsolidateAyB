using ServiceTramasMicros.Entidades;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ServiceTramasMicros
{
    public class ProcesaCadena
    {
        /// <summary>
        /// Método que sirve para quitar el hexadecimal del layout y splitea la cadena regresando un objeto
        /// Mensaje que contiene el encabezado, el cuerpo y trailer
        /// </summary>
        /// <param name="cadenaEntrada">Es la cadena que se recive de micros</param>
        /// <returns></returns>
        public Mensaje ProcesarCadena(string cadenaEntrada)
        {
            Mensaje msg = new Mensaje() { Header = "", Data = "", Trailer = "", Ping = "", Version = "" };
            if (cadenaEntrada != "")
            {
                try
                {
                    //obtenemos el HEADER y DATA
                    char[] separadores = { (char)CodeHaxadecimal.InicioTexto, (char)CodeHaxadecimal.FinDelTexto };
                    string[] seccionarMensaje = cadenaEntrada.Split(separadores);

                    // es un layout
                    msg.Header = seccionarMensaje[0];
                    msg.Data = seccionarMensaje[1];
                    msg.Trailer = seccionarMensaje[2];
                    #region Si es un ping
                    string elimnarCaracter = msg.Header.Replace((char)CodeHaxadecimal.InicioEncabezado, '¬').Trim('¬').Trim('\n').Trim('\t');
                    if (elimnarCaracter.Trim() == "0")
                    {
                        msg.Header.Replace(((char)CodeHaxadecimal.InicioTexto).ToString(), "");
                        msg.Ping = cadenaEntrada;
                    }
                    #endregion

                    #region Si es un pedido de versión
                    bool EsMsgVersion = CheckVersion(msg.Data);
                    if (EsMsgVersion == true)
                    {
                        msg.Version = "";
                    }
                    #endregion

                    return msg;
                }
                catch (Exception)
                {
                    msg.Header = "";
                    msg.Data = "";
                    msg.Trailer = "";
                    return msg;
                }
            }
            return null;
        }
        public bool CheckVersion(string datos)
        {
            string[] datosArray = datos.Split((char)CodeHaxadecimal.SepararArchivos);
            foreach (var item in datosArray)
            {
                string[] lineaArray = item.Split('|');
                if (lineaArray[0].ToString().Trim() == "V" || lineaArray[0].ToString().Trim() == "v")
                {
                    return true;
                }
            }
            return false;
        }
        /// <summary>
        /// Hace un split a la cadena eliminando el caracter de inicio de texto STX
        /// </summary>
        /// <param name="datos">Cuerpo del mensaje</param>
        /// <returns></returns>
        public string ProcesarDatos(string datos)
        {
            string _datos = "";
            if (datos != "")
            {
                _datos = datos.Replace(((char)CodeHaxadecimal.InicioTexto).ToString(), "¬")
                        .Replace(((char)CodeHaxadecimal.FinDelTexto).ToString(), "¬")
                        .Replace(((char)CodeHaxadecimal.SepararArchivos).ToString(), "¬");
                return _datos;
            }
            return _datos;
        }
        /// <summary>
        /// Regrersa un objeto Layout a partir de una trama con caracteres extraños
        /// </summary>
        /// <param name="datos">Es el cuerpo del mensaje que envía Micros</param>
        /// <returns></returns>
        public Layout GeneraLayout(string datos)
        {
            List<string> ltsLayout = new List<string>();
            if (datos != "")
            {
                ltsLayout = datos.Split('¬').ToList();
                Layout layout = new Layout();
                layout.IniciarListas();
                foreach (var item in ltsLayout)
                {
                    string[] elementoLayout = item.Split('|');
                    switch (elementoLayout[0])
                    {
                        case "T":
                            layout.ltsTender.Add(item);
                            layout.nombreArchivo = elementoLayout[1];
                            break;
                        case "P":
                            layout.ltsPayment.Add(item);
                            break;
                        case "M":
                            layout.ltsMenu.Add(item);
                            break;
                        case "D":
                            layout.ltsDiscount.Add(item);
                            break;
                        case "S":
                            layout.ltsServicesCharges.Add(item);
                            break;
                        case "I":
                            layout.ltsImpuestos.Add(item);
                            break;
                        default:
                            break;
                    }
                }
                return layout;
            }
            return null;
        }
        /// <summary>
        /// Regrersa un objeto Layout a partir de una trama limpia
        /// </summary>
        /// <param name="ruta">Ruta completa de archivo</param>
        /// <returns></returns>
        public Layout GeneraLayoutFromTramaLimpia(string ruta)
        {
            List<string> ltsLayout = new List<string>();
            Layout layout = new Layout();
            layout.IniciarListas();
            try
            {
                ltsLayout = System.IO.File.ReadAllLines(ruta).ToList();
                if (ltsLayout.Count == 0)
                    return null;
                foreach (var item in ltsLayout)
                {
                    string[] elementoLayout = item.Split('|');
                    switch (elementoLayout[0])
                    {
                        case "T":
                            layout.ltsTender.Add(item);
                            layout.nombreArchivo = elementoLayout[1];
                            break;
                        case "P":
                            layout.ltsPayment.Add(item);
                            break;
                        case "M":
                            layout.ltsMenu.Add(item);
                            break;
                        case "D":
                            layout.ltsDiscount.Add(item);
                            break;
                        case "S":
                            layout.ltsServicesCharges.Add(item);
                            break;
                        case "I":
                            layout.ltsImpuestos.Add(item);
                            break;
                        default:
                            break;
                    }
                }
                return layout;
            }
            catch (Exception ex)
            {
                throw new Exception("Error generación de objeto layout a partir de un archivo que se sospechaba era trama limpia. " + ex.Message);
            }
        }
    }
}
