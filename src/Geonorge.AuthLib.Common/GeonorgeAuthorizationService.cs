using System;
using System.Collections.Generic;
using System.Linq;
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

        public const string ClaimIdentifierRole = "http://schemas.microsoft.com/ws/2008/06/identity/claims/role";
        public const string ClaimIdentifierUsername = "preferred_username";
        private const string GeonorgeRoleNamePrefix = "nd.";
        
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
                
                //AppendFakeRolesForDemoUser(usernameClaim.Value, claims); // TODO: Remove when BaatAuthz can supply proper role list
            }

            return claims;
        }

        private async Task AppendRoles(string username, List<Claim> claims)
        {
            BaatAuthzUserRolesResponse response = await _baatAuthzApi.GetRoles(username);
            
            response.Services
                .Where(role => role.StartsWith(GeonorgeRoleNamePrefix))
                .ToList()
                .ForEach(role => claims.Add(new Claim(ClaimIdentifierRole, role)));
        }

        private void AppendFakeRolesForDemoUser(string username, List<Claim> claims)
        {
            if (username == "esk_jenhen")
            {
                claims.Add(new Claim(ClaimIdentifierRole, GeonorgeRoles.MetadataAdmin));
                claims.Add(new Claim(ClaimIdentifierRole, GeonorgeRoles.MetadataEditor));
            }
        }
    }
}