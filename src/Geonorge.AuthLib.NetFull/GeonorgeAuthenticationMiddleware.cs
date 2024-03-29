using System;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web.Configuration;
using Autofac;
using Autofac.Integration.Owin;
using Geonorge.AuthLib.Common;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Owin.Host.SystemWeb;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.OpenIdConnect;
using Owin;

namespace Geonorge.AuthLib.NetFull
{
    public static class GeonorgeAuthenticationMiddleware
    {
        /// <summary>
        /// Configure Geonorge authentication with GeoID and BaatAuthzApi.
        /// Required AppSettings:
        /// * GeoID:ClientId
        /// * GeoID:ClientSecret
        /// * GeoID:MetadataAddress
        /// * GeoID:Authority
        /// * GeoID:RedirectUri
        /// * GeoID:Issuer
        /// * GeoID:BaatAuthzApiUrl
        /// * GeoID:BaatAuthzApiCredentials
        /// </summary>
        /// <param name="app"></param>
        public static void UseGeonorgeAuthentication(this IAppBuilder app)
        {
            app.SetDefaultSignInAsAuthenticationType(CookieAuthenticationDefaults.AuthenticationType);
            
            app.UseCookieAuthentication(new CookieAuthenticationOptions());

            _ = app.UseOpenIdConnectAuthentication(
                openIdConnectOptions: new OpenIdConnectAuthenticationOptions
                {
                    ClientId = GetAppSetting("GeoID:ClientId"),
                    ClientSecret = GetAppSetting("GeoID:ClientSecret"),
                    MetadataAddress = GetAppSetting("GeoID:MetadataAddress"),
                    Authority = GetAppSetting("GeoID:Authority"),
                    RedirectUri = GetAppSetting("GeoID:RedirectUri"),
                    PostLogoutRedirectUri = GetAppSetting("GeoID:RedirectUri"),
                    Scope = OpenIdConnectScope.OpenId,
                    ResponseType = OpenIdConnectResponseType.CodeIdTokenToken,
                    TokenValidationParameters = new TokenValidationParameters()
                    {
                        ValidIssuer = GetAppSetting("GeoID:Issuer")
                    },
                    Notifications = new OpenIdConnectAuthenticationNotifications()
                    {
                        SecurityTokenValidated = async (context) =>
                        {
                            var sessionSection = (SessionStateSection)WebConfigurationManager.GetSection("system.web/sessionState");
                            context.AuthenticationTicket.Properties.ExpiresUtc = DateTimeOffset.UtcNow + sessionSection.Timeout;
                            context.AuthenticationTicket.Properties.AllowRefresh = true;

                            context.AuthenticationTicket.Identity.AddClaim(new Claim(GeonorgeClaims.IdToken, context.ProtocolMessage.IdToken));
                            context.AuthenticationTicket.Identity.AddClaim(new Claim(GeonorgeClaims.AccessToken, context.ProtocolMessage.AccessToken));

                            var geonorgeAuthorizationService = context.OwinContext.GetAutofacLifetimeScope().Resolve<IGeonorgeAuthorizationService>();
                            context.AuthenticationTicket.Identity.AddClaims(await geonorgeAuthorizationService.GetClaims(context.AuthenticationTicket.Identity));
                        },
                        AuthenticationFailed = (context) =>
                        {
                            context.HandleResponse();
                            context.Response.Redirect("/?errormessage=" + context.Exception.Message);
                            return Task.FromResult(0);
                        },
                        RedirectToIdentityProvider = context =>
                        {
                            if (context.ProtocolMessage.RequestType == OpenIdConnectRequestType.Logout)
                            {
                                var idToken = context.OwinContext.Authentication.User.FindFirst(GeonorgeClaims.IdToken);
                                if (idToken != null)
                                {
                                    var idTokenHint = idToken.Value;
                                    context.ProtocolMessage.IdTokenHint = idTokenHint;
                                }
                            }
                            return Task.CompletedTask;
                        }
                    }
                }
            );
            
        }
        
        private static string GetAppSetting(string paramName)
        {
            return System.Configuration.ConfigurationManager.AppSettings[paramName];
        }
    }
}