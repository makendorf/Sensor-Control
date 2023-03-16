using Network;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace Service
{
    public class Sensors
    {
        private static List<COM> com;
        public static List<COM> COM
        {
            get
            {
                return com;
            } 
            set
            {
                if(com != value)
                {
                    com = value;
                    File.WriteAllText("comlist.cfg", JsonConvert.SerializeObject(com, Formatting.Indented));
                }
            } 
        }
        public Clients Client;
        public void Stop()
        {
            if (COM == null)
            {
                for (int i = 0; i < COM.Count; i++)
                {
                    COM[i].ClosePort();
                }
            }
        }
        public void Start()
        {
            try
            {
                Client = new Clients();
                Client.Start();
                Client.client.OnReceive += Receive;
            }
            catch (Exception ex)
            {
                using (var wr = new StreamWriter("log.txt"))
                {
                    wr.WriteLine(ex.Message);
                }
            }
            Thread.Sleep(5000);
            StartRead();
            //File.WriteAllText("settings.cfg", JsonConvert.SerializeObject(COMs, Formatting.Indented));
        }

        private void StartRead()
        {
            try
            {
                if (COM == null)
                {
                    COM = JsonConvert.DeserializeObject<List<COM>>(File.ReadAllText("comlist.cfg"));

                    for (int i = 0; i < COM.Count; i++)
                    {
                        COM[i].OnReadError += Sensors_OnReadError;
                        COM[i].OnRead += Sensors_OnRead;
                        COM[i].ClosePort();
                        Thread.Sleep(1000);
                        COM[i].OpenPort();
                    }
                }
                else
                {
                    for (int i = 0; i < COM.Count; i++)
                    {
                        COM[i].OnReadError += Sensors_OnReadError;
                        COM[i].OnRead += Sensors_OnRead;
                        COM[i].ClosePort();
                        Thread.Sleep(1000);
                        COM[i].OpenPort();
                    }
                }
                
            }
            catch (Exception ex)
            {
                COM = new List<COM>();
                using (var wr = new StreamWriter("log.txt", true))
                {
                    wr.WriteLine(ex.Message);
                }
            }
        }

        private void Sensors_OnRead(Sensor sensor)
        {
            File.WriteAllText($@"sensors\{sensor.StartAdress}.cfg", JsonConvert.SerializeObject(sensor, Formatting.Indented));
            var payload = new NetworkPayload(PacketType.NewValueSensor, NetworkSerialization.Serialize(sensor), Client.client.ID.GUID, Global.CLIENTS);
            Client.client.Send(payload);
        }

        private void Sensors_OnReadError(string message)
        {
            using (var wr = new StreamWriter("log.txt", true))
            {
                wr.WriteLine(message);
            }
        }

        private void Receive(NetworkPayload Packet)
        {
            switch (Packet.Type)
            {
                case PacketType.ClientID:
                    {
                        NetworkPayload payload = new NetworkPayload(PacketType.GetSensorListService);
                        Client.client.Send(payload);
                        break;
                    }
                case PacketType.GetSensorListService:
                    {
                        var _s = (COM[])NetworkSerialization.Deserialize(Packet.Data);
                        COM = new List<COM>(_s);
                        break;
                    }
                case PacketType.AddComInWorkflow:
                    {
                        var _com = (COM)NetworkSerialization.Deserialize(Packet.Data);
                        COM.Add(_com);
                        break;
                    }
                case PacketType.AddSensorInCom:
                    {
                        try
                        {
                            var _sensor = (Sensor)NetworkSerialization.Deserialize(Packet.Data);
                            for (int i = 0; i < COM.Count; i++)
                            {
                                if (COM[i]._Guid == _sensor._COMGUID)
                                {
                                    COM[i].Sensors.Add(_sensor);
                                }
                            }
                            File.WriteAllText("comlist.cfg", JsonConvert.SerializeObject(com, Formatting.Indented));
                        }
                        catch(Exception ex)
                        {
                            using (var wr = new StreamWriter("log.txt", true))
                            {
                                wr.WriteLine(ex.StackTrace);
                            }
                        }
                        using (var wr = new StreamWriter("log.txt", true))
                        {
                            wr.WriteLine("Новый датчик");
                        }
                        break;
                    }
            }
        }
    }
}
