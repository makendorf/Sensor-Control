using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Network;
using Newtonsoft.Json;

namespace Service
{
    public class Clients
    {
        public Network.Client client;
        public const string addr = "172.16.1.191";
        public const int port = 2330;

        public static ClientID ID = new ClientID();
        public Clients()
        {
            FileInfo info = new FileInfo(Process.GetCurrentProcess().MainModule.FileName);
            Directory.SetCurrentDirectory(info.DirectoryName + @"\");
            if (File.Exists($@"ID.cfg"))
            {
                ID = JsonConvert.DeserializeObject<ClientID>(File.ReadAllText("ID.cfg"));
            }
            else
            {
                ID = new ClientID
                {
                    DisplayName = System.Net.Dns.GetHostName() + "/" + Environment.UserName,
                    Name = System.Net.Dns.GetHostName() + "/" + Environment.UserName,
                    ID = Guid.NewGuid().ToString(),
                    Type = ClientType.Service,
                    ChangeIdenififer = false
                };
                File.WriteAllText("ID.cfg", JsonConvert.SerializeObject(ID, Formatting.Indented));
            }
            
        }
        public void Start()
        {
            Connect();
        }
        private void ClientCnf()
        {
            client.OnConnected += Connected;
            client.OnDisconnect += Disconnected;

        }
        private void Connect()
        {
            client = new Network.Client(addr, port);
            client.ID = ID;
            ClientCnf();
            Task.Factory.StartNew(() =>
            {
                client.Connect();
            });
        }
        private void Connected()
        {
            NetworkPayload payload = new NetworkPayload(PacketType.ClientID, NetworkSerialization.Serialize(client.ID));
            client.Send(payload);
        }

        private void Disconnected()
        {
            Log.Info("Соединение с сервером разорвано");
        }
    }
}
