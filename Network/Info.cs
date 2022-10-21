using System;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;

namespace Network
{
    public enum ClientType
    {
        Unknown = 0,
        Service = 1,
        Client = 2
    }

    [Serializable]
    public class ClientID
    {
        //  GUID, присваиваемый сервером при соединении
        public string ID;
        //  Сервисное имя клиента
        public string Name = "Undefined";
        //  Отображаемое имя клиента
        public string DisplayName = "Undefined";
        // Может ли клиент изменять свой GUID ?
        public bool ChangeIdenififer = false;
        // Тип клиента служба/клиент
        public ClientType Type;
        public ClientID()
        {
            Type = ClientType.Unknown;
            ID = Guid.NewGuid().ToString();
        }
        public ClientID(string id)
        {
            Type = ClientType.Unknown;
            ID = id;
        }

        public override string ToString()
        {
            return ID;
        }


    }
    [Serializable]
    public struct Info
    {
        [NonSerialized]
        public TcpClient Client;
        public ClientID ID;
        public TcpState State;
        [NonSerialized]
        public Thread Handler;
        [NonSerialized]
        public DateTime Inactivity;
        [NonSerialized]
        public int CountPacket;
        public void DropConnection()
        {
            State = TcpState.Unknown;
            Client.Close();
        }

        public Socket GetSocket()
        {
            return Client.Client;
        }
        public override bool Equals(object obj)
        {
            if ((obj == null) || !GetType().Equals(obj.GetType()))
            {
                return false;
            }
            else
            {
                Info p = (Info)obj;
                return (Client == p.Client);
            }
        }

        public override int GetHashCode()
        {
            return Client.GetHashCode();
        }

        public override string ToString()
        {
            return ID.ToString();
        }
        public void SetInactive()
        {
            Inactivity = DateTime.Now;
        }
        public Info(TcpClient client, ClientID clientID = null) : this()
        {
            if (clientID == null)
            {
                ID = new ClientID();
                Client = client;
                Handler = null;
                State = TcpState.Unknown;
            }
            else
            {
                ID = clientID;
                Client = client;
                Handler = null;
                State = TcpState.Unknown;
            }
            Inactivity = DateTime.Now;
            Client = client;

        }
    }

}
