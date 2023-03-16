using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Net.Sockets;
using System.Net;
using System.Net.NetworkInformation;

namespace Network
{
    public static class ClientExtensions
    {
        public static TcpState GetState(this TcpClient tcpClient)
        {
            var foo = IPGlobalProperties.GetIPGlobalProperties()
              .GetActiveTcpConnections()
              .SingleOrDefault(x => x.LocalEndPoint.Equals(tcpClient.Client.LocalEndPoint)
                                 && x.RemoteEndPoint.Equals(tcpClient.Client.RemoteEndPoint)
              );

            return foo != null ? foo.State : TcpState.Unknown;
        }

    };

    public class Server
    {
        public delegate void DelegateStartStop();
        public delegate void DelegeateConnection(ref Info client);
        public delegate void DelegateReceive(NetworkPayload data, ref Info client);
        public delegate void DelegateReceiveError(ref Info client, int ID, Exception exception = null);
        public delegate void DelegateSendError(Exception exception = null);
        private delegate void DelegateTestConnection(ref Info Client);
        public event DelegeateConnection OnClientConnected;
        public event DelegeateConnection OnClientDisconnected;
        public event DelegateReceive OnReceive;
        public event DelegateStartStop OnStart;
        public event DelegateReceiveError OnReceiveError;
        public event DelegateReceiveError OnSendError;
        private event DelegateTestConnection OnTestConnection;


        private readonly IPAddress _address;
        private readonly int _port;
        private bool _running;
        private List<Info> _connections;
        readonly TcpListener listener;
        private Thread threadListener;

        public IPAddress Address { get { return _address; } }
        public int Port { get { return _port; } }

       
        public Server(string addr, int port)
        {
            _running = false;
            _port = port;
            _address = IPAddress.Parse(addr);
            _connections = new List<Info>();
            listener = new TcpListener(Address, Port);
        }

        public Server(IPAddress addr, int port)
        {
            _running = false;
            _port = port;
            _address = addr;
            _connections = new List<Info>();

            listener = new TcpListener(Address, Port);
        }

        public object FindClient(Guid receiver)
        {
            for(int i = 0; i < _connections.Count; i++)
            {
                Info info = _connections[i];
                if(info.ID.GUID == receiver)
                {
                    return info;
                }
            }
            return null;
        }

       

        public bool IsRunning()
        {
            return _running;
        }

        public void Stop()
        {
            listener.Stop();
            _running = false;
            for (int i = 0; i < _connections.Count; i++)
            {
                if(_connections[i].State == TcpState.Established)
                {
                    _connections[i].DropConnection();
                }
            }
        }

        public void Start()
        {
            if (IsRunning())
            {
                return;
            }
            
            threadListener = new Thread(new ParameterizedThreadStart(ListenerClients));
            threadListener.Start();
        }
        private void ListenerClients(object obj)
        {
            Log.Warning("Запуск сервера...");
            listener.Start();
            _running = true;
            OnStart?.Invoke();
            Log.Success($"Сервер запущен.");
            while (IsRunning())
            {
                TcpClient client = listener.AcceptTcpClient();
                Info clientInfo = new Info(client)
                {
                    State = ClientExtensions.GetState(client)
                };
                Thread handler = new Thread(new ParameterizedThreadStart(HandleClient));
                clientInfo.Handler = handler;
                _connections.Add(clientInfo);
                handler.Start(clientInfo);
                OnTestConnection?.Invoke(ref clientInfo);
                OnClientConnected?.Invoke(ref clientInfo);
            }
        }
        public ref List<Info> GetClients()
        {
            return ref _connections;
        }
        public List<Info> GetService()
        {
            var listPear = new List<Info>();
            for(int i = 0; i < _connections.Count; i++)
            {
                if(_connections[i].ID.Type == ClientType.Service)
                {
                    listPear.Add(_connections[i]);
                }
            }
            return listPear;
        }
        public Info RegistrationClient(ClientID ID, Info Client)
        {
            int _id = _connections.IndexOf(Client);
            var _client = _connections[_id];
            _client.ID = ID;
            _connections[_id] = _client;
            return _connections[_id];
        }
        public Info RegistrationService(ClientID ID, Info Client)
        {
            for(int i = 0; i < _connections.Count; i++)
            {
                if(ID.GUID == _connections[i].ID.GUID)
                {
                    Client.ID = ID;
                    _connections[i] = Client;
                    for (int j = _connections.Count - 1; j > 0; j--)
                    {
                        if(_connections[j].ID.Type == ClientType.Unknown)
                        {
                            _connections.RemoveAt(j);
                            return _connections[i];
                        }
                    }
                }
            }
            return new Info();
        }
        public void Send(Info client, byte[] data)
        {
            NetworkPayload pay = (NetworkPayload)NetworkSerialization.Deserialize(data);
            int PacketID = new Random().Next(1000, 2000);
            if (client.State != TcpState.Established)
            {
                Log.Debug(client.State.ToString());
                return;
            }
            try
            {
                var Packets = new List<NetworkPayload>();
                List<byte> buffer = new List<byte>();
                for (int i = 0, j = 0; i < data.Length; i++, j++)
                {
                    if (j < 4096)
                    {
                        buffer.Add(data[i]);
                    }
                    else
                    {
                        j = 0;
                        Packets.Add(new NetworkPayload(PacketType.None, buffer.ToArray()));
                        //Log.Success(buffer.Count.ToString());
                        buffer.Clear();
                        buffer.Add(data[i]);
                    }
                }
                Packets.Add(new NetworkPayload(PacketType.None, buffer.ToArray()));
                int leg = 0;
                for (int i = 0; i < Packets.Count; i++)
                {
                    //Log.Info($"Пакет №{i + 1}: {Packets[i].Data.Length}");
                    leg += Packets[i].Data.Length;
                    data = NetworkSerialization.Serialize(Packets[i], true, Packets.Count, PacketID, i + 1);
                    try
                    {
                        client.GetSocket().Send(data);
                    }
                    catch { }
                    Thread.Sleep(10);
                }
            }
            catch (Exception exc)
            {
                OnSendError?.Invoke(ref client, PacketID, exc);
            }
        }


        private async void HandleClient(object obj)
        {
            Info client = (Info)obj;
            
            int _id = 0;
            List<PacketsHound> PacketsRecive = new List<PacketsHound>();
            NetworkStream stream = client.Client.GetStream();
            client.State = ClientExtensions.GetState(client.Client);
            //List<NetworkPayload> PacketList = new List<NetworkPayload>();
            //List<PacketsHound> Packets = new List<PacketsHound>();
            while (client.State == TcpState.Established)
            {
                try
                {
                    if (ClientExtensions.GetState(client.Client) != client.State)
                    {
                        client.State = ClientExtensions.GetState(client.Client);
                    }
                    if (client.State == TcpState.Established && stream.DataAvailable)
                    {
                        try
                        {

                            byte[] data = new byte[client.Client.Available];
                            //int dataLen = stream.Read(data, 0, client.Client.Available);
                            int dataLen = await stream.ReadAsync(data, 0, client.Client.Available);
                            byte[] payload = new byte[dataLen];
                            Array.Copy(data, payload, dataLen);

                            int i = 0;

                            while (i < payload.Length)
                            {
                                int _tryRead = 0;
                                int packetSize = BitConverter.ToInt32(payload, i);
                                i += 4;
                                int packetCount = BitConverter.ToInt32(payload, i);
                                i += 4;
                                int packetID = BitConverter.ToInt32(payload, i);
                                i += 4;
                                int packetNumber = BitConverter.ToInt32(payload, i);
                                i += 4;
                                byte[] packetData = new byte[packetSize];

                                try
                                {
                                    Array.Copy(payload, i, packetData, 0, packetSize);
                                }
                                catch
                                {
                                    while (true)
                                    {
                                        if (_tryRead < packetCount + 6)
                                        {

                                            Thread.Sleep(40);
                                            try
                                            {
                                                data = new byte[client.Client.Available];
                                                dataLen = await stream.ReadAsync(data, 0, client.Client.Available);
                                                byte[] buff = new byte[payload.Length + dataLen];
                                                Array.Copy(payload, buff, payload.Length);
                                                Array.Copy(data, 0, buff, payload.Length, dataLen);
                                                payload = new byte[buff.Length];
                                                Array.Copy(buff, payload, buff.Length);
                                                try
                                                {
                                                    Array.Copy(payload, i, packetData, 0, packetSize);
                                                    break;
                                                }
                                                catch
                                                {
                                                    //Log.Error($"{client.ID.Name}: Ошибка приема. Повтор");
                                                    _tryRead += 1;
                                                }
                                            }
                                            catch { }
                                        }
                                        else
                                        {
                                            break;
                                        }
                                    }
                                }
                                if (_tryRead > packetCount + 6)
                                {
                                    await stream.FlushAsync();
                                    payload = new byte[0];
                                    break;
                                }

                                i += packetSize;

                                var packet = (NetworkPayload)NetworkSerialization.Deserialize(packetData);

                                try
                                {
                                    if (PacketsRecive.Count == 0)
                                    {
                                        PacketsRecive.Add(new PacketsHound
                                        {
                                            ID = packetID,
                                            Quantity = packetCount,
                                            PacketList = new List<PacketHound>()
                                            {
                                                new PacketHound
                                                {
                                                    NumberPacket = packetNumber,
                                                    Size = packetSize,
                                                    Data = packet.Data
                                                }
                                            }
                                        });
                                    }
                                    for (int j = 0; j < PacketsRecive.Count; j++)
                                    {
                                        if (PacketsRecive[j].ID == packetID)
                                        {
                                            if (packetNumber > 1)
                                            {
                                                PacketsRecive[j].PacketList.Add(new PacketHound
                                                {
                                                    NumberPacket = packetNumber,
                                                    Size = packetSize,
                                                    Data = packet.Data
                                                });
                                            }
                                            if (PacketsRecive[j].PacketList.Count == PacketsRecive[j].Quantity)
                                            {
                                                PacketsRecive[j].PacketList.Sort((x, y) => x.NumberPacket.CompareTo(y.NumberPacket));
                                                byte[] buffer = new byte[0];
                                                foreach (var _packet in PacketsRecive[j].PacketList)
                                                {
                                                    var _buffer = new byte[_packet.Data.Length + buffer.Length];
                                                    Array.Copy(buffer, 0, _buffer, 0, buffer.Length);
                                                    Array.Copy(_packet.Data, 0, _buffer, buffer.Length, _packet.Data.Length);

                                                    buffer = _buffer;
                                                }
                                                var SourcePacket = (NetworkPayload)NetworkSerialization.Deserialize(buffer);
                                                try
                                                {
                                                    PacketsRecive.RemoveAt(j);
                                                }
                                                finally
                                                {
                                                    _id = _connections.IndexOf(client);
                                                    if (_id != -1)
                                                    {
                                                        var _cl = _connections[_id];
                                                        client.CountPacket += 1;
                                                        OnReceive?.Invoke(SourcePacket, ref _cl);
                                                    }

                                                    else
                                                    {
                                                        Log.Warning("Unknown client");
                                                    }
                                                }

                                            }
                                            break;
                                        }
                                        else if (j == PacketsRecive.Count - 1)
                                        {
                                            PacketsRecive.Add(new PacketsHound
                                            {
                                                ID = packetID,
                                                Quantity = packetCount,
                                                PacketList = new List<PacketHound>()
                                            {
                                                new PacketHound
                                                {
                                                    NumberPacket = packetNumber,
                                                    Size = packetSize,
                                                    Data = packet.Data
                                                }
                                            }
                                            });
                                            break;
                                        }
                                    }
                                }
                                catch (Exception exc)
                                {
                                    _id = _connections.IndexOf(client);
                                    if (_id != -1)
                                    {
                                        var _cl = _connections[_id];
                                        OnReceiveError?.Invoke(ref _cl, packetID, exc);
                                    }
                                }
                            }
                        }
                        catch { }

                    }
                    Thread.Sleep(40);
                }
                catch
                {
                    break;
                }
            }
            _id = _connections.IndexOf(client);
            if (_id != -1)
            {
                var _cl = _connections[_id];
                OnClientDisconnected?.Invoke(ref _cl);
            }
        }
    }
}
