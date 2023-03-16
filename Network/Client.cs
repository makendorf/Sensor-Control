using System;
using System.Timers;
using System.Net;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Collections.Generic;
//using System.Windows.Forms;

namespace Network
{
    public class Client
    {
        public delegate void DelegateConnectDisconnect();
        public delegate void DelegateReceive(NetworkPayload data);
        public delegate void DelegateSend(NetworkPayload data);
        public delegate void DelegateReceiveError(int ID, Exception exception = null);
        public delegate void DelegateSendError(Exception exception = null);
        public event DelegateConnectDisconnect OnConnected;
        public event DelegateConnectDisconnect OnConnectionFailure;
        public event DelegateConnectDisconnect OnDisconnect;
        public event DelegateReceive OnReceive;
        public event DelegateSend OnSend;
        public event DelegateReceiveError OnReceiveError;
        public event DelegateReceiveError OnSendError;


        private readonly IPAddress _address;
        private readonly int _port;
        private bool _running;
        

        private readonly TcpClient client;
        private System.Timers.Timer _timerPing;
        public IPAddress Address { get { return _address; } }
        public int Port { get { return _port; } }

        public ClientID ID;
        public Thread Handler;

        public Client(string addr, int port)
        {
            _running = false;
            _port = port;
            _address = IPAddress.Parse(addr);
            client = new TcpClient();
            ID = new ClientID();
            OnConnectionFailure += Reconnect;
            _timerPing = new System.Timers.Timer
            {
                Interval = 5000
            };
            _timerPing.Elapsed += _timerPing_Elapsed;
            //Log.OnLog += Log_OnLog;

        }

        private void Log_OnLog(LogLevel Level, string Message)
        {
           using(var writer = new StreamWriter(@"C:\HoundClient\Log.txt", true))
            {
                writer.WriteLine(Message);
            }
        }

        public Client(IPAddress addr, int port)
        {
            _running = false;
            _port = port;
            _address = addr;
            client = new TcpClient();
            ID = new ClientID();
            OnConnectionFailure += Reconnect;
        }
        private void _timerPing_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (client.Connected)
            {
                NetworkPayload ping = new NetworkPayload()
                {
                    Data = NetworkSerialization.Serialize(666),
                    Type = PacketType.Ping
                };
                Send(ping);
            }
            else
            {
                _timerPing.Stop();
                _running = false;
            }
        }

        private void Reconnect()
        {
            Log.Debug("Переподключение...");
            Thread.Sleep(1000);
            Connect();
        }

        public bool IsConnected()
        {
            return client.Connected;
        }

        public void Connect()
        {
            try
            {
                client.Connect(Address, Port);
                Log.Debug("Соединение установленно");
                _running = true;
                _timerPing.Start();
                Handler = new Thread(new ParameterizedThreadStart(HandleConnection));
                Handler.Start(client);
                OnConnected?.Invoke();
            }
            catch (Exception)
            {
                OnConnectionFailure?.Invoke();
            }

        }

        public void Disconnect()
        {
            OnConnectionFailure -= Reconnect;
            _running = false;
            _timerPing.Stop();
            try
            {
                client.Client.Disconnect(false);
            }
            catch { }
        }

        private void SendImpl(byte[] data, int PacketID)
        {
            if (!IsConnected())
            {
                return;
            }
            
            try
            {
                _timerPing.Stop();
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
                for (int i = 0; i < Packets.Count; i++)
                {
                    data = NetworkSerialization.Serialize(Packets[i], true, Packets.Count, PacketID, i + 1);
                    client.Client.Send(data);
                    Thread.Sleep(10);
                }
            }
            catch (Exception exc)
            {
                if (client.Connected && _running)
                {
                    OnSendError?.Invoke(PacketID, exc);
                }

            }
            finally
            {
                _timerPing.Start();
            }
        }

        public void Send(NetworkPayload Payload)
        {
            Payload.Sender = ID.GUID;
            int PacketID = new Random().Next(1000, 2000);
            if (Guid.Empty == Payload.Receiver || null == Payload.Receiver)
            {
                Payload.Receiver = Global.ServerID;
            }
            OnSend?.Invoke(Payload);
            SendImpl(NetworkSerialization.Serialize(Payload, false), PacketID);
        }
       
        private async void HandleConnection(object obj)
        {
            TcpClient tcp = (TcpClient)obj;
            List<PacketsHound> PacketsRecive = new List<PacketsHound>();
            NetworkStream stream = tcp.GetStream();
            stream.Flush();
            bool connected = IsConnected();

            //List<NetworkPayload> PacketList = new List<NetworkPayload>();
            

            while (_running)
            {
                connected = IsConnected();
                if (connected && stream.DataAvailable)
                {
                    byte[] data = new byte[tcp.Client.Available];
                    
                    try
                    {
                        int dataLen = await stream.ReadAsync(data, 0, tcp.Client.Available);
                        byte[] payload = new byte[dataLen];
                        Array.Copy(data, payload, dataLen);

                        //Payload - все данные, что мы получили, далее мы просто идём это всё дело считывать

                        int i = 0;
                        
                        while (i < payload.Length)
                        {
                            int _tryRead = 0;
                            int packetSize = 0;
                            int packetCount = 0;
                            int packetID = 0;
                            int packetNumber = 0;
                            packetSize = BitConverter.ToInt32(payload, i);
                            i += 4;
                            packetCount = BitConverter.ToInt32(payload, i);
                            i += 4; 
                            packetID = BitConverter.ToInt32(payload, i);
                            i += 4;
                            packetNumber = BitConverter.ToInt32(payload, i);
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
                                                Log.Error("Ошибка приема. Повтор");
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

                            var packet = (NetworkPayload)NetworkSerialization.Deserialize(packetData);

                            i += packetSize;
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
                                                OnReceive?.Invoke(SourcePacket);
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
                            catch(Exception)
                            {
                                OnReceiveError?.Invoke(packetID);
                            }
                        }
                    }
                    catch (Exception exc)
                    {
                        Log.Error(exc.Message + "\n" + exc.StackTrace);
                    }
                }
                Thread.Sleep(20);
            }
            OnDisconnect?.Invoke();
        }
    }
}
