using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Geonorge.AuthLib.Common.Logging;

namespace Geonorge.AuthLib.Common
{
    public interface IBaatAuthzApi
    {
        /// <summary>
        /// returns information about a user, e.g. name and organization
        /// </summary>
        /// <param name="username"></param>
        Task<BaatAuthzUserInfoResponse> Info(string username);
    }

    /// <summary>
    /// Implementation of the BaatAuthzApi. Returns authorization information about BAAT users. 
    /// </summary>
    public class BaatAuthzApi : IBaatAuthzApi
    {
        private static readonly ILog Log = LogProvider.For<BaatAuthzApi>();

        private readonly IHttpClientFactory _httpClientFactory;

        private static HttpClient _httpClient;
        
        private readonly string _apiUrl;
        private readonly string _apiCredentials;
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="apiUrl">the full url to the BAAT api endpoint</param>
        /// <param name="apiCredentials">Http basic authentication params e.g. username:password </param>
        /// <param name="httpClientFactory">provider of http clients used for communicating with BAAT</param>
        public BaatAuthzApi(string apiUrl, string apiCredentials, IHttpClientFactory httpClientFactory)
        {
            _apiUrl = apiUrl;
            _apiCredentials = apiCredentials;
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
            Log.Debug("Fetching data from {url}", url);
            
            var res = await GetClient().GetAsync(url);

            if (!res.IsSuccessStatusCode)
            {
                Log.Error("Looking up {user} from BaatAuthzApi failed with status code: {code}", username, res.StatusCode);
                return BaatAuthzUserInfoResponse.Empty;
            }
            
            var json = await res.Content.ReadAsStringAsync();
            
            Log.Debug("Response from BaatAuthzApi: {json}", json);
            
            return JsonConvert.DeserializeObject<BaatAuthzUserInfoResponse>(json);
        
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
            
            Log.Debug("Connecting to BaatAuthzApi with credentials: {credentials}", _apiCredentials);
            
            var byteArray = Encoding.ASCII.GetBytes(_apiCredentials);
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));
            return client;
        }

    }
}