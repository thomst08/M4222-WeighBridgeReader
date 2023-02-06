using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Text.Json;
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

        //URL to submit all data to
        private string _url;

        //Security details for receival API
        private string _siteId;
        private string _clientId;
        private string _secret;

        /// <summary>
        /// Constructor used to setup the service and to load all the weighbridges
        /// </summary>
        /// <param name="logger">Access to the eventlog and messages</param>
        /// <param name="configuration">Access to the config files for loading in settings</param>
        /// <exception cref="ArgumentException">Only occurs if the config file is incorrectly setup</exception>
        public Worker(ILogger<Worker> logger, IConfiguration configuration)
        {
            _logger = logger;

            _url = configuration.GetValue<string>("PostURL");

            _siteId = configuration.GetValue<string>("Security:SiteId");
            _clientId = configuration.GetValue<string>("Security:ClientId");
            _secret = configuration.GetValue<string>("Security:Secret");

            if(string.IsNullOrWhiteSpace(_siteId) || string.IsNullOrWhiteSpace(_clientId) || string.IsNullOrWhiteSpace(_secret))
            {
                throw new ArgumentException("Appsettings file is incorrectly configured, missing security settings");
            }

            List<WeighBridgeSettings> weighBridges = configuration.GetSection("WeighBridges").Get<List<WeighBridgeSettings>>();

            if(weighBridges == null)
            {
                throw new ArgumentException("Appsettings file is incorrectly configured, does not have any weighbridges setup");
            }
 
            foreach(WeighBridgeSettings wb in weighBridges)
            {
                if (wb.IP == string.Empty || wb.Port == 0 || wb.WeighbridgeName == string.Empty)
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
                        _ = Task.Run(async () => await SendWeightInformationAsync(wb.ExtractPostData(), _logger));

#if DEBUG
                        _logger.LogInformation("Posting data");
                        _logger.LogInformation($"WeighBridge: Weight - {wb.LastReadValue} - Status - {wb.WeighBridgeStatus}");
                        _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
#endif
                    }
                }

                //Wait for delay time
                await Task.Delay(_waitTime, stoppingToken);
            }
        }


        /// <summary>
        /// Function used to send weight information to URL
        /// </summary>
        /// <param name="postData">Weigh information to send</param>
        /// <param name="logger"></param>
        /// <returns></returns>
        private async Task SendWeightInformationAsync(WeighbridgePOSTData postData, ILogger<Worker> logger)
        {
            HttpClient client = new HttpClient();
            try
            {
                string jsonString = JsonSerializer.Serialize(postData);
                HttpContent content = new StringContent(jsonString, UnicodeEncoding.UTF8, "application/json");

                //Add the security details
                content.Headers.Add("SiteId", _siteId);
                content.Headers.Add("ClientId", _clientId);
                content.Headers.Add("Secret", _secret);

                client.Timeout = TimeSpan.FromSeconds(4);

                HttpResponseMessage temp = await client.PostAsync(_url, content);
                if (temp != null)
                {
                    if (temp.StatusCode != HttpStatusCode.NoContent)
                        logger.LogError($"Issue communications with API, received '{temp.StatusCode}' error code at {DateTime.Now}");
#if DEBUG
                    else
                        logger.LogInformation("Received HTTP code 204 back from API");
#endif
                }
            }
            catch(Exception ex)
            {
                logger.LogError($"Error Posting to URL: {ex.Message}");
            }
            
            client.Dispose();
        }
    }
}