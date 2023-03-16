using Network;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Threading;
using System.Xml.Linq;

namespace Server
{
    internal class Program
    {
        public static Network.Server Server;
        public static List<COM> COMs = new List<COM> ();

        private static void StartServer()
        {
            Server = new Network.Server(IPAddress.Any, 2330);
            
            SetRegisterService();
            COMs = GetAllValue();

            Server.OnReceive += ReceiveData;
            Server.OnClientConnected += ClientConnected;
            Server.OnClientDisconnected += ClientDisconnected;
            Server.Start();
        }
        private static void SetRegisterService()
        {
            SQL SqlConn = new SQL();
            string sql = "select guid, displayname from RegistrationService";
            using (var reader = SqlConn.ExecuteQuery(sql))
            {
                var listPeer = Server.GetClients();
                while (reader.Read())
                {
                    var onExit = false;
                    for (int i = 0; i < listPeer.Count; i++)
                    {
                        if (reader.GetGuid(0) == listPeer[i].ID.GUID) onExit = true;
                    }
                    if (!onExit)
                    {
                        Info info = new Info(null, new ClientID
                        {
                            GUID = reader.GetGuid(0),
                            DisplayName = reader.GetString(1),
                            Type = ClientType.Service
                        });
                        listPeer.Add(info);
                    }
                }
            }
        }
        private static void ClientDisconnected(ref Info client)
        {
            var listPeer = Server.GetClients();
            for (int i = 0; i < listPeer.Count; i++)
            {
                if (client.ID.GUID == listPeer[i].ID.GUID)
                {
                    switch (client.ID.Type)
                    {
                        case ClientType.Client:
                            {
                                Log.Info("Клиент: " + client.ID.DisplayName + " отключился");
                                try
                                {
                                    listPeer[i].DropConnection();
                                    listPeer.RemoveAt(i);
                                }
                               catch (Exception ex)
                                {
                                    Log.Error(ex.Source);
                                }
                                break;
                            }
                            
                        case ClientType.Service:
                            {
                                Log.Info("Служба: " + client.ID.DisplayName + " вышла из сети");
                                listPeer[i].DropConnection();
                                var cl = listPeer[i];
                                cl.State = System.Net.NetworkInformation.TcpState.Unknown;
                                listPeer[i] = cl;
                                var srv = new RegistrationService()
                                {
                                    DisplayName = listPeer[i].ID.DisplayName,
                                    GUID = listPeer[i].ID.GUID,
                                    IsOnline = false
                                };
                                for (int j = 0; j < listPeer.Count; j++)
                                {
                                    if(listPeer[j].ID.Type == ClientType.Client)
                                    {
                                        Server.Send(listPeer[j], NetworkSerialization.Serialize(new NetworkPayload(PacketType.ServiceDisconection, NetworkSerialization.Serialize(srv))));
                                    }
                                }
                                break;
                            }
                    }
                    break;
                }
            }
        }

        private static void ClientConnected(ref Info client)
        {
            Log.Warning($"Клиент {client.Client.Client.RemoteEndPoint} подключен");
        }

        private static void ReceiveData(NetworkPayload data, ref Info Sender)
        {
            SQL SqlConn = new SQL();
            var listPeer = Server.GetClients();

            if (data.Type == PacketType.ClientID)
            {
                var ID = (ClientID)NetworkSerialization.Deserialize(data.Data);
                switch (ID.Type)
                {
                    case ClientType.Client:
                        {
                            Sender = Server.RegistrationClient(ID, Sender);
                            Log.Debug($"{Sender.Client.Client.RemoteEndPoint}: Смена имени на {Sender.ID.Name}");
                            break;
                        }
                    case ClientType.Service:
                        {
                            var pear = Server.RegistrationService(ID, Sender);
                            var srv = new RegistrationService()
                            {
                                DisplayName = pear.ID.DisplayName,
                                GUID = pear.ID.GUID,
                                IsOnline = true
                            };
                            for (int j = 0; j < listPeer.Count; j++)
                            {
                                if (listPeer[j].ID.Type == ClientType.Client)
                                {
                                    Server.Send(listPeer[j], NetworkSerialization.Serialize(new NetworkPayload(PacketType.ServiceDisconection, NetworkSerialization.Serialize(srv))));
                                }
                            }
                            Log.Debug($"{Sender.Client.Client.RemoteEndPoint}: Служба {((Info)Server.FindClient(ID.GUID)).ID.DisplayName} запущена");
                            break;
                        }
                }

                NetworkPayload payload = new NetworkPayload
                {
                    Type = PacketType.ClientID
                };
                Server.Send(Sender, NetworkSerialization.Serialize(payload));
            }
            switch (Sender.ID.Type)
            {
                case ClientType.Service:
                    {
                        switch (data.Receiver)
                        {
                            case var value when value == Global.ServerID:
                                {
                                    switch (data.Type)
                                    {
                                        case PacketType.GetSensorListService:
                                            {
                                                try
                                                {
                                                    data.Swap();
                                                    List<COM> _coms = new List<COM>();
                                                    for(int i = 0; i < COMs.Count; i++)
                                                    {
                                                        if(COMs[i]._GuidWorkFlow == Sender.ID.GUID)
                                                        {
                                                            _coms.Add(COMs[i]);
                                                        }
                                                    }
                                                    data.Data = NetworkSerialization.Serialize(_coms.ToArray());
                                                    Server.Send(Sender, NetworkSerialization.Serialize(data));
                                                }
                                                catch (Exception exc)
                                                {
                                                    Log.Error(exc.StackTrace);
                                                }
                                                break;
                                            }
                                    }
                                    break;
                                }
                            case var value when value == Global.CLIENTS:
                                {
                                    if(data.Type == PacketType.NewValueSensor)
                                    {
                                        var sensor = (Sensor)NetworkSerialization.Deserialize(data.Data);
                                        
                                        for (int i = 0; i < COMs.Count; i++)
                                        {
                                            if(COMs[i]._Guid == sensor._COMGUID)
                                            {
                                                for (int j = 0; i < COMs[i].Sensors.Count; j++)
                                                {
                                                    if(COMs[i].Sensors[j]._Guid == sensor._Guid)
                                                    {
                                                        COMs[i].Sensors[j] = sensor;
                                                        break;
                                                    }
                                                }
                                            }
                                        }
                                    }
                                    for (int i = 0; listPeer.Count > 0; i++)
                                    {
                                        if (listPeer[i].ID.Type == ClientType.Client)
                                        {
                                            Server.Send(listPeer[i], NetworkSerialization.Serialize(data));
                                        }
                                    }
                                    break;
                                }
                            default:
                                {
                                    var receiver = Server.FindClient(data.Receiver);
                                    Server.Send((Info)receiver, NetworkSerialization.Serialize(data));
                                    break;
                                }
                        }
                        break;
                    }
                case ClientType.Client:
                    {
                        switch (data.Receiver)
                        {
                            case var value when value == Global.ServerID:
                                {
                                    switch (data.Type)
                                    {
                                        case PacketType.RegistrationService:
                                            {
                                                var register = (RegistrationService)NetworkSerialization.Deserialize(data.Data);
                                                string sql = "insert into RegistrationService (GUID, DisplayName) values " +
                                                    "(@guid, @name)";
                                                SqlConn.SetSqlParameters(new List<System.Data.SqlClient.SqlParameter>
                                                {
                                                    new System.Data.SqlClient.SqlParameter("@guid", register.GUID),
                                                    new System.Data.SqlClient.SqlParameter("@name", register.DisplayName)
                                                });
                                                int count = SqlConn.ExecuteNonQuery(sql);
                                                data.Swap();
                                                data.Data = NetworkSerialization.Serialize("ОК");
                                                data.Type = PacketType.Message;
                                                if (count > 0)
                                                {
                                                    Log.Info($@"Регистрация службы {register.DisplayName} | GUID: {register.GUID}");
                                                    Server.Send(Sender, NetworkSerialization.Serialize(data));
                                                    SetRegisterService();
                                                }
                                                else
                                                {
                                                    data.Data = NetworkSerialization.Serialize("Ошибка");
                                                }
                                                break;
                                            }
                                        case PacketType.GetServiceList:
                                            {
                                                List<RegistrationService> servicePeer = new List<RegistrationService>();
                                                foreach (var pear in listPeer)
                                                {
                                                    if(pear.ID.Type == ClientType.Service)
                                                    {
                                                        var srv = new RegistrationService
                                                        {
                                                            DisplayName = pear.ID.DisplayName,
                                                            GUID = pear.ID.GUID,
                                                            IsOnline = pear.State == System.Net.NetworkInformation.TcpState.Established
                                                        };
                                                        servicePeer.Add(srv);
                                                    }
                                                }
                                                data.Data = NetworkSerialization.Serialize(servicePeer.ToArray());
                                                Server.Send(Sender, NetworkSerialization.Serialize(data));
                                                break;
                                            }
                                        case PacketType.GetAllCom:
                                            {
                                                Guid WF = (Guid)NetworkSerialization.Deserialize(data.Data);
                                                List<COM> _coms = new List<COM>();
                                                for (int i = 0; i < COMs.Count; i++)
                                                {
                                                    if(COMs[i]._GuidWorkFlow == WF)
                                                    {
                                                        var com = COMs[i];
                                                        _coms.Add(com);
                                                    }
                                                }

                                                data.Data = NetworkSerialization.Serialize(_coms.ToArray());
                                                Server.Send(Sender, NetworkSerialization.Serialize(data));
                                                break;
                                            }
                                        case PacketType.GetAllSensors:
                                            {
                                                var COMGUID = (Guid)NetworkSerialization.Deserialize(data.Data);

                                                foreach(var com in COMs)
                                                {
                                                    if(com._Guid == COMGUID)
                                                    {
                                                        data.Data = NetworkSerialization.Serialize(com.Sensors.ToArray());
                                                        Server.Send(Sender, NetworkSerialization.Serialize(data));
                                                        break;
                                                    }
                                                }
                                                break;
                                            }
                                        case PacketType.AddComInWorkflow:
                                            {
                                                var com = (COM)NetworkSerialization.Deserialize(data.Data);
                                                string sql = "insert into COM (GUID, COM, TimerUpdate, TypeCom, Workflow) values " +
                                                    "(@guid, @com, @timer, @type, @wf)";
                                                SqlConn.SetSqlParameters(new List<System.Data.SqlClient.SqlParameter>
                                                {
                                                    new System.Data.SqlClient.SqlParameter("@guid", com._Guid),
                                                    new System.Data.SqlClient.SqlParameter("@com", (int)com._COM),
                                                    new System.Data.SqlClient.SqlParameter("@timer", com._TimerUpdate),
                                                    new System.Data.SqlClient.SqlParameter("@type", (int)com._TypeAdapter),
                                                    new System.Data.SqlClient.SqlParameter("@wf", com._GuidWorkFlow)
                                                });
                                                int count = SqlConn.ExecuteNonQuery(sql);

                                                var peer = (Info)Server.FindClient(com._GuidWorkFlow);
                                                if(peer.State == System.Net.NetworkInformation.TcpState.Listen)
                                                {
                                                    Server.Send(peer, NetworkSerialization.Serialize(data));
                                                }
                                                
                                                data.Swap();
                                                data.Data = NetworkSerialization.Serialize("ОК");
                                                data.Type = PacketType.Message;
                                                if (count > 0)
                                                {
                                                    Log.Info($@"Добавлен COM на {peer.ID.DisplayName}");
                                                    Server.Send(Sender, NetworkSerialization.Serialize(data));
                                                }
                                                else
                                                {
                                                    data.Data = NetworkSerialization.Serialize("Ошибка");
                                                }
                                                COMs = GetAllValue();
                                                goto case PacketType.GetAllCom;
                                            }
                                        case PacketType.AddSensorInCom:
                                            {
                                                var sensor = (Sensor)NetworkSerialization.Deserialize(data.Data);
                                                string sql = "insert into Sensors (GUID, COMGUID, sAdress, Type, Name) values " +
                                                    "(@guid, @com, @addr, @type, @name)";
                                                SqlConn.BeginTransaction();
                                                int count = 0;
                                                Guid wf = Guid.Empty;
                                                try
                                                {
                                                    SqlConn.SetSqlParameters(new List<System.Data.SqlClient.SqlParameter>
                                                    {
                                                        new System.Data.SqlClient.SqlParameter("@guid", sensor._Guid),
                                                        new System.Data.SqlClient.SqlParameter("@com", sensor._COMGUID),
                                                        new System.Data.SqlClient.SqlParameter("@addr", sensor.StartAdress),
                                                        new System.Data.SqlClient.SqlParameter("@type", (int)sensor.TypeSensor),
                                                        new System.Data.SqlClient.SqlParameter("@name", sensor._Name),
                                                    });
                                                    count = SqlConn.ExecuteNonQuery(sql);

                                                    sql = "insert into Channels (GUID, GUIDSENSOR, NAME) values (@guid, @sensorGuid, @name)";
                                                    for(int i = 0; i < sensor.Channels.Length; i++)
                                                    {
                                                        SqlConn.SetSqlParameters(new List<System.Data.SqlClient.SqlParameter>
                                                        {
                                                            new System.Data.SqlClient.SqlParameter("@sensorGuid", sensor._Guid),
                                                            new System.Data.SqlClient.SqlParameter("@guid", sensor.Channels[i].GuId),
                                                            new System.Data.SqlClient.SqlParameter("@name", sensor.Channels[i].NameChannel),
                                                        });
                                                        SqlConn.ExecuteNonQuery();
                                                    }
                                                    

                                                    SqlConn.Commit();
                                                }
                                                catch(Exception ex)
                                                {
                                                    Log.Error(ex.Message);
                                                    SqlConn.RollBack();
                                                }

                                                sql = "select top 1 Workflow from COM where GUID like @guid";
                                                SqlConn.SetSqlParameters(new List<System.Data.SqlClient.SqlParameter>
                                                    {
                                                        new System.Data.SqlClient.SqlParameter("@guid", sensor._COMGUID)
                                                    });
                                                using (var reader = SqlConn.ExecuteQuery(sql))
                                                {
                                                    while (reader.Read())
                                                    {
                                                        wf = reader.GetGuid(0);
                                                    }
                                                }

                                                Log.Info($@"{wf}");
                                                var peer = (Info)Server.FindClient(wf);
                                                Log.Info($@"{peer.ID.DisplayName}");
                                                if (peer.State == System.Net.NetworkInformation.TcpState.Established)
                                                {
                                                    Server.Send(peer, NetworkSerialization.Serialize(data));
                                                    Log.Info($@"{peer.ID.DisplayName} получил данные");
                                                }

                                                data.Swap();
                                                data.Data = NetworkSerialization.Serialize("ОК");
                                                data.Type = PacketType.Message;
                                                if (count > 0)
                                                {
                                                    Log.Info($@"Добавлен датчик на {peer.ID.DisplayName}");
                                                    Server.Send(Sender, NetworkSerialization.Serialize(data));
                                                }

                                                COMs = GetAllValue();
                                                goto case PacketType.GetAllSensors;
                                            }
                                        
                                    }
                                    break;
                                }
                            default:
                                {
                                    var receiver = Server.FindClient(data.Receiver);
                                    Server.Send((Info)receiver, NetworkSerialization.Serialize(data));
                                    break;
                                }
                        }
                        break;
                    }
            }
        }

        private static void WriteInDBValueSensor(Sensor sensor)
        {
            dynamic TestValue(float v)
            {
                if (v == 999) return DBNull.Value;
                else return v;
            }
            SQL SqlConn = new SQL();
            SqlConn.BeginTransaction();
            string sql = "insert into " +
                "SensorsValue (GUIDSENSOR, DT, C1, C2, C3, C4, C5, C6, C7, C8) " +
                "values (@guid, GETDATE(), @c1, @c2, @c3, @c4, @c5, @c6, @c7, @c8)";
            SqlConn.SetSqlParameters(new List<System.Data.SqlClient.SqlParameter>
            {
                new System.Data.SqlClient.SqlParameter("@guid", sensor._Guid),
                new System.Data.SqlClient.SqlParameter("@c1", TestValue(sensor.Channels[0].Value)),
                new System.Data.SqlClient.SqlParameter("@c2", TestValue(sensor.Channels[1].Value)),
                new System.Data.SqlClient.SqlParameter("@c3", TestValue(sensor.Channels[2].Value)),
                new System.Data.SqlClient.SqlParameter("@c4", TestValue(sensor.Channels[3].Value)),
                new System.Data.SqlClient.SqlParameter("@c5", TestValue(sensor.Channels[4].Value)),
                new System.Data.SqlClient.SqlParameter("@c6", TestValue(sensor.Channels[5].Value)),
                new System.Data.SqlClient.SqlParameter("@c7", TestValue(sensor.Channels[6].Value)),
                new System.Data.SqlClient.SqlParameter("@c8", TestValue(sensor.Channels[7].Value)),
            });
            try
            {
                SqlConn.ExecuteNonQuery(sql);
                SqlConn.Commit();
            }
            catch
            {
                SqlConn.RollBack();
            }
            SqlConn.Close();
            
        }

        private static List<COM> GetAllValue()
        {
            SQL SqlConn = new SQL();
            string sql = "select * from COM";
            var comlist = new List<COM>();
            using (var reader = SqlConn.ExecuteQuery(sql))
            {
                while (reader.Read())
                {
                    var _guid = reader.GetGuid(0);
                    var _port = (COMport)reader.GetInt32(1);
                    var _type = (TypeAdapter)reader.GetInt32(3);
                    var _timer = reader.GetFloat(2);
                    var _WF = reader.GetGuid(4);
                    var com = new COM(_guid, _port, _type, _timer)
                    {
                        _GuidWorkFlow = _WF
                    };
                    comlist.Add(com);
                }
            }

            sql = "select * from Sensors where COMGUID = @guid";
            for (int i = 0; i < comlist.Count; i++)
            {
                SqlConn.SetSqlParameters(new List<System.Data.SqlClient.SqlParameter>
                {
                    new System.Data.SqlClient.SqlParameter("@guid", comlist[i]._Guid)
                });
                comlist[i].Sensors = new List<Sensor>();
                using (var reader = SqlConn.ExecuteQuery(sql))
                {
                    while (reader.Read())
                    {
                        var sensor = new Sensor(reader.GetGuid(0), reader.GetGuid(1), reader.GetInt32(2), (TypeSensor)reader.GetInt32(3), reader.GetString(4));

                        var _SqlConn = new SQL();
                        sql = "SELECT GUID, NAME from Channels where GUIDSENSOR = @guidSensor";
                        _SqlConn.SetSqlParameters(new List<System.Data.SqlClient.SqlParameter>
                        {
                            new System.Data.SqlClient.SqlParameter("@guidSensor", sensor._Guid)
                        });
                        using (var _reader = _SqlConn.ExecuteQuery(sql))
                        {
                            if (reader.HasRows)
                            {
                                ReadValue[] value = new ReadValue[8];
                                for(int j = 0; reader.Read(); j++)
                                {
                                    value[j].GuId = reader.GetGuid(0);
                                    value[j].NameChannel = reader.GetString(1);
                                    value[j].Value = 999;
                                    value[j].Status = ConnectionStatus.None;
                                }
                            }
                        }

                        comlist[i].Sensors.Add(sensor);
                    }

                }
            }

            return comlist;
        }

        private static COM[] GetAllCOM(string guid)
        {
            var sql = new SQL();
            List<COM> com = new List<COM>();
            string sqlstr = "select GUID, COM, TypeCom, TimerUpdate, Workflow from COM where Workflow like @guid";
            sql.SetSqlParameters(new List<System.Data.SqlClient.SqlParameter>
            {
                new System.Data.SqlClient.SqlParameter("@guid", guid)
            });
            using(var reader = sql.ExecuteQuery(sqlstr))
            {
                while (reader.Read())
                {
                    com.Add(new COM(reader.GetGuid(0), (COMport)reader.GetInt32(1), (TypeAdapter)reader.GetInt32(2), reader.GetFloat(3)));
                }
                Log.Info(com.Count.ToString());
            }
            return com.ToArray();
        }
        private static Sensor[] GetAllSensor(string guid)
        {
            var sql = new SQL();
            List<Sensor> sensors = new List<Sensor>();
            string sqlstr = "select GUID, COMGUID, sAdress, Type from Sensors where COMGUID like @guid";
            sql.SetSqlParameters(new List<System.Data.SqlClient.SqlParameter>
            {
                new System.Data.SqlClient.SqlParameter("@guid", guid)
            });
            using (var reader = sql.ExecuteQuery(sqlstr))
            {
                while (reader.Read())
                {
                    sensors.Add(new Sensor(reader.GetGuid(0), reader.GetGuid(1), reader.GetInt32(2), (TypeSensor)reader.GetInt32(3)));
                }
                Log.Info(sensors.Count.ToString());
            }
            return sensors.ToArray();
        }

        static void Main(string[] args)
        {
            StartServer();
        }
    }
}
