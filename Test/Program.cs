using Network;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Test
{
    internal class Program
    {
        public static List<COM> COMs = new List<COM>();
        static void Main(string[] args)
        {
            try
            {
                COM o = new COM()
                {
                    _COM = COMport.COM3,
                    _TypeAdapter = TypeAdapter.AC2,
                    _TimerUpdate = 10000
                };
                o.Sensors = new List<Sensor>()
                {
                    new Sensor(o._COM, 1, TypeSensor.МПР51),
                    new Sensor(o._COM, 2, TypeSensor.МПР51),
                    new Sensor(o._COM, 3, TypeSensor.МПР51),
                    new Sensor(o._COM, 4, TypeSensor.МПР51)
                };
                COMs.Add(o);
                File.WriteAllText("settings.cfg", JsonConvert.SerializeObject(COMs, Formatting.Indented));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                using (var wr = new StreamWriter("log.txt"))
                {
                    wr.WriteLine(ex.Message);
                }
            }
        }

        private static void Sensors_OnReadError(string message)
        {
            Console.WriteLine(message);
        }
    }
}
