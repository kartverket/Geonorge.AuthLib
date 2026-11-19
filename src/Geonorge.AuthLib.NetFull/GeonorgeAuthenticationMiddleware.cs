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
        /// Optional AppSettings:
        /// * GeoID:CookieName - name of the authentication cookie. Set a unique value for applications
        ///   that share host name with other applications (e.g. sub applications under /register),
        ///   otherwise they overwrite each other's authentication cookie.
        /// </summary>
        /// <param name="app"></param>
        public static void UseGeonorgeAuthentication(this IAppBuilder app)
        {
            app.SetDefaultSignInAsAuthenticationType(CookieAuthenticationDefaults.AuthenticationType);
            
            var cookieOptions = new CookieAuthenticationOptions
            {
                CookieManager = new SystemWebChunkingCookieManager()
            };
            var cookieName = GetAppSetting("GeoID:CookieName");
            if (!string.IsNullOrWhiteSpace(cookieName))
                cookieOptions.CookieName = cookieName;

            app.UseCookieAuthentication(cookieOptions);

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
                    CookieManager = new NonceCleanupCookieManager(),
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
                            if (context.ProtocolMessage.RequestType == OpenIdConnectRequestType.Authentication
                                && !RequestUtility.IsBrowserNavigation(context.Request))
                            {
                                // AJAX/fetch/resource requests cannot complete a login, keep the 401 instead of
                                // redirecting (no nonce cookie is written for these, see NonceCleanupCookieManager).
                                context.HandleResponse();
                                return Task.CompletedTask;
                            }

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