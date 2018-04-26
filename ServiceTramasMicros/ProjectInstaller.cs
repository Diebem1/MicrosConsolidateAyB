using ServiceTramasMicros.Model;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Install;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;

namespace FactoSender
{
    [RunInstaller(true)]
    public partial class ProjectInstaller : System.Configuration.Install.Installer
    {
        public ProjectInstaller()
        {
            InitializeComponent();
        }
        protected override void OnBeforeUninstall(IDictionary savedState)
        {
            base.OnBeforeUninstall(savedState);//Esta linea siempre debe ir, y estar antes que todas
            #region Quitar el proceso que pudiera estarse ejecutando
            string nombreProceso = "";
            try
            {
                nombreProceso = serviceInstaller1.ServiceName;
            }
            catch (Exception)
            {
                nombreProceso = "FactoSender";
            }
            try
            {
                Process[] tempProcArray = Process.GetProcessesByName(nombreProceso);
                foreach (Process tempPro in tempProcArray)
                {
                    Funciones.EscribeLog("Finalizando proceso " + nombreProceso + ".\n"
                                     , EventLogEntryType.Information, false);
                    tempPro.Kill();
                    tempPro.WaitForExit();
                }

            }
            catch (Exception ex)
            {
                Funciones.EscribeLog("No fue posible finalizar proceso " + nombreProceso + ".\n"
                                     + ex + " - \n"
                                     , EventLogEntryType.Warning, false);
            }
            #endregion
        }
    }
}