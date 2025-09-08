using System;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Geonorge.AuthLib.Common.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Geonorge.AuthLib.Common
{
    public interface IBaatAuthzApi
    {
        /// <summary>
        /// returns information about a user, e.g. name and organization
        /// </summary>
        /// <param name="username"></param>
        Task<BaatAuthzUserInfoResponse> Info(string username);

        Task<BaatAuthzUserRolesResponse> GetRoles(string username);
    }

    /// <summary>
    /// Implementation of the BaatAuthzApi. Returns authorization information about BAAT users. 
    /// </summary>
    public class BaatAuthzApi : IBaatAuthzApi
    {
        private static readonly ILog Log = LogProvider.For<BaatAuthzApi>();
        private readonly ILogger<BaatAuthzApi> _logger;

        private readonly IHttpClientFactory _httpClientFactory;

        private static HttpClient _httpClient;
        
        private readonly string _apiUrl;
        private readonly string _apiCredentials;

        /// <summary>
        /// Initialize the BaatAuthzApi. Will use HttpClientFactory for api communications.
        /// </summary>
        /// <param name="logger">logger instance for logging</param>
        /// <param name="config">
        /// Required config: API URL as well as basic auth (username:password)
        /// <br/>Requires API URL in appsettings.json: Urls:BaatAuthzApi
        /// <br/>Requires basic auth credentials in appsettings.json: BaatAuthzApiCredentials
        /// </param>
        /// <param name="httpClientFactory">provider of http clients used for communicating with BAAT</param>
        public BaatAuthzApi(ILogger<BaatAuthzApi> logger, IConfiguration config, IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _apiUrl = config["auth:baat:BaatAuthzApiUrl"];
            _apiCredentials = config["auth:baat:BaatAuthzApiCredentials"];
            _httpClientFactory = httpClientFactory;
        }

        /// <summary>
        /// Initialize the BaatAuthzApi. Will use static HttpClient for api communications.
        /// </summary>
        /// <param name="apiUrl">the full url to the BAAT api endpoint</param>
        /// <param name="apiCredentials">Http basic authentication params e.g. username:password </param>
        public BaatAuthzApi(string apiUrl, string apiCredentials)
        {
            _apiUrl = apiUrl;
            _apiCredentials = apiCredentials;
        }
        
        /// <summary>
        /// Returns information about a user, e.g. name and organization
        /// </summary>
        /// <param name="username"></param>
        public async Task<BaatAuthzUserInfoResponse> Info(string username)
        {
            var url = $"{_apiUrl}authzinfo/{username}";
            if (_logger != null)
                _logger.LogDebug("Fetching data from {url}", url);
            else
                Log.Debug("Fetching data from {url}", url);
            
            var res = await GetClient().GetAsync(url);

            if (!res.IsSuccessStatusCode)
            {
                if (_logger != null)
                    _logger.LogError("Looking up {user} from BaatAuthzApi failed with status code: {code}", username, res.StatusCode);
                else
                    Log.Error("Looking up {user} from BaatAuthzApi failed with status code: {code}", username, res.StatusCode);
                return BaatAuthzUserInfoResponse.Empty;
            }
            
            var json = await res.Content.ReadAsStringAsync();
            
            if (_logger != null)
                _logger.LogDebug("Response from BaatAuthzApi: {json}", json);
            else
                Log.Debug("Response from BaatAuthzApi: {json}", json);
            
            return JsonConvert.DeserializeObject<BaatAuthzUserInfoResponse>(json);
        
        }

        public async Task<BaatAuthzUserRolesResponse> GetRoles(string username)
        {
            var url = $"{_apiUrl}authzlist/{username}";
            if (_logger != null)
                _logger.LogDebug("Fetching data from {url}", url);
            else
                Log.Debug("Fetching data from {url}", url);

            var stopwatch = new Stopwatch();
            stopwatch.Start();
            var res = await GetClient().GetAsync(url);
            stopwatch.Stop();
            
            if (_logger != null)
                _logger.LogInformation("Http call to {url} with response code {statuscode} executed in {millis}", url, res.StatusCode, stopwatch.ElapsedMilliseconds);
            else
                Log.Info("Http call to {url} with response code {statuscode} executed in {millis}", url, res.StatusCode, stopwatch.ElapsedMilliseconds);
            
            if (!res.IsSuccessStatusCode)
            {
                if (_logger != null)
                    _logger.LogError("Looking up {user} from BaatAuthzApi failed with status code: {code}", username, res.StatusCode);
                else
                    Log.Error("Looking up {user} from BaatAuthzApi failed with status code: {code}", username, res.StatusCode);
                return BaatAuthzUserRolesResponse.Empty;
            }
            
            var json = await res.Content.ReadAsStringAsync();

            if (_logger != null)
                _logger.LogDebug("Response from BaatAuthzApi: {json}", json);
            else
                Log.Debug("Response from BaatAuthzApi: {json}", json);
            
            return JsonConvert.DeserializeObject<BaatAuthzUserRolesResponse>(json);

        }

        private HttpClient GetClient()
        {
            HttpClient client = null;
            if (_httpClientFactory != null)
            {
                client = _httpClientFactory.CreateClient();
            } else {
                if (_httpClient == null)
                    _httpClient = new HttpClient();
                client = _httpClient;
            }
            
            if (_logger != null)
                _logger.LogDebug("Connecting to BaatAuthzApi with credentials: {credentials}", _apiCredentials);
            else
                Log.Debug("Connecting to BaatAuthzApi with credentials: {credentials}", _apiCredentials);
            
            var byteArray = Encoding.ASCII.GetBytes(_apiCredentials);
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));
            return client;
        }

    }
}