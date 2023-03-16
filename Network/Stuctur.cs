using Hangfire.Annotations;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Network
{
    [Serializable]
    public class RegistrationService : INotifyPropertyChanged
    {
        public RegistrationService(Guid _guid, string _name)
        {
            GUID = _guid;
            DisplayName = _name;
        }
        public RegistrationService()
        {
            GUID = Guid.Empty;
            DisplayName = "";
        }

        private Guid guid;
        private string name;
        private bool isOnline;

        public delegate void StatusChanged(bool status);
        public event StatusChanged Status;

        public bool IsOnline
        {
            get => isOnline;
            set
            {
                isOnline = value;
                OnPropertyChanged("IsOnline");
                Status?.Invoke(isOnline);
            }
        }
        public string DisplayName 
        {
            get => name;
            set 
            {
                if (name != value)
                {
                    name = value;
                    OnPropertyChanged("DisplayName");
                }
            } 
        }
        public Guid GUID
        {
            get => guid;
            set
            {
                if (guid != value)
                {
                    guid = value;
                    OnPropertyChanged("GUID");
                }
            }
        }

        public override string ToString() => DisplayName;
        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
    public struct PacketsHound
    {
        public int ID;
        public int Quantity;
        public List<PacketHound> PacketList;
    }
    public struct PacketHound
    {
        public int Size;
        public byte[] Data;
        public int NumberPacket;
    }

}
