using System.Net.Sockets;
using System.Net;
using System.Text;
using Microsoft.Extensions.Configuration;
using WeighBridgeReader.Classes.WeighBridge;
using WeighBridgeReader.Classes;

namespace WeighBridgeReader
{
    public class Worker : BackgroundService
    {
        //Logger used for errors and messages
        private readonly ILogger<Worker> _logger;

        //Delayed wait time for how long to wait till the next run
        private readonly int _waitTime = 1000;

        //Contains all the weighbridges to read
        private List<M4222_WBReader> _weighBridges = new List<M4222_WBReader>(); 


        /// <summary>
        /// Constructor used to setup the service and to load all the weighbridges
        /// </summary>
        /// <param name="logger">Access to the eventlog and messages</param>
        /// <param name="configuration">Access to the config files for loading in settings</param>
        /// <exception cref="ArgumentException">Only occurs if the config file is incorrectly setup</exception>
        public Worker(ILogger<Worker> logger, IConfiguration configuration)
        {
            _logger = logger;

            List<WeighBridgeSettings> weighBridges = configuration.GetSection("WeighBridges").Get<List<WeighBridgeSettings>>();

            if(weighBridges == null)
            {
                throw new ArgumentException("Appsettings file is incorrectly configured");
            }
 
            foreach(WeighBridgeSettings wb in weighBridges)
            {
                if (wb.IP == string.Empty || wb.Port == 0)
                    new Exception("Weighbridge incorrectly setup in the config file");

                _weighBridges.Add(new M4222_WBReader(logger, wb));
            }
        }


        /// <summary>
        /// Executes on run and controls the looping
        /// </summary>
        /// <param name="stoppingToken"></param>
        /// <returns></returns>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                foreach(WBReader wb in _weighBridges)
                {
                    bool result = wb.ReadNetwork();
                    if(result)
                    {
                        _logger.LogInformation($"WeighBridge: Weight - {wb.LastReadValue} - Status - {wb.WeighBridgeStatus}");
                        _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                    }
                }

                //Wait for delay time
                await Task.Delay(_waitTime, stoppingToken);
            }
        }
    }
}