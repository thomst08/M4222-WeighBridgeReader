using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Net.Http.Headers;
using Microsoft.Extensions.Configuration;
using WeighBridgeReader.Classes.WeighBridge;
using WeighBridgeReader.Classes;
using static System.Formats.Asn1.AsnWriter;
using Microsoft.Extensions.Logging;
using System.Xml.Linq;


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
        private int _timeout = 10;

        //Hold authention details for microsoft servers
        AzureAuthentication _authention;

        private readonly string _enviromentName;
        private readonly string _weighbridgeTableName;


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
            int i = configuration.GetValue<int>("PostTimeoutSeconds");
            if (i != 0 || i > 0)
                _timeout = i;

            _enviromentName = configuration.GetValue<string>("EnviromentTableName");
            _weighbridgeTableName = configuration.GetValue<string>("WeighbridgeTableName");

            //Load all authentication details
            string authenticationURL = configuration.GetValue<string>("Security:AuthenticationURL");
            string clientId = configuration.GetValue<string>("Security:ClientId");
            string clientSecret = configuration.GetValue<string>("Security:ClientSecret");
            string scope = configuration.GetValue<string>("Security:Scope");

            if(string.IsNullOrWhiteSpace(authenticationURL) || string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret) || string.IsNullOrWhiteSpace(scope))
                throw new ArgumentException("Appsettings file is incorrectly configured, missing security settings");

            _authention = new AzureAuthentication(logger, authenticationURL, clientId, clientSecret, scope);


            List<WeighBridgeSettings> weighBridges = configuration.GetSection("WeighBridges").Get<List<WeighBridgeSettings>>();

            if(weighBridges == null)
                throw new ArgumentException("Appsettings file is incorrectly configured, does not have any weighbridges setup");


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
                        _ = Task.Run(async () => await SendWeightInformationAsync(wb.ExtractPostData()));

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
        /// Sends the weight information to Dataverse servers
        /// </summary>
        /// <param name="postData">Weight information read from bridge</param>
        /// <returns></returns>
        private async Task SendWeightInformationAsync(WeighbridgePOSTData postData)
        {
            string tokenType = string.Empty;
            string token = string.Empty;

            //Fetch authencation token details for dataverse
            try
            {
                (tokenType, token) = _authention.TokenAsync().Result;
            }
            catch
            {
                return;
            }

            //Setup and send data to Dataverse
            HttpClient client = new HttpClient();
            try
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(tokenType, token);

                HttpContent content = new StringContent(postData.JsonSerialize(_enviromentName, _weighbridgeTableName), UnicodeEncoding.UTF8, "application/json");
                content.Headers.Add("OData-MaxVersion", "4.0");
                content.Headers.Add("OData-Version", "4.0");
                content.Headers.TryAddWithoutValidation("If-None-Match", "null");

                client.Timeout = TimeSpan.FromSeconds(_timeout);
                HttpResponseMessage received = await client.PostAsync(_url, content);
                if (received != null)
                {
                    if (received.StatusCode != HttpStatusCode.NoContent && received.StatusCode != HttpStatusCode.Accepted)
                        _logger.LogError($"Issue communications with API, received '{received.StatusCode}' error code at {DateTime.Now}");
#if DEBUG
                    else
                        _logger.LogInformation($"Received HTTP '{received.StatusCode}' back from API");
#endif
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error Posting results to URL: {ex.Message}");
            }

            client.Dispose();
        }
    }
}