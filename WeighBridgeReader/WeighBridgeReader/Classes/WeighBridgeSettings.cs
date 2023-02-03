using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace WeighBridgeReader.Classes
{
    internal class WeighBridgeSettings
    {
        private string _ip = string.Empty;
        public string IP { get => _ip; set => _ip = value; }
        public IPAddress IPAddress {
            get {
                if(_ip == string.Empty)
                    throw new Exception("IP not set");

                return IPAddress.Parse(_ip);
            }
        }
        public int Port { get; set; }
    }
}
