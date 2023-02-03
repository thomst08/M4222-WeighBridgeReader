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
        private string _weighbridgeName = string.Empty;

        public string WeighbridgeName { get => _weighbridgeName; set => _weighbridgeName = value; }
        public string IP { get => _ip; set => _ip = value; }
        public int Port { get; set; }

        public IPAddress IPAddress {
            get {
                if(_ip == string.Empty)
                    throw new Exception("IP not set");

                return IPAddress.Parse(_ip);
            }
        }
    }
}
