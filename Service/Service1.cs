using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace Service
{
    public partial class Service1 : ServiceBase
    {
        Sensors sensors;
        public Service1()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            Process process = Process.GetCurrentProcess();
            FileInfo info = new FileInfo(process.MainModule.FileName);
            Directory.SetCurrentDirectory(info.DirectoryName + @"\");
            sensors = new Sensors();
            try
            {
                sensors.Start();
            }
            catch (Exception ex)
            {
                using(var wr = new StreamWriter("log.txt"))
                {
                    wr.WriteLine(ex.Message);
                }
            }
        }

        protected override void OnStop()
        {
            sensors.Stop();
            sensors.Client.client.Disconnect();
        }
    }
}
