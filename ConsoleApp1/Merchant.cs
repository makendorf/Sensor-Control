using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp1
{
    public struct Document
    {
        public double Возмещение;
        public double Коммисия;
    }
    public class Merchant
    {
        static public List<Merchant> list = new List<Merchant>();

        
        public string ID;
        public Document Doc;
        bool IsWrite;

        public bool IsExist()
        {
            return IsWrite;
        }

        void asd()
        {
            
        }
    }
}
