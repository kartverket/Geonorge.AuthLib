using System.Security.Claims;
using System.Threading.Tasks;
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
                    CookieManager = new SystemWebCookieManager(),
                    TokenValidationParameters = new TokenValidationParameters()
                    {
                        ValidIssuer = GetAppSetting("GeoID:Issuer")
                    },
                    Notifications = new OpenIdConnectAuthenticationNotifications()
                    {
                        SecurityTokenValidated = async (context) =>
                        {
                            context.AuthenticationTicket.Identity.AddClaim(new Claim(GeonorgeClaims.IdToken, context.ProtocolMessage.IdToken));
                            context.AuthenticationTicket.Identity.AddClaim(new Claim(GeonorgeClaims.AccessToken, context.ProtocolMessage.AccessToken));

                            var geonorgeAuthorizationService = context.OwinContext.GetAutofacLifetimeScope().Resolve<IGeonorgeAuthorizationService>();
                            context.AuthenticationTicket.Identity.AddClaims(await geonorgeAuthorizationService.GetClaims(context.AuthenticationTicket.Identity));
                        },
                        AuthenticationFailed = (context) =>
                        {
                            // handle IDX21323 exception
                            if (context.Exception.Message.Contains("IDX21323"))
                            {
                                context.HandleResponse();
                                var url = context.Request.Uri.ToString(); 
                                context.OwinContext.Response.Redirect(url);
                            }
                            else
                            {
                                context.HandleResponse();
                                context.Response.Redirect("/?errormessage=" + context.Exception.Message);
                            }
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