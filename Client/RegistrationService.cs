using MetroFramework.Forms;
using Network;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Client
{
    public partial class RegistrationService : MetroForm
    {
        public Network.RegistrationService registration;
        public RegistrationService()
        {
            InitializeComponent();
        }

        private void metroButton1_Click(object sender, EventArgs e)
        {
            Thread thread = new Thread(RegistrationSC);
            thread.Start();
            while (thread.IsAlive)
            {
                Thread.Sleep(1000);
            }
            var ID = JsonConvert.DeserializeObject<ClientID>(File.ReadAllText("service\\ID.cfg"));
            ID.DisplayName = metroTextBox1.Text;
            File.WriteAllText($@"{Directory.GetCurrentDirectory()}\service\ID.cfg", JsonConvert.SerializeObject(ID, Formatting.Indented));
            registration = new Network.RegistrationService()
            {
                DisplayName = metroTextBox1.Text,
            };
            registration.GUID = JsonConvert.DeserializeObject<ClientID>(File.ReadAllText("service\\ID.cfg")).ID;
            
            DialogResult = DialogResult.OK;
            Close();
        }
        private void RegistrationSC()
        {
            Process p = new Process();
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.FileName = $@"{Directory.GetCurrentDirectory()}\bat\install_service.bat";
            p.StartInfo.CreateNoWindow = true;
            p.Start();
            p.WaitForExit();
        }
    }
}
