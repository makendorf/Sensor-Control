using com.dalsemi.onewire.adapter;
using com.dalsemi.onewire.container;
using Hangfire.Annotations;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

namespace Network
{
    [Serializable]
    public enum TypeAdapter
    {
        AC2 = 1,
        AC3 = 0,
        None = 2
    }
    [Serializable]
    public enum TypeSensor
    {
        МПР51 = 0,
        ТРМ138 = 1,
        МВА8 = 2,
        None = 3
    }
    [Serializable]
    public enum COMport
    {
        COM1 = 0,
        COM2 = 1,
        COM3 = 2,
        COM4 = 3,
        COM5 = 4,
        COM6 = 5,
        COM7 = 6,
        COM8 = 7,
        None = 9
    }
    [Serializable]
    public struct SensorList
    {
        public int TimerUpdate;
        public COM Com;
        public TypeAdapter Type;
        [NonSerialized]
        public Thread handler;
        public List<Sensor> Sensors;
    }
    [Serializable]
    public class COM : HoundSensorsMetods, INotifyPropertyChanged
    {
        public delegate void ReadError(string message);
        public event ReadError OnReadError;

        public delegate void Read(Sensor sensor);
        public event Read OnRead;

        private string guid = Guid.NewGuid().ToString();
        private string guidworkflow;
        private COMport com = COMport.None;
        private float timerUpdate = 10000;
        private TypeAdapter type = TypeAdapter.None;
        [NonSerialized]
        private Thread handler;
        private ConnectionStatus status = ConnectionStatus.None; 
        private bool _running = false;
        private DateTime timestamp;

        public DateTime TimeStamp
        {
            get => timestamp;
            private set
            {
                if (timestamp != value)
                {
                    timestamp = value;
                    OnPropertyChanged("TimeStamp");
                }
            }
        }
        public string _Guid 
        { 
            get => guid;  
            private set
            {
                if (guid != value)
                {
                    guid = value;
                    OnPropertyChanged("_GUID");
                }
            }
        }
        public string _GuidWorkFlow
        {
            get => guidworkflow;
            set
            {
                if (guidworkflow != value)
                {
                    guidworkflow = value;
                    OnPropertyChanged("_GuidWorkFlow");
                }
            }
        }
        public ConnectionStatus _Status 
        { 
            get => status; 
            private set
            {
                if (status != value)
                {
                    status = value;
                    OnPropertyChanged("_Status");
                }
            }
        }
        public COMport _COM
        {
            get => com;
            set
            {
                if (com != value)
                {
                    com = value;
                    OnPropertyChanged("_COM");
                }
            }
        }
        public TypeAdapter _TypeAdapter
        {
            get => type;
            set
            {
                if (type != value)
                {
                    type = value;
                    OnPropertyChanged("_TypeAdapter");
                }
            }
        }
        public float _TimerUpdate
        {
            get => timerUpdate;
            set
            {
                if (timerUpdate != value)
                {
                    timerUpdate = value;
                    OnPropertyChanged("_TimerUpdate");
                }
            }
        
        }
        public List<Sensor> Sensors = new List<Sensor>();
        public COM()
        {

        }
        public COM(string guid, COMport _port, TypeAdapter type, float update)
        {
            _Guid = guid;
            _COM = _port;
            _TypeAdapter = type;
            _TimerUpdate = update;
        }
        private ConnectionStatus AC3_Open()
        {
            return (ConnectionStatus)OpenPort((int)com, (int)BaudRate.Baud9600, (int)Parity.None, (int)LengthBits.Einteen, (int)StopBits.One, (int)ConverterType.Auto);
        }
        private ConnectionStatus AC2_Open()
        {
            return (ConnectionStatus)AC2_Open((int)com);
        }
        public void OpenPort()
        {
            if(com == COMport.None)
            {
                OnReadError?.Invoke("COM порт не выбран.");
            }
            if(type == TypeAdapter.None)
            {
                OnReadError?.Invoke("Не выбран адаптер");
            }
            _running = true;
            handler = new Thread(Handler);
            handler.Start();
            Thread.Sleep(10000);
            while (!_running)
            {
                handler.Start();
                Thread.Sleep(10000);
            } 
            

        }
        public new void ClosePort()
        {
            if(handler != null)
            {
                handler.Abort();
            }
        }
        private void Handler()
        {
            switch (type)
            {
                case TypeAdapter.AC2:
                    {
                        status = AC2_Open();
                        break;
                    }
                case TypeAdapter.AC3:
                    {
                        status = AC3_Open();
                        break;
                    }
                default:
                    {
                        status = ConnectionStatus.None;
                        OnReadError?.Invoke(ConnectionStatus.None.ToString());
                        break;
                    }
            }
            if(status == ConnectionStatus.Ok)
            {
                while (_running)
                {
                    for (int i = 0; i < Sensors.Count; i++)
                    {
                        var sensor = Sensors[i];
                        if (sensor != null)
                        {
                            try
                            {
                                switch (type)
                                {
                                    case TypeAdapter.AC2:
                                        {
                                            sensor.AC2_ReadMpr51();
                                            timestamp = DateTime.Now;
                                            OnRead?.Invoke(sensor);
                                            break;
                                        }
                                    case TypeAdapter.AC3:
                                        {
                                            sensor.ReadIEEE32();
                                            timestamp = DateTime.Now;
                                            OnRead?.Invoke(sensor);
                                            break;
                                        }
                                    default:
                                        {
                                            break;
                                        }
                                }
                            }
                            catch (Exception ex)
                            {
                                OnReadError?.Invoke(ex.Message);
                            }
                        }
                    }
                    Thread.Sleep((int)timerUpdate);
                }
            }
            else
            {
                _running = false;
                OnReadError?.Invoke(status.ToString());
            }
        }
        public override string ToString() => _COM.ToString();
        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
    [Serializable]
    public class Sensor : HoundSensorsMetods, INotifyPropertyChanged
    {
        private ConnectionStatus result = ConnectionStatus.None;
        private TypeSensor typeSensor = TypeSensor.None;
        private string com = "";
        private int startAdress = 0;
        private string guid = Guid.NewGuid().ToString();
        private DateTime time;
        public TypeSensor TypeSensor { get { return typeSensor; } set { typeSensor = value; } }
        public ConnectionStatus Result { get { return result; } set { result = value; } }
        public string _COMGUID { get { return com; } set { com = value; } }
        public int StartAdress { get { return startAdress; } set { startAdress = value; } }
        public string _Guid { get { return guid; } private set { guid = value; } }
        public DateTime _Time { get { return time; } private set { time = value; } }
        public ReadValue[] Channels = new ReadValue[8]
        {
            new ReadValue("Канал 1", 999, ConnectionStatus.None),
            new ReadValue("Канал 2", 999, ConnectionStatus.None),
            new ReadValue("Канал 3", 999, ConnectionStatus.None),
            new ReadValue("Канал 4", 999, ConnectionStatus.None),
            new ReadValue("Канал 5", 999, ConnectionStatus.None),
            new ReadValue("Канал 6", 999, ConnectionStatus.None),
            new ReadValue("Канал 7", 999, ConnectionStatus.None),
            new ReadValue("Канал 8", 999, ConnectionStatus.None),
        };
        private int timestamp;
        public Sensor(string guid, string com, int channel, TypeSensor type)
        {
            this.guid = guid;
            this.com = com;
            this.startAdress = channel;
            this.typeSensor = type;
        }
        public Sensor(string com, int channel, TypeSensor type)
        {
            this.com = com;
            this.startAdress = channel;
            this.typeSensor = type;
        }
        public Sensor()
        {
        }
        public void AC2_ReadMpr51()
        {
            Result = (ConnectionStatus)AC2_ReadMpr51(StartAdress - 1, (int)BaudRate.Baud9600, ref Channels[0].Value, ref Channels[1].Value, ref Channels[2].Value, ref Channels[3].Value);
            if(Result != ConnectionStatus.Ok)
            {
                Channels[0].Value = Channels[1].Value = Channels[2].Value = Channels[3].Value = 999;
            }
            time = DateTime.Now;
        }
        public void ReadIEEE32()
        {
            for(int i = 0; i < Channels.Length; i++)
            {
                Channels[i].Status = (ConnectionStatus)ReadIEEE32(startAdress + i, (int)LengthBits.Eight, Marshal.StringToCoTaskMemAnsi("rEAd"), ref Channels[i].Value, ref timestamp, -1);
            }
            time = DateTime.Now;
        }
        public ReadValue[] GetChannelsValue()
        {
            return Channels;
        }
        public ConnectionStatus[] GetChannelsStatus()
        {
            ConnectionStatus[] result = new ConnectionStatus[Channels.Length];
            for (int i = 0; i < Channels.Length; i++)
            {
                result[i] = Channels[i].Status;
            }
            return result;
        }
        public override string ToString() => com.ToString();
        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
    [Serializable]
    public struct ReadValue
    {
        public string GuId;
        public string NameChannel;
        public float Value;
        public ConnectionStatus Status;
        public ReadValue(string name, float value, ConnectionStatus status)
        {
            NameChannel = name;
            Value = value;
            Status = status;
            GuId = Guid.NewGuid().ToString();
        }
    }
}
