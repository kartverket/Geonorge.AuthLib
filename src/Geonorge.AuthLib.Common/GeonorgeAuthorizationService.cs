using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Geonorge.AuthLib.Common.Logging;

namespace Geonorge.AuthLib.Common
{
    /// <summary>
    /// Retrieves information from Geonorges authorization service, also known as BAAT.
    /// </summary>
    public class GeonorgeAuthorizationService : IGeonorgeAuthorizationService
    {
        private static readonly ILog Log = LogProvider.For<GeonorgeAuthorizationService>(); 

        private readonly IBaatAuthzApi _baatAuthzApi;

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
            Claim usernameClaim =
                identity.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier");

            BaatAuthzUserInfoResponse response = await _baatAuthzApi.Info(usernameClaim.Value);

            var claims = new List<Claim>();

            if (response == BaatAuthzUserInfoResponse.Empty)
                Log.Warn("Empty response from BaatAuthzApi - no claims appended to user");
            else
            {
                
                claims.AddRange(new List<Claim>
                {
                    new Claim("Name", string.IsNullOrEmpty(response?.Name) ? "" : response.Name),
                    new Claim("Email", response.Email),
                    new Claim("AuthorizedFrom", response.AuthorizedFrom),
                    new Claim("AuthorizedUntil", response.AuthorizedUntil),
                    new Claim("OrganizationName", response.Organization?.Name),
                    new Claim("OrganizationOrgnr", response.Organization?.Orgnr),
                    new Claim("OrganizationContactName", response.Organization?.ContactName),
                    new Claim("OrganizationContactEmail", response.Organization?.ContactEmail),
                    new Claim("OrganizationContactPhone", response.Organization?.ContactPhone)
                });

                AppendFakeRolesForDemoUser(usernameClaim.Value, claims); // TODO: Remove when BaatAuthz can supply proper role list
            }

            return claims;
        }

        private void AppendFakeRolesForDemoUser(string username, List<Claim> claims)
        {
            if (username == "esk_jenhen")
            {
                claims.Add(new Claim("http://schemas.microsoft.com/ws/2008/06/identity/claims/role", GeonorgeRoles.MetadataAdmin));
                claims.Add(new Claim("http://schemas.microsoft.com/ws/2008/06/identity/claims/role", GeonorgeRoles.MetadataEditor));
            }
        }
    }
}