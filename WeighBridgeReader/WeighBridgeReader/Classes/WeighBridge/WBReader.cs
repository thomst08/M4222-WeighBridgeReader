using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace WeighBridgeReader.Classes.WeighBridge
{
    internal abstract class WBReader
    {
        private readonly ILogger<Worker> _logger;
        private readonly int _readLength = 256;

        private IPAddress IPAddress { get; set; }
        private int Port { get; set; }

        public string WeighbridgeName { get; private set; }
        public float LastReadValue { get; protected set; }
        public char WeighBridgeStatus { get; protected set; }


        /// <summary>
        /// Setups up the weighbridge into a default state
        /// </summary>
        /// <param name="logger">Logger to write errors</param>
        /// <param name="settings"></param>
        public WBReader(ILogger<Worker> logger, WeighBridgeSettings settings)
        {
            _logger = logger;
            WeighbridgeName = settings.WeighbridgeName;
            IPAddress = settings.IPAddress;
            Port = settings.Port;

            LastReadValue = -1;
            WeighBridgeStatus = ' ';
        }


        /// <summary>
        /// Used to read the device over the network
        /// </summary>
        /// <returns>Returns true if read was successful, false otherwise</returns>
        public bool ReadNetwork()
        {
            bool result = false;

            try
            {
                Socket sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                EndPoint ep = new IPEndPoint(IPAddress, Port);
                sock.Connect(ep);


                if (sock.Connected)
                {
                    byte[] bytes = new byte[_readLength];
                    int i = sock.Receive(bytes);
                    result = ReadInWeight(i, bytes);
                }

                sock.Close();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error Reading: {ex.Message}");
                result = false;
            }

            return result;
        }


        /// <summary>
        /// Extracts the weighbridge data into object for POST
        /// </summary>
        /// <returns>Setups object with weight data for POSTing</returns>
        public WeighbridgePOSTData ExtractPostData()
        {
            return new WeighbridgePOSTData { Weighbridge = this.WeighbridgeName, Weight = this.LastReadValue, Status = this.WeighBridgeStatus.ToString() };
        }


        /// <summary>
        /// Used to read the data from a bridge, all bridges can be different, this will allow for changes for each bridge
        /// </summary>
        /// <param name="bytesLength">Length of the data received form the communication</param>
        /// <param name="bytes">Bytes of data received from read</param>
        /// <returns>Returns true if data was read, else false for all others</returns>
        /// <exception cref="NotImplementedException">Only throws an error if this function is not overridden</exception>
        protected virtual bool ReadInWeight(int bytesLength, byte[] bytes)
        {
            throw new NotImplementedException();
        }
    }
}
