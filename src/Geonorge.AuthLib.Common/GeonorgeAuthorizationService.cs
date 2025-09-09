using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using Geonorge.AuthLib.Common.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Geonorge.AuthLib.Common
{
    /// <summary>
    /// Retrieves information from Geonorges authorization service, also known as BAAT.
    /// </summary>
    public class GeonorgeAuthorizationService : IGeonorgeAuthorizationService
    {
        private static readonly ILog Log = LogProvider.For<GeonorgeAuthorizationService>();
        private readonly ILogger _logger;

        public const string ClaimIdentifierRole = "http://schemas.microsoft.com/ws/2008/06/identity/claims/role";
        public const string ClaimIdentifierUsername = "preferred_username";
        private const string GeonorgeRoleNamePrefix = "nd.";
        
        private readonly IBaatAuthzApi _baatAuthzApi;
        
        private readonly IHttpClientFactory _httpClientFactory;

        private readonly (string clientid, string clientsecret, string IntrospectionUrl) _config;

        public GeonorgeAuthorizationService(ILogger<GeonorgeAuthorizationService> logger, IConfiguration config, IBaatAuthzApi baatAuthzApi, IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _config = (config["auth:oidc:ClientId"], config["auth:oidc:ClientSecret"], config["auth:oidc:IntrospectionUrl"]);
            _baatAuthzApi = baatAuthzApi;
            _httpClientFactory = httpClientFactory;
        }

        public GeonorgeAuthorizationService(IBaatAuthzApi baatAuthzApi)
        {
            _baatAuthzApi = baatAuthzApi;
        }

        /// <summary>
        ///     Returning claims for the given user from BaatAuthzApi.
        /// </summary>
        /// <param name="identity"></param>
        /// <returns></returns>
        public async Task<List<Claim>> GetClaims(ClaimsIdentity identity)
        {
            Claim usernameClaim = identity.FindFirst(ClaimIdentifierUsername);

            BaatAuthzUserInfoResponse response;
            try
            {
                response = await _baatAuthzApi.Info(usernameClaim.Value);
            }
            catch (Exception e)
            {
                throw new Exception("Error while communicating with BaatAutzApi: " + e.Message, e);
            }
            
            var claims = new List<Claim>();

            if (response != null && response == BaatAuthzUserInfoResponse.Empty)
                if (_logger != null)
                    _logger.LogWarning("Empty response from BaatAuthzApi - no claims appended to user");
                else
                    Log.Warn("Empty response from BaatAuthzApi - no claims appended to user");
            else
            {
                claims.AddRange(new List<Claim>
                {
                    new Claim(GeonorgeClaims.Name, string.IsNullOrEmpty(response?.Name) ? "" : response.Name),
                    new Claim(GeonorgeClaims.Email, response.Email),
                    new Claim(GeonorgeClaims.AuthorizedFrom, response.AuthorizedFrom),
                    new Claim(GeonorgeClaims.AuthorizedUntil, response.AuthorizedUntil),
                });

                if (response.Organization != null)
                {
                    claims.AddRange(new List<Claim>
                    {
                        new Claim(GeonorgeClaims.OrganizationName, response.Organization.Name),
                        new Claim(GeonorgeClaims.OrganizationOrgnr, response.Organization.Orgnr),
                        new Claim(GeonorgeClaims.OrganizationContactName, response.Organization.ContactName),
                        new Claim(GeonorgeClaims.OrganizationContactEmail, response.Organization.ContactEmail),
                        new Claim(GeonorgeClaims.OrganizationContactPhone, response.Organization.ContactPhone)
                    });
                }

                await AppendRoles(usernameClaim.Value, claims);
            }

            return claims;
        }

        public async Task<string> GetUserNameFromIntrospection(string token)
        {
            var authToken = token?.Replace("Bearer ", "");

            string username;

            var formUrlEncodedContent = new FormUrlEncodedContent(new[] {
                new KeyValuePair<string, string>("token", authToken),
                new KeyValuePair<string, string>("client_id", _config.clientid),
                new KeyValuePair<string, string>("client_secret", _config.clientsecret)
            }
            );

            try
            {
                var client = _httpClientFactory.CreateClient();

                var request = new HttpRequestMessage(HttpMethod.Post, _config.IntrospectionUrl);
                request.Content = formUrlEncodedContent;
                var response = await client.SendAsync(request);
                var result = await response.Content.ReadAsStringAsync();

                using (JsonDocument doc = JsonDocument.Parse(result))
                {
                    JsonElement root = doc.RootElement;

                    bool isActiveToken = root.TryGetProperty("active", out JsonElement activeElement)
                                         && activeElement.GetBoolean();

                    if (isActiveToken)
                    {
                        if (root.TryGetProperty("username", out JsonElement usernameElement))
                        {
                            return usernameElement.GetString();
                        }
                    }
                }

                if (_logger != null)
                    _logger.LogError($"Could not get user info from token. Token is not active.");
                else
                    Log.Error($"Could not get user info from token.");
                return null;

            }
            catch (Exception exception)
            {
                if (_logger != null)
                    _logger.LogError(exception, $"Could not get user info from token.");
                else
                    Log.Error(exception, $"Could not get user info from token.");
                return null;
            }

        }

        private async Task AppendRoles(string username, List<Claim> claims)
        {
            BaatAuthzUserRolesResponse response = await _baatAuthzApi.GetRoles(username);

            if (response.Services != null)
            {
                response.Services
                    .Where(role => role.StartsWith(GeonorgeRoleNamePrefix))
                    .ToList()
                    .ForEach(role => claims.Add(new Claim(ClaimIdentifierRole, role)));
            }
        }
    }
}