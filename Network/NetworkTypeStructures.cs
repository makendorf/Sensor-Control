using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace Network
{
    public enum PacketType
    {
        None = 0,
        ClientID,
        Ping,
        GetSensorListService,
        RegistrationService,
        Message,
        GetServiceList,
        GetAllCom,
        AddComInWorkflow,
        AddSensorInCom,
        GetAllSensors,
        NewValueSensor,
        ServiceDisconection
    }
    
    [Serializable]
    public enum Division
    {
        None    = 0,
        Uboiny  = 1,
        OVKI    = 2,
        Syrevoe = 3
    }
    [Serializable]
    public enum TypeDocument
    {
        None,
        RaportUboiny,
        UboinyNaryadNboic,
        UboinyNaryadKISH,
        UboinyNaryadSubShk,

        OVKI_Sostaviteli,
        OVKI_Termisty,
        OVKI_Formovka,

        Syr_Baader,
        Syr_Rezchiki,
        Syr_Zhilovka,
        Syr_Zasolka,
        Syr_Obvalka,
        Syr_Raspilovka,
        Syr_Rasfasovka
    }
    [Serializable]
    public class NetworkPayload
    {
        public Guid Sender;
        public Guid Receiver;
        public PacketType Type;
        public byte[] Data;

        public NetworkPayload(PacketType type, byte[] data, Guid sender, Guid receiver)
        {
            Type = type;
            Data = data;
            Sender = sender;
            Receiver = receiver;
        }
        public NetworkPayload()
        {
        }
        public NetworkPayload(PacketType type, byte[] data)
        {
            Type = type;
            Data = data;
            Sender = Guid.Empty;
            Receiver = Guid.Empty;
        }
        public NetworkPayload(PacketType type)
        {
            Type = type;
            Data = new byte[0];
            Sender = Guid.Empty;
            Receiver = Guid.Empty;
        }
        public void Swap()
        {
            (Receiver, Sender) = (Sender, Receiver);
        }
    }
    
    public class NetworkSerialization
    {
        public static byte[] Serialize(object Object, bool AppendSize = false, int CountPacket = 1, int ID = 0, int NumberPacket = 0)
        {
            byte[] bytes;
            BinaryFormatter formatter = new BinaryFormatter();
            using (MemoryStream stream = new MemoryStream())
            {
                formatter.Serialize(stream, Object);
                bytes = stream.ToArray();
            }

            if(!AppendSize)
            {
                return bytes;
            }
            else
            {
                int number = bytes.Length;
                int count = CountPacket;
                int id = ID;
                int numberPacket = NumberPacket;

                byte[] data = new byte[number + 16]; //Выделяем память под данные + их размер
                byte[] size = BitConverter.GetBytes(number);
                byte[] countdata = BitConverter.GetBytes(count);
                byte[] iddata = BitConverter.GetBytes(id);
                byte[] idpacket = BitConverter.GetBytes(NumberPacket);

                size.CopyTo(data, 0);
                countdata.CopyTo(data, 4);
                iddata.CopyTo(data, 8);
                idpacket.CopyTo(data, 12);
                bytes.CopyTo(data, 16);
                return data;
            }

        }

        public static object Deserialize(byte[] Data)
        {
            try
            {
                BinaryFormatter formatter = new BinaryFormatter();
                using (MemoryStream stream = new MemoryStream(Data))
                {
                    return formatter.Deserialize(stream);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);
                Log.Debug(ex.StackTrace);
                return null;
            }
        }
    }
}
