using System.Security.Claims;
using System.Threading.Tasks;
using Kartverket.MetadataEditor.Util;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.OpenIdConnect;
using Owin;

namespace Geonorge.AuthLib.NetFull
{
    public partial class Startup
    {
        public void ConfigureGeonorgeAuth(IAppBuilder app)
        {
            app.SetDefaultSignInAsAuthenticationType(CookieAuthenticationDefaults.AuthenticationType);

            app.UseCookieAuthentication(new CookieAuthenticationOptions());
            app.UseOpenIdConnectAuthentication(
                new OpenIdConnectAuthenticationOptions
                {
                    ClientId = GetAppSetting("GeoID:ClientId"),
                    ClientSecret = GetAppSetting("GeoID:ClientSecret"),
                    MetadataAddress = GetAppSetting("GeoID:MetadataAddress"),
                    Authority = GetAppSetting("GeoID:Authority"),
                    RedirectUri = GetAppSetting("GeoID:RedirectUri"),
                    PostLogoutRedirectUri = GetAppSetting("GeoID:RedirectUri"),
                    Scope = OpenIdConnectScope.OpenId,
                    ResponseType = OpenIdConnectResponseType.CodeIdTokenToken,
                    ProtocolValidator = new GeonorgeGeoidProtocolValidator() { RequireState = false, RequireStateValidation = false},
                    Notifications = new OpenIdConnectAuthenticationNotifications()
                        {
                            MessageReceived = (context) =>
                            {
                                //Log.Info("*** MessageReceived");
                                return Task.FromResult(0);
                            },
                            SecurityTokenReceived = (context) =>
                            {
                                //Log.Info("*** SecurityTokenReceived");
                                //Log.Info(Json.Encode(context.ProtocolMessage));
                                return Task.FromResult(0);
                            },
                            SecurityTokenValidated = (context) =>
                            {
                                //Log.Info("*** SecurityTokenValidated");
                                //Log.Info(Json.Encode(context.ProtocolMessage));
                                context.AuthenticationTicket.Identity.AddClaim(new Claim("id_token",context.ProtocolMessage.IdToken));
                                context.AuthenticationTicket.Identity.AddClaim(new Claim("access_token",context.ProtocolMessage.AccessToken));
                                return Task.FromResult(0);
                            },
                            AuthorizationCodeReceived = (context) =>
                            {                            
                                //Log.Info("*** AuthorizationCodeReceived");
                                //Log.Info(Json.Encode(context.ProtocolMessage));
                                return Task.FromResult(0);
                            },
                            AuthenticationFailed = (context) =>
                            {
                                //Log.Info("*** AuthenticationFailed");
                                //Log.Info(Json.Encode(context.ProtocolMessage));
                                return Task.FromResult(0);
                            },
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