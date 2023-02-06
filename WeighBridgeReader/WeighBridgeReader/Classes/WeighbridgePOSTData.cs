using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WeighBridgeReader.Classes
{
    internal class WeighbridgePOSTData
    {
        private string _name = string.Empty;
        private string _status = string.Empty;

        public string Weighbridge { get => _name; set => _name = value; }
        public float Weight { get; set; }
        public string Status { get => _status; set=> _status = value; }
        public DateTime Time { get; set; }
    }
}
