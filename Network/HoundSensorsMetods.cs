using com.dalsemi.onewire;
using com.dalsemi.onewire.adapter;
using com.dalsemi.onewire.container;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Network
{
    public enum BaudRate
    {
        [Description("2400")]
        Baud2400 = 0,
        [Description("4800")]
        Baud4800 = 1,
        [Description("9600")]
        Baud9600 = 2,
        [Description("14400")]
        Baud14400 = 3,
        [Description("19200")]
        Baud19200 = 4,
        [Description("38400")]
        Baud38400 = 6,
        [Description("57600")]
        Baud57600 = 7,
        [Description("115200")]
        Baud115200 = 8
    }

    public enum Parity
    {
        [Description("Отсутствует")]
        None = 0,
        [Description("Чет")]
        Even = 1,
        [Description("Нечет")]
        Odd = 2
    }

    public enum LengthBits
    {
        [Description("8")]
        Einteen = 1,
        [Description("11")]
        Eight = 0
    }

    public enum StopBits
    {
        [Description("1")]
        One = 0,
        [Description("2")]
        Two = 2
    }

    public enum ConverterType
    {
        Wire = 2,
        Auto = 1,
        Semiauto = 0
    }
    public enum ConnectionStatus
    {
        Ok = 0,
        None = 1,

        InvalidArgument = -1,
        PortNotOpened = -2,
        PortError = -5,
        Io = -100,
        Format = -101,
        Timeout = -102,
        InvalidCrc = -103,
        NErr = -104,
        Exception = -105,
        InvalidPacket = -106,
        SensorFailure = -300
    }
    [Serializable]
    public class HoundSensorsMetods
    {
        
        [DllImport("owen_io.dll", EntryPoint = "OpenPort", CallingConvention = CallingConvention.StdCall)]
        public static extern int OpenPort(int n, int speed, int part, int bits, int stop, int vid);

        [DllImport("owen_io.dll", EntryPoint = "ClosePort", CallingConvention = CallingConvention.StdCall)]
        public static extern int ClosePort();

        [DllImport("owen_io.dll", EntryPoint = "ReadIEEE32", CallingConvention = CallingConvention.StdCall)]
        public static extern int ReadIEEE32(int adr, int adr_type, System.IntPtr command, ref float value, ref int time, int index);

        [DllImport("owen_io.dll", EntryPoint = "ReadUInt", CallingConvention = CallingConvention.StdCall)]
        public static extern int ReadUInt(int adr, int adr_type, System.IntPtr command, ref int value, int index);

        [DllImport("owen_io.dll", EntryPoint = "WriteIEEE32", CallingConvention = CallingConvention.StdCall)]
        public static extern int WriteIEEE32(int adr, int adr_type, System.IntPtr command, float value, int index);

        [DllImport("owen_io.dll", EntryPoint = "AC2_ReadMpr51", CallingConvention = CallingConvention.StdCall)]
        public static extern int AC2_ReadMpr51(int adr, int speed, ref float t_prod, ref float t_suhogo, ref float t_vlag, ref float otn_vlag);
        
        [DllImport("owen_io.dll", EntryPoint = "AC2_Open", CallingConvention = CallingConvention.StdCall)]
        public static extern int AC2_Open(int n);
        [DllImport("owen_io.dll", EntryPoint = "ReadStoredDotS", CallingConvention = CallingConvention.StdCall)]
        public static extern int ReadStoredDotS(int address, int addressType, System.IntPtr parameterName, ref float value, int index);
        [DllImport("owen_io.dll", EntryPoint = "OwenIO", CallingConvention = CallingConvention.StdCall)]
        public static extern int OwenIO(int address, int addressType, int is_read, ref System.IntPtr parameterName, ref char[] _params, ref int param_sz);
        public static void getAdapter(ref DSPortAdapter Adapter, string USB, string adapterName)
        {
            try
            {
                Adapter = OneWireAccessProvider.getAdapter(adapterName, USB);
                //Log.Debug(Adapter.ToString());
                Adapter.adapterDetected();
                Adapter.targetAllFamilies();
                Adapter.beginExclusive(true);
                Adapter.reset();
                Adapter.setSearchAllDevices();
            }
            catch
            {
                throw new Exception($"Не удалось подключиться к выбранному устройству: {adapterName} - {USB}");
            }
        }
        internal static double getTemperature(TemperatureContainer tc)
        {
            byte[] state = tc.readDevice();
            double lastTemp;

            tc.doTemperatureConvert(state);
            state = tc.readDevice();
            lastTemp = tc.getTemperature(state);

            return lastTemp;
        }
    }
}
