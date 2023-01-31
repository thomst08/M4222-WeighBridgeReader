using System.Net.Sockets;
using System.Net;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace WeighBridgeReader
{
    public class Worker : BackgroundService
    {
        private readonly IPAddress _deviceIP;
        private readonly int _port;

        private readonly ILogger<Worker> _logger;
        private readonly int _stringLength = 11;
        private readonly int _waitTime = 1000;

        public Worker(ILogger<Worker> logger, IConfiguration configuration)
        {
            _logger = logger;

            _deviceIP = IPAddress.Parse(configuration.GetValue<string>("WeighBridgeIP"));
            _port = configuration.GetValue<int>("WeighBridgePort");
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            
            int byteLength = 253;

            while (!stoppingToken.IsCancellationRequested)
            {
                Socket sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                EndPoint ep = new IPEndPoint(_deviceIP, _port);
                sock.Connect(ep);

                

                if (sock.Connected)
                {
                    byte[] bytes = new byte[byteLength];
                    int i = sock.Receive(bytes);
                    //Console.WriteLine(Encoding.UTF8.GetString(bytes));
                    ReadInWeight(i, bytes);
                }

                sock.Close();

                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                await Task.Delay(_waitTime, stoppingToken);
            }
        }


        private void ReadInWeight(int bytesLength, byte[] bytes)
        {
            //Check to see if read is complete read
            if (bytesLength % _stringLength != 0)
                return;

            for(int i = 0; i < bytesLength; i++)
            {
                if (bytes[i] != 2)
                    continue;

                byte[] data = new byte[7];
                Array.Copy(bytes, i + 2, data, 0, 7);
                i += _stringLength;
                
                bool result = float.TryParse(Encoding.UTF8.GetString(data), out float weight);
                //Check if the weight was read correctly
                if(!result)
                    continue;

                Console.WriteLine($"Found Weight: {weight}");
            }
        }
    }
}