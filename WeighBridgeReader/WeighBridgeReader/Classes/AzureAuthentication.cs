using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using static System.Formats.Asn1.AsnWriter;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Security.Principal;
using System.Net.Http;

namespace WeighBridgeReader.Classes
{
    internal class AzureAuthentication
    {
        /// <summary>
        /// Class used to hold authentication information form Microsoft
        /// </summary>
        private class TokenDetails
        {
            private string _tokenType = string.Empty;
            private string _accessToken = string.Empty;


            [JsonPropertyName("token_type")]
            public string TokenType { get => _tokenType; set => _tokenType = value; }

            [JsonPropertyName("expires_in")]
            public int ExpiresIn { get; set; }

            [JsonPropertyName("ext_expires_in")]
            public int ExtExpiresIn { get; set; }

            [JsonPropertyName("access_token")]
            public string AccessToken { get => _accessToken; set => _accessToken = value; }
        }

        /// <summary>
        /// Used to limit the number of retries when fetching authentication
        /// </summary>
        private const int RetryCount = 3;

        /// <summary>
        /// URL used to talk to Microsoft authentication servers
        /// </summary>
        private readonly string _authenticationURL;

        //Details to hold each piece of data needed to get a token
        private readonly string _clientId;
        private readonly string _clientSecret;
        private readonly string _scope;
        private readonly string _grantType;

        private ILogger<Worker> _logger;

        //Current token and expire time
        private TokenDetails _token = new TokenDetails();
        private DateTime _endTime = DateTime.Now;


        /// <summary>
        /// Pulbic access to get the latest token on request
        /// </summary>
        public async Task<string> TokenAsync()
        {
            if (TokenValid())
                await UpdateToken();

            return _token.TokenType + " " + _token.AccessToken;
        }


        public AzureAuthentication(ILogger<Worker> logger, string authenticationURL, string clientId, string clientSecret, string scope)
        {
            _logger = logger;

            _authenticationURL = authenticationURL;

            _grantType = "client_credentials";
            _clientId = clientId;
            _clientSecret = clientSecret;
            _scope = scope;
        }


        /// <summary>
        /// Retrieves the latest token, trys 3 times if it fails
        /// </summary>
        /// <exception cref="Exception">Throws an error only if a token was not returned</exception>
        private async Task<bool> UpdateToken()
        {
            bool result;
            int i = RetryCount;
            do
            {
                i--;
                result = await RetreveAuthenticationToken();
            } while (!result || i <= 0);
            
            if(!result)
                throw new Exception("Unable to retrive token details");

            return true;
        }


        /// <summary>
        /// Retreives the latest Authentication Token on request
        /// </summary>
        /// <returns></returns>
        private async Task<bool> RetreveAuthenticationToken()
        {
            try
            {
                //Setup the authentication details
                List<KeyValuePair<string, string>> contentData = new List<KeyValuePair<string, string>> {
                    new KeyValuePair<string, string>("grant_type", _grantType),
                    new KeyValuePair<string, string>("client_id", _clientId),
                    new KeyValuePair<string, string>("client_secret", _clientSecret),
                    new KeyValuePair<string, string>("scope", _scope)
                };

                HttpClient httpContent = new HttpClient();
                FormUrlEncodedContent encodedContent = new FormUrlEncodedContent(contentData);
                encodedContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/x-www-form-urlencoded");

                HttpResponseMessage response = await httpContent.PostAsync(_authenticationURL, encodedContent);
                string receivedContent = await response.Content.ReadAsStringAsync();

                _token = JsonSerializer.Deserialize<TokenDetails>(receivedContent)!;
                _endTime = DateTime.Now.AddSeconds(_token.ExpiresIn).AddMinutes(-5);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error retreiving authentication token: {ex.Message}");
                return false;
            }

#if DEBUG
            _logger.LogInformation("Success generating Token");
#endif
            return true;
        }


        /// <summary>
        /// Check if the time has expired or if there is an issue with the token
        /// </summary>
        /// <returns></returns>
        private bool TokenValid()
        {
            if(_token == null || _token == new TokenDetails())
                return false;

            return _token.AccessToken == string.Empty || _token.TokenType == string.Empty || _endTime <= DateTime.Now;
        }
    }
}
