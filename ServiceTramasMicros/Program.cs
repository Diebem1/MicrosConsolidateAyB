using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;

namespace ServiceTramasMicros
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(string[] args)
        {
//#if (!DEBUG)
            ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[] 
            { 
                new ServicioTramasMicros() 
            };
            ServiceBase.Run(ServicesToRun);
//#else
            //Debug
            //ServicioTramasMicros servicio = new ServicioTramasMicros();
            //servicio.Process();
            //System.Threading.Thread.Sleep(System.Threading.Timeout.Infinite);
//#endif
        }        
    }
}