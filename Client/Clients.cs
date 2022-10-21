using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Network;

namespace Client
{
    public class Clients
    {
        public Network.Client client;
        public const string addr = "172.16.1.191";
        public const int port = 2330;

        public static ClientID ID = new ClientID
        {
            DisplayName = System.Net.Dns.GetHostName() + "/" + Environment.UserName,
            Name = System.Net.Dns.GetHostName() + "/" + Environment.UserName,
            ID = Guid.NewGuid().ToString(),
            Type = ClientType.Client,
            ChangeIdenififer = false
        };
        public Clients()
        {
        }
        public void Start()
        {
            Connect();
        }
        private void ClientCnf()
        {
            client.OnConnected += Connected;
            client.OnDisconnect += Disconnected;
            client.OnReceive += Receive;
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

        private void Receive(NetworkPayload Packet)
        {
            switch (Packet.Type)
            {
                case PacketType.ClientID:
                    {

                        break;
                    }
            }
        }
    }
}
