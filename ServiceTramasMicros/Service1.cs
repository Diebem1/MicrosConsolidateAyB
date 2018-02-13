using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading;

namespace ServiceTramasMicros
{
    public partial class Service1 : ServiceBase
    {
        WorkerRole workerRole = new WorkerRole();
        public Service1()
        {
            InitializeComponent();
        }
        protected override void OnStart(string[] args)
        {
            workerRole._thread = new Thread(workerRole.WorkerThreadFunc);
            workerRole._thread.Name = "Service Tramas Micros";
            workerRole._thread.IsBackground = true;
            workerRole._thread.Start();
        }
        protected override void OnStop()
        {
            workerRole._shutdownEvent.Set();
            if (!workerRole._thread.Join(3000))
            { // give the thread 3 seconds to stop
                workerRole._thread.Abort();
            }
        }
        public void Process()
        {
            //Console.WriteLine("Activado");
            workerRole._thread = new Thread(workerRole.WorkerThreadFunc);
            workerRole._thread.Name = "Service Tramas Micros";
            workerRole._thread.IsBackground = true;
            workerRole._thread.Start();
        }
    }
}