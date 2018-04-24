using ServiceTramasMicros.DocumentXml.DocumentoFiscalv11;
using ServiceTramasMicros.Entidades;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ServiceTramasMicros
{
    public class ReadElements
    {
        bool valido = false;
        decimal totalTicket = 0m;
        decimal totalImpuesto = 0m;
        decimal sumaImpuestos = 0m;

        public documentoFiscal GenerarDocumento(Layout layout)
        {
            try
            {
                if (layout.ltsPayment.Count() == 1)
                {
                    // no hacemos nada
                }
                else if (layout.ltsPayment.Count() == 2) // obtenemos el de mayor monto
                {
                    List<string> pagoAmandar = new List<string>();
                    decimal monto1 = Convert.ToDecimal(layout.ltsPayment[0].Split('|')[10]);
                    decimal monto2 = Convert.ToDecimal(layout.ltsPayment[1].Split('|')[10]);

                    if (monto1 >= 0 && monto2 >= 0)
                    {
                        string amount = "";
                        if (monto1 > monto2)//seleccionamos el primerpago
                        {
                            amount = monto1.ToString();
                        }
                        else if (monto2 > monto1) //seleccionamos el segundo pago
                        {
                            amount = monto2.ToString();
                        }
                        else if (monto1 == monto2)
                        {
                            amount = monto1.ToString();
                        }
                        layout.ltsPayment = layout.ltsPayment.Where(x => x.Split('|')[10] == amount).ToList();
                    }

                }
                else if (layout.ltsPayment.Count() > 2)
                {

                    //foreach (var item in layout.ltsPayment)
                    //{

                    //}
                    List<decimal> ltsMontos = new List<decimal>();

                    layout.ltsPayment.ForEach(x =>
                    {
                        decimal monto = Convert.ToDecimal(x.Split('|')[10]);
                        ltsMontos.Add(monto);
                    });

                    List<decimal> Ordenar = (from val in ltsMontos.ToArray()
                                             orderby val descending
                                             select val).ToList();
                    string valorAtomar = Ordenar[0].ToString();
                    string newListPago = layout.ltsPayment.Where(x => x.Split('|')[10] == valorAtomar).FirstOrDefault();
                    layout.ltsPayment.Clear();
                    layout.ltsPayment.Add(newListPago);
                }

                documentoFiscal docFiscal = new documentoFiscal();
                List<string> llaves;
                List<string> valores;
                List<string> rfcs;
                List<string> rfcsValores;
                List<string> llavevalores;
                decimal servicios = 0m;
                decimal sservicio = 0m;
                int indice;
                int indice2;
                string rfcSeleccionado;
                List<string> cargoHabitacion;
                rfcSeleccionado = "";
                valido = true;
                docFiscal.sistemaEmisor = "FTO";
                docFiscal.numeroTransaccion = layout.ltsTender[0].Split('|')[1];
                docFiscal.identificador = layout.ltsTender[0].Split('|')[5];//Identificador de sucursal
                if (layout.ltsTender[0].Split('|')[14].Trim() == "RoomChg")
                {
                    docFiscal.estatus = "NF";
                }
                else
                {
                    docFiscal.estatus = "SF";
                }
                //asignado sucursal
                //               
                sucursal sucursal = new sucursal();
                sucursal = AgregarSucursal(layout.ltsTender[0].Split('|')[5]);
                //sea grega la sucursal validar contra el xml de emisro 
                //posponer 
                docFiscal.sucursal = sucursal;
                docFiscal.cliente = new cliente();
                docFiscal.cliente.cheque = layout.ltsTender[0].Split('|')[3];
                //agregando emisor
                docFiscal.emisor = new emisor();
                docFiscal.emisor = AgregarEmisor();
                docFiscal.emisor.rfc = rfcSeleccionado;
                //agregando el emisor 
                docFiscal.receptor = new receptor();
                docFiscal.receptor = AgregarReceptor();
                docFiscal.nombreArchivo = "";
                docFiscal.rutaArchivado = "";
                docFiscal.transaccionReferenciada = layout.ltsTender[0].Split('|')[2];
                //agregando los productos
                docFiscal.producto = AgregarProducto(layout);
                docFiscal.agrupadoConceptos = AgregarTasaImpuestoTrasladado(layout);
                //agregando Datos x
                docFiscal.descripcion = "";
                docFiscal.tipoDeElaboracion = enumElaboracion.AUTOMATICA;
                docFiscal.nombreCorte = new nombreCorte();
                docFiscal.paisExpedidoEn = "";
                docFiscal.tipoDeCambio = 0;
                //agregando detalle uso
                docFiscal.agrupadoDetalleUso = agregarDetalleUso();
                //agregar envío
                docFiscal.envio = AgregarEnvio();
                //agregando fecha de procesado
                docFiscal.fechasProceso = AgregarFecha(layout);
                //agregando gravables
                docFiscal.propina = 0;
                //agregar usurio presidente
                docFiscal.usuario = AgregaIdentificadorPresidente();
                //agregando plantilla
                docFiscal.plantilla = AgregaPlantilla();
                docFiscal.idPlantilla = 1;
                docFiscal.idPlantillaSpecified = true;
                // agregar prestador 
                docFiscal.prestadorServicioCFD = AgregaPrestador();
                //agregar serie
                docFiscal.serie = "";
                docFiscal.tipoDeComprobante = enumTipoDeComprobante.INGRESO;
                //agregando text
                docFiscal.agregadoTextos = AgregaTexto();
                docFiscal.agregadoImagenes = AgregaImagen();
                // agregando idioma
                docFiscal.idioma = "ES";
                docFiscal.agrupadoReferencias = AgregarReferencias();
                //agregar documento referenciado
                docFiscal.docReferenciado = AgregardocRef();
                //agregando totales
                docFiscal.subtotal = Convert.ToDecimal(layout.ltsTender[0].Split('|')[10]);
                docFiscal.total = Convert.ToDecimal(layout.ltsTender[0].Split('|')[12]);
                docFiscal.moneda = "MXP";
                docFiscal.formaDePago = "PAGO EN UNA SOLA EXHIBICION";
                docFiscal.descuentos = 0;
                docFiscal.impuestos = 0;
                docFiscal.totalTexto = "";
                // agregando usuarios
                docFiscal.usuario = AgregaUsuario();

                if (layout.ltsServicesCharges.Count() > 0)
                {
                    List<string> serv = new List<string>();
                    foreach (var item in layout.ltsServicesCharges)
                    {
                        string[] arrayServ = item.Split('|');
                        servicios = servicios + Convert.ToDecimal(arrayServ[12]);
                        sservicio = sservicio + Convert.ToDecimal(arrayServ[10]);
                    }
                    List<noGravable> ltsNoGravable = new List<noGravable>();
                    noGravable propina = new noGravable();
                    propina.importe = servicios;
                    ltsNoGravable.Add(propina);
                    totalTicket = totalTicket + sservicio;
                    docFiscal.noGravable = ltsNoGravable.ToArray();

                }

                if (layout.ltsDiscount.Count() > 0)
                {
                    string d = "";
                    List<string> ds = new List<string>();
                    decimal totaldescuento = 0m;
                    decimal descuentosNetos = 0;
                    decimal descuentosIVa = 0m;

                    foreach (var item in layout.ltsDiscount)
                    {
                        string[] arrayDescount = item.Split('|');
                        if (arrayDescount[10].IndexOf("-") > 0)
                        {
                            descuentosNetos = descuentosNetos + Convert.ToDecimal(arrayDescount[10].Replace("-", ""));
                        }
                        else
                        {
                            descuentosNetos = descuentosNetos + Convert.ToDecimal(arrayDescount[10].Replace("-", ""));
                        }

                        if (arrayDescount[11].IndexOf("-") > 0)
                        {
                            descuentosIVa = descuentosIVa + Convert.ToDecimal(arrayDescount[11].Replace("-", ""));
                        }
                        else
                        {
                            descuentosIVa = descuentosIVa + Convert.ToDecimal(arrayDescount[11].Replace("-", ""));
                        }
                        if (arrayDescount[12].IndexOf("-") > 0)
                        {
                            totaldescuento = totaldescuento + Convert.ToDecimal(arrayDescount[12].Replace("-", ""));
                        }
                        else
                        {
                            totaldescuento = totaldescuento + Convert.ToDecimal(arrayDescount[12].Replace("-", ""));
                        }
                    }

                    docFiscal.descuentos = totaldescuento;
                    List<desgloseDescuento> desgloses = new List<desgloseDescuento>();
                    desgloseDescuento desglose = new desgloseDescuento();
                    desglose.descuentoBruto = totaldescuento;
                    desglose.descuentoNeto = descuentosNetos;
                    desglose.descuentoImpuestos = descuentosIVa;
                    totalTicket = totalTicket - descuentosNetos;
                    totalImpuesto = totalImpuesto - descuentosIVa;
                    desgloses.Add(desglose);
                    docFiscal.desgloseDescuento = desgloses.ToArray();
                }

                if (layout.ltsPayment.Count() > 0)
                {
                    string sPagos = "";
                    string sNumCuenta = "";
                    List<string> lstP = new List<string>();
                    foreach (var item in layout.ltsPayment)
                    {
                        string[] arrayPago = item.Split('|');
                        if (!sPagos.Trim().Equals(""))
                        {
                            if (!arrayPago[14].Trim().Equals(""))
                            {
                                sPagos = sPagos + ", ";
                            }
                        }
                        if (!arrayPago[6].Trim().Equals(""))
                        {
                            if (arrayPago[6].Length > 4)
                            {
                                sNumCuenta = sNumCuenta + arrayPago[6].Substring(arrayPago[6].Length - 4);
                            }
                        }
                        sPagos = sPagos + arrayPago[14].Trim();
                    }
                    docFiscal.metodoDePago = sPagos;
                    docFiscal.numeroCuenta = sNumCuenta;
                }

                decimal granTotal = 0m;
                granTotal = totalTicket + totalImpuesto;
                if (!granTotal.Equals(sumaImpuestos))
                {
                    valido = false;
                }
                if (!granTotal.Equals(docFiscal.total))
                {
                    valido = false;
                }
                return docFiscal;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message + "-" + ex.StackTrace);
            }
        }
        private sucursal AgregarSucursal(string noSucursal)
        {
            sucursal su = new sucursal();
            su.numero = noSucursal;
            su.calle = "";
            su.codigoPostal = "";
            su.colonia = "";
            su.estado = "";
            su.municipio = "";
            su.nombre = "";
            su.pais = "";
            return su;
        }
        private emisor AgregarEmisor()
        {
            emisor emisorDoc = new emisor();
            emisorDoc.id = 0;
            emisorDoc.rfc = "";
            emisorDoc.nombre = "";
            emisorDoc.numCertificado = "";
            emisorDoc.calle = "";
            emisorDoc.numero = "";
            emisorDoc.colonia = "";
            emisorDoc.municipio = "";
            emisorDoc.estado = "";
            emisorDoc.pais = "";
            emisorDoc.codigoPostal = "";
            return emisorDoc;
        }
        private receptor AgregarReceptor()
        {
            receptor receptor = new receptor();
            receptor.rfc = "";
            receptor.nombre = "";
            receptor.id = 0;
            receptor.idSpecified = true;
            receptor.cuenta = "";
            receptor.calle = "";
            receptor.colonia = "";
            receptor.numero = "";
            receptor.codigoPostal = "";
            receptor.estado = "";
            receptor.municipio = "";
            receptor.pais = "";
            receptor.correoElectronico = "";
            return receptor;
        }
        private producto[] AgregarProducto(Layout layout)
        {
            List<producto> retorno = new List<producto>();
            int indice = 0;
            try
            {
                foreach (var mproducto in layout.ltsMenu)
                {
                    detalle detalle = new detalle();
                    producto producto = new producto();
                    producto.id = 0;
                    string[] menuArray = mproducto.Split('|');
                    string[] descipciones = new string[1];
                    descipciones[0] = menuArray[14];
                    producto.descripcion = descipciones;
                    producto.cantidadFacturada = Convert.ToDecimal(menuArray[8]);
                    producto.importeNeto = Convert.ToDecimal(menuArray[10]);
                    totalTicket = totalTicket + Convert.ToDecimal(menuArray[10]);
                    producto.valorUnitarioNeto = Convert.ToDecimal(menuArray[9]);
                    producto.descuento = 0;
                    producto.descuentoDesc = "";
                    producto.impuesto = Convert.ToDecimal(menuArray[11]);
                    totalImpuesto = totalImpuesto + Convert.ToDecimal(menuArray[11]);
                    producto.impuestoDesc = "";
                    producto.ivaPorcentaje = 0;
                    producto.tipoCambio = 0;
                    detalle.id = 1;
                    detalle.importeNeto = 0;

                    List<detalle> ltsDetalle = new List<detalle>();
                    ltsDetalle.Add(detalle);
                    retorno.Add(producto);
                }
                return retorno.ToArray();
            }
            catch (Exception ex)
            {
                throw new Exception("Error al agregar los productos " + ex.Message);
            }
        }
        private agrupadoConceptos[] AgregarTasaImpuestoTrasladado(Layout layout)//documentoFiscal doc)
        {
            try
            {
                List<agrupadoConceptos> ltsAgrupado = new List<agrupadoConceptos>();
                foreach (var item in layout.ltsImpuestos)
                {
                    List<concepto> ltsConcepto = new List<concepto>();
                    agrupadoConceptos agrupado = new agrupadoConceptos();
                    concepto concepto = new concepto();
                    agrupado.id = 0;
                    agrupado.descripcion = "";
                    agrupado.producto = "";
                    agrupado.subtotal = 0m;
                    agrupado.total = 0m;
                    agrupado.descuentoFactura = 0m;
                    agrupado.impuesto = 0m;
                    agrupado.impuestoDesc = "";
                    agrupado.promocion = 0m;
                    agrupado.promocionDesc = "";
                    agrupado.ivaPorcentaje = 1;
                    agrupado.tipoCambio = 1;
                    #region Impuesto
                    impTrasladado impTraISH = new impTrasladado();
                    impTraISH.impuesto = "IVA";
                    string currentImporteItem = "0";
                    try
                    {
                        currentImporteItem = item.Split('|')[12].Trim();
                    }
                    catch (Exception)
                    {
                    }
                    string currentTasaItem = "0";
                    try
                    {
                        currentTasaItem = item.Split('|')[8].Trim();
                    }
                    catch (Exception)
                    {
                    }

                    impTraISH.importe = Convert.ToDecimal(currentImporteItem);
                    impTraISH.tasa = Convert.ToDecimal(currentTasaItem);
                    sumaImpuestos = sumaImpuestos + impTraISH.importe;

                    impTrasladado[] imp = new impTrasladado[1];
                    imp[0] = impTraISH;
                    agrupado.impTrasladado = imp;
                    #endregion

                    concepto.tipoDeCargo = enumTipoDeCargo.RECURRENTE;
                    concepto.cantidadFacturada = 1;
                    concepto.descripcion = new string[1];
                    concepto.importeNeto = 0m;
                    concepto.valorUnitarioNeto = 0m;
                    concepto.tipoCambio = 0.0;
                    concepto.periodoIni = new DateTime();
                    concepto.periodoFin = new DateTime();
                    concepto.tarifaBase = 0m;
                    concepto.descuento = 0m;
                    concepto.descuentoDesc = "";
                    concepto.impuesto = 0m;
                    concepto.impuestoDesc = "";
                    ltsConcepto.Add(concepto);
                    agrupado.concepto = ltsConcepto.ToArray();
                    ltsAgrupado.Add(agrupado);
                }

                return ltsAgrupado.ToArray();
            }
            catch (Exception ex)
            {
                throw new Exception("Error al agregar los impuestos" + ex.Message);
            }
        }
        private detalleUso[] agregarDetalleUso()
        {
            List<detalleUso> agrupado = new List<detalleUso>();
            detalleUso dues = new detalleUso();
            dues.cu = "";
            dues.imp = 0m;
            dues.impSpecified = true;
            return agrupado.ToArray();
        }
        private envio AgregarEnvio()
        {
            envio envio = new envio();
            envio.nombre = "";
            envio.calle = "";
            envio.numero = "";
            envio.colonia = "";
            envio.municipio = "";
            envio.estado = "";
            envio.pais = "";
            envio.codigoPostal = "";
            envio.dirLinea1 = "";
            envio.dirLinea2 = "";
            envio.dirLinea3 = "";
            return envio;
        }
        private fechasProceso AgregarFecha(Layout layout)
        {
            fechasProceso fechaP = new fechasProceso();
            try
            {
                fechaP.corteIni = new DateTime();
                fechaP.corteFin = new DateTime();
                fechaP.expedicion = new DateTime();
                fechaP.fechaDeTipoDeCambio = new DateTime();
                fechaP.fechaDenominativa = new DateTime();
                fechaP.pago = new DateTime();
                fechaP.periodicidad = enumPeriodicidad.MENSUAL;
                fechaP.fechaConsumo = layout.ltsTender[0].Split('|')[7];
                return fechaP;
            }
            catch (Exception ex)
            {
                throw new Exception("Error al agregar la fecha de consumo " + ex.Message);
            }

        }
        private usuario AgregaIdentificadorPresidente()
        {
            usuario usr = new usuario();
            try
            {
                usr.elaboro = "";
                usr.sello = "";
                return usr;
            }
            catch (Exception ex)
            {
                throw new Exception("Error al agregar el identificador " + ex.Message);
            }
        }
        private plantilla AgregaPlantilla()
        {
            plantilla p = new plantilla();
            display d = new display();
            p.id = 1;
            p.layout = "1";
            d.id = 1;
            d.Value = "2";
            p.display = d;
            return p;
        }
        private prestadorServiciosCFD AgregaPrestador()
        {

            prestadorServiciosCFD prestador = new prestadorServiciosCFD();
            prestador.noAutorizacion = 1;
            prestador.nombre = "";
            prestador.rfc = "";
            prestador.noCertificado = "";
            prestador.selloDelPSGECFD = "";
            prestador.fechaAutorizacion = new DateTime();
            return prestador;
        }
        private agregadoTextos AgregaTexto()
        {
            agregadoTextos texto = new agregadoTextos();

            texto.texto1 = "";
            texto.texto2 = "";
            texto.texto3 = "";
            texto.texto4 = "";
            return texto;
        }
        private agregadoImagenes AgregaImagen()
        {
            agregadoImagenes img = new agregadoImagenes();
            img.rutaImagen1 = "";
            img.rutaImagen2 = "";
            return img;
        }

        private referencia[] AgregarReferencias()
        {
            List<referencia> ltsreferencia = new List<referencia>();
            referencia refDocFiscal = new referencia();
            refDocFiscal.banco = "";
            refDocFiscal.tipoFicha = "";
            refDocFiscal.cuentaConvenio = "";
            refDocFiscal.referencia1 = "";
            refDocFiscal.sucursal = "";
            refDocFiscal.clabe = "";
            refDocFiscal.aba = "";
            refDocFiscal.swift = "";
            ltsreferencia.Add(refDocFiscal);
            return ltsreferencia.ToArray();
        }
        private docReferenciado AgregardocRef()
        {
            docReferenciado docRef = new docReferenciado();
            docRef.folio = 0;
            docRef.serie = "";
            return docRef;
        }
        private usuario AgregaUsuario()
        {
            usuario usr = new usuario();
            usr.elaboro = "admin";
            usr.sello = "admin";
            return usr;
        }

    }
}
