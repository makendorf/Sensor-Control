using Control_Sensor;
using MetroFramework.Forms;
using Network;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Client
{
    public partial class Form1 : MetroForm
    {
        public Clients Client = new Clients();
        public BindingList<Network.RegistrationService> RegistrationService = new BindingList<Network.RegistrationService>();
        public BindingList<COM> Coms = new BindingList<COM>();
        public Form1()
        {
            InitializeComponent();
            Client.Start();
            Client.client.OnReceive += Client_OnReceive;
            metroComboBox1.DisplayMember = "DisplayName";
            metroComboBox1.ValueMember = "GUID";
            metroComboBox1.DataSource = RegistrationService;

            listBox1.DisplayMember = "_COM";
            listBox1.ValueMember = "_GUID";
            listBox1.DataSource = Coms;
            

        }
        private void Form1_Load(object sender, EventArgs e)
        {
            
        }

        private void Client_OnReceive(NetworkPayload data)
        {
            switch (data.Type)
            {
                case PacketType.ClientID:
                    {
                        Log.Info("Успешная регистрация в системе");
                        LoadServiceList();
                        break;
                    }
                case PacketType.Message:
                    {
                        MessageBox.Show((string)NetworkSerialization.Deserialize(data.Data));
                        break;
                    }
                case PacketType.GetServiceList:
                    {
                        var srv = (Network.RegistrationService[])NetworkSerialization.Deserialize(data.Data);
                        RegistrationService.Clear();
                        foreach (var service in srv)
                        {
                            service.Status += Service_Status;
                            service.IsOnline = service.IsOnline;
                            RegistrationService.Add(service);
                        }
                        metroComboBox1.SelectedIndex = 1;
                        break;
                    }
                case PacketType.ServiceDisconection:
                    {
                        var srv = (Network.RegistrationService)NetworkSerialization.Deserialize(data.Data);
                        for(int i = 0; i < RegistrationService.Count; i++)
                        {
                            if(RegistrationService[i].GUID == srv.GUID)
                            {
                                RegistrationService[i].IsOnline = srv.IsOnline;
                                break;
                            }
                        }
                        break;
                    }
                case PacketType.GetAllCom:
                    {
                        var coms = (COM[])NetworkSerialization.Deserialize(data.Data);

                        Coms.Clear();
                        for (int i = 0; i < coms.Length; i++)
                        {
                            Coms.Add(coms[i]);
                            //listBox1.Items.Add(coms[i]._COM);
                        }
                        var payload = new NetworkPayload(PacketType.GetAllSensors);
                        payload.Data = NetworkSerialization.Serialize(listBox1.SelectedValue);
                        Client.client.Send(payload);
                        break;
                    }
                case PacketType.GetAllSensors:
                    {
                        flowLayoutPanel1.Invoke(new Action(() =>
                        {
                            try
                            {
                                var sensors = (Sensor[])NetworkSerialization.Deserialize(data.Data);
                                var panel = flowLayoutPanel1;
                                panel.Controls.Clear();
                                panel.Visible = false;
                                foreach (Sensor sensor in sensors)
                                {
                                    switch (sensor.TypeSensor)
                                    {
                                        case TypeSensor.МВА8:
                                            {
                                                var SensorControl = new Sensor_MBA8_TPM138
                                                {
                                                    type = sensor.TypeSensor,
                                                    _StartAddress = sensor.StartAdress,
                                                    GUID = sensor._Guid,
                                                    Status = sensor.Result,
                                                    T_Prod = sensor.GetChannelsValue(),
                                                    TimeStamp = sensor._Time
                                                };
                                                flowLayoutPanel1.Controls.Add(SensorControl);
                                                break;
                                            }
                                        case TypeSensor.ТРМ138: goto case TypeSensor.МВА8;
                                        case TypeSensor.МПР51:
                                            {
                                                var SensorControl = new SensorMPR51
                                                {
                                                    type = sensor.TypeSensor,
                                                    Channel = sensor.StartAdress,
                                                    GUID = sensor._Guid,
                                                    T_Prod = Math.Round(sensor.Channels[0].Value, 2),
                                                    T_Dry = Math.Round(sensor.Channels[1].Value, 2),
                                                    T_Wet = Math.Round(sensor.Channels[2].Value, 2),
                                                    T_Relative = Math.Round(sensor.Channels[3].Value, 2),
                                                    Status = sensor.Result == ConnectionStatus.Ok ? StatusSensor.Available : StatusSensor.Unavailable,
                                                    TimeStamp = sensor._Time
                                                };
                                                flowLayoutPanel1.Controls.Add(SensorControl);
                                                break;
                                            }
                                    }
                                }
                                panel.Visible = true;
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show(ex.Message);
                            }
                        }));
                        
                        break;
                    }
                case PacketType.NewValueSensor:
                    {
                        var sensor = (Sensor)NetworkSerialization.Deserialize(data.Data);
                        for(int i = 0; i < flowLayoutPanel1.Controls.Count; i++)
                        {
                            switch (TypeDescriptor.GetClassName(flowLayoutPanel1.Controls[i]).ToLower())
                            {
                                case "control_sensor.sensor_mba8_tpm138":
                                    {
                                        var sensorControl = flowLayoutPanel1.Controls[i] as Sensor_MBA8_TPM138;
                                        if(sensorControl.GUID == sensor._Guid)
                                        {
                                            sensorControl.T_Prod = sensor.GetChannelsValue();
                                            sensorControl.Status = sensor.Result;
                                            sensorControl.TimeStamp = sensor._Time;
                                        }
                                        break;
                                    }
                                case "control_sensor.sensormpr51":
                                    {
                                        var sensorControl = flowLayoutPanel1.Controls[i] as SensorMPR51;
                                        if (sensorControl.GUID == sensor._Guid)
                                        {
                                            sensorControl.T_Prod = Math.Round(sensor.Channels[0].Value, 2);
                                            sensorControl.T_Dry = Math.Round(sensor.Channels[1].Value, 2);
                                            sensorControl.T_Wet = Math.Round(sensor.Channels[2].Value, 2);
                                            sensorControl.T_Relative = Math.Round(sensor.Channels[3].Value, 2);
                                            sensorControl.Status = sensor.Result == ConnectionStatus.Ok ? StatusSensor.Available : StatusSensor.Unavailable;
                                            sensorControl.TimeStamp = sensor._Time;
                                        }
                                        break;
                                    }
                            }
                        }
                        
                        break;
                    }

            }
        }

        private void Service_Status(bool status)
        {
            if (status)
            {
                flowLayoutPanel1.BackColor = Color.FromArgb(170, 255, 198);
            }
            else
            {
                flowLayoutPanel1.BackColor = Color.FromArgb(255, 170, 172);
            }
        }

        private void LoadServiceList()
        {
            var payload = new NetworkPayload(PacketType.GetServiceList);
            Client.client.Send(payload);
        }
        private void metroButton1_Click(object sender, EventArgs e)
        {
            var regForm = new RegistrationService();
            if(regForm.ShowDialog() == DialogResult.OK)
            {
                var payload = new NetworkPayload
                {
                    Data = NetworkSerialization.Serialize(regForm.registration),
                    Type = PacketType.RegistrationService
                };
                Client.client.Send(payload);
            }
        }

        private void metroComboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            var payload = new NetworkPayload(PacketType.GetAllCom);
            payload.Data = NetworkSerialization.Serialize(metroComboBox1.SelectedValue);
            Client.client.Send(payload);
        }
        private void flowLayoutPanel1_MouseClick(object sender, MouseEventArgs e)
        {
            if (metroComboBox1.SelectedIndex != -1)
            {
                if (e.Button == MouseButtons.Right)
                {
                    ContextMenuStrip menu = new ContextMenuStrip();
                    menu.Items.Add("Добавить COM порт", null, AddCom);
                    menu.Items.Add("Добавить Устройство", null, AddSensor);
                    menu.Show(panel1, e.X, e.Y);
                }
            }
            else
            {
                ContextMenuStrip menu = new ContextMenuStrip();
                menu.Items.Add("Сначала выберите сервер");
                menu.Show(panel1, e.X, e.Y);
            }
        }
        private void panel1_MouseClick(object sender, MouseEventArgs e)
        {
            
        }

        private void AddSensor(object sender, EventArgs e)
        {
            switch (metroComboBox1.SelectedIndex)
            {
                case -1:
                    {
                        MessageBox.Show("Выбирете сервер");
                        break;
                    }
                default:
                    {
                        var AddCOMForm = new AddSensor(((Network.RegistrationService)metroComboBox1.SelectedItem).DisplayName, (COM)listBox1.SelectedItem);
                        if (AddCOMForm.ShowDialog() == DialogResult.OK)
                        {
                            var sensor = AddCOMForm.sensor;
                            NetworkPayload payload = new NetworkPayload()
                            {
                                Data = NetworkSerialization.Serialize(sensor),
                                Type = PacketType.AddSensorInCom
                            };
                            Client.client.Send(payload);
                        }
                        break;
                    }
            }
        }

        private void AddCom(object sender, EventArgs e)
        {
            switch (metroComboBox1.SelectedIndex)
            {
                case -1:
                    {
                        MessageBox.Show("Выбирете сервер");
                        break;
                    }
                default:
                    {
                        var AddCOMForm = new AddCOM();
                        AddCOMForm.Workflow = ((Network.RegistrationService)metroComboBox1.SelectedItem).GUID;
                        if(AddCOMForm.ShowDialog() == DialogResult.OK)
                        {
                            var com = AddCOMForm.com;
                            NetworkPayload payload = new NetworkPayload()
                            {
                                Data = NetworkSerialization.Serialize(com),
                                Type = PacketType.AddComInWorkflow
                            };
                            Client.client.Send(payload);
                        }
                        break;
                    }
            }
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            var payload = new NetworkPayload(PacketType.GetAllSensors);
            payload.Data = NetworkSerialization.Serialize(listBox1.SelectedValue);
            Client.client.Send(payload);
        }

        private void button1_Click(object sender, EventArgs e)
        {

        }
    }

    public static class Util
    {
        public enum Effect { Roll, Slide, Center, Blend }
        public const int AW_ACTIVATE = 0x20000;
        public const int AW_HIDE = 0x10000;
        public const int AW_BLEND = 0x80000;
        public const int AW_CENTER = 0x00000010;
        public const int AW_SLIDE = 0X40000;
        public const int AW_HOR_POSITIVE = 0x1;
        public const int AW_HOR_NEGATIVE = 0X2;
        public static void Animate(Control ctl, Effect effect, int msec, int angle) 
        {
            int flags = effmap[(int)effect];
            if (ctl.Visible) { flags |= 0x10000; angle += 180; }
            else
            {
                if (ctl.TopLevelControl == ctl) flags |= 0x20000;
                else if (effect == Effect.Blend) throw new ArgumentException();
            }
            flags |= dirmap[(angle % 360) / 45];
            bool ok = AnimateWindow(ctl.Handle, msec, flags);
            if (!ok) throw new Exception("Animation failed");
            ctl.Visible = ctl.Visible;
        }

        private static int[] dirmap = { 1, 5, 4, 6, 2, 10, 8, 9 };
        private static int[] effmap = { 0, 0x40000, 0x10, 0x80000 };

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern bool AnimateWindow(IntPtr handle, int msec, int flags);
    }
}
