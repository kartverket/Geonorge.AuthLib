# Geonorge.AuthLib
Autentisering og autoriseringsbibliotek for applikasjoner i Geonorge

Biblioteket har tre deler:

* Geonorge.AuthLib.Common
  * Felles logikk og hjelpeklasser for uthenting av brukerinformasjon fra BaatAuthz-apiet.
* Geonorge.AuthLib.NetCore
  * Gjenbrukbar konfigurasjon for .net core applikasjoner
* Geonorge.AuthLib.NetFull
  * Gjenbrukbar konfigurasjon for .net framework applikasjoner


## Bruk av biblioteket i en .net framework applikasjon

De fleste av Geonorges applikasjoner benytter Autofac for dependency injection. Derfor benytter også dette biblioteket Autofac. 

### Installer disse pakkene i prosjektet: 

```
Install-Package Geonorge.AuthLib.NetFull
Install-Package Microsoft.Owin.Host.SystemWeb
Install-Package Autofac.Mvc5.Owin
``` 

Geonorge.AuthLib.NetFull ligger tilgjengelig som en nuget-pakke på byggeserveren til Geonorge. 

### Opprett en Startup.cs
I prosjektrota må det være en Startup.cs som konfigurere Autofac og Geonorge.AuthLib.

Eksempel fra MetadataEditoren: 
```
using Autofac;
using Geonorge.AuthLib.NetFull;
using Kartverket.MetadataEditor.App_Start;
using Microsoft.Owin;
using Owin;

[assembly: OwinStartup(typeof(Kartverket.MetadataEditor.Startup))]

namespace Kartverket.MetadataEditor
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            // Use Autofac as an Owin middleware
            var container = DependencyConfig.Configure(new ContainerBuilder());
            app.UseAutofacMiddleware(container);
            app.UseAutofacMvc();  // requires Autofac.Mvc5.Owin nuget package installed
            
            app.UseGeonorgeAuthentication();
        }
       
    }
}
```

Det er viktig at Autofac settes opp som [Owin Middleware](https://github.com/aspnet/AspNetKatana/wiki). Dette gjør vi for å kunne få tak i tjenesteklasser i løpet av autentiseringen av brukeren. 

### Konfigurasjon av Autofac

Autofac må ha beskjed om hvordan klassene til dette biblioteket skal instansieres. Dette gjøres ved å registrere en egen Autofac-modul i oppstarten. 

```
  builder.RegisterModule<GeonorgeAuthenticationModule>();
```

```builder``` er et objekt av typen Autofac.ContainerBuilder. I veldig mange av Geonorge-prosjektene har vi en egen DependencyConfig-klasse. Linjen over vil vi ofte plassere i denne klassen.

### Tilrettelegge for bruk av netstandard2.0

Ordinære .net framework applikasjoner kan få en feilmelding om at *System.Object is not found*. Da må du tilretteleggg for at appen kan benytte netstandard2.0. Dette gjøres ved å legge til følgende i web.config:

```
  <system.web>
    ...
    <compilation debug="true" targetFramework="4.7.2">
      <assemblies>
        <add assembly="netstandard, Version=2.0.0.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51" />
      </assemblies>
    </compilation>
    ...
  </system.web>
```

### Konfigurasjonsvariabler

Følgende konfigurasjonsvariabler må være definert i appsettings.config i applikasjonen: 

```
  <add key="GeoID:ClientId" value="" />
  <add key="GeoID:ClientSecret" value="" />
  <add key="GeoID:Authority" value="" />
  <add key="GeoID:Issuer" value="" />
  <add key="GeoID:RedirectUri" value="https://xxxx/signin-oidc" />
  <add key="GeoID:PostLogoutRedirectUri" value="https://xxxx/signout-callback-oidc" />
  <add key="GeoID:MetadataAddress" value="" />
  <add key="GeoID:BaatAuthzApiUrl" value=""/>
  <add key="GeoID:BaatAuthzApiCredentials" value=""/> <!-- brukernavn og passord separert med kolon -->
```

### Oppsett av innlogging og utlogging

Vi må deretter sette opp Controller-metoder for innlogging, utlogging og en callback for når logout-operasjonen har blitt gjennomført. 

```
    public void SignIn()
    {
        var redirectUrl = Url.Action(nameof(HomeController.Index), "Home");
        HttpContext.GetOwinContext().Authentication.Challenge(new AuthenticationProperties { RedirectUri = redirectUrl },
            OpenIdConnectAuthenticationDefaults.AuthenticationType);
    }

    public void SignOut()
    {
      var redirectUri = WebConfigurationManager.AppSettings["GeoID:PostLogoutRedirectUri"];
      HttpContext.GetOwinContext().Authentication.SignOut(
          new AuthenticationProperties {RedirectUri = redirectUri},
          OpenIdConnectAuthenticationDefaults.AuthenticationType,
          CookieAuthenticationDefaults.AuthenticationType);
    }

    /// <summary>
    /// This is the action responding to /signout-callback-oidc route after logout at the identity provider
    /// </summary>
    /// <returns></returns>
    public ActionResult SignOutCallback()
    {
        return RedirectToAction(nameof(RegistersController.Index), "Registers");
    }

```

For å få utlogging til å fungere må det konfigureres en signout-callback og dette har vi standardisert til å være ruten /signout-callback-oidc på samme måte som for innlogging (/signin-callback-oidc). Denne ruten for innlogging blir levert av Openid Connect biblioteket til Microsoft.

Eksempel på konfigurert callback rute i Registeret:

RouteConfig.cs
```
  routes.MapRoute("OIDC-callback-signout", "signout-callback-oidc", new { controller = "Home", action = "SignOutCallback"});

```

## Hjelpeklasser


### ClaimsPrincipal

Biblioteket inneholder noen utvidelser av ClaimsPrincipal-klassen for å gjøre uthenting av Claims enklere.

Eksempel: 
```
string username = ClaimsPrincipal.Current.GetUsername();
string organization = ClaimsPrincipal.Current.GetOrganizationName();
```

Se eksempel på bruk av ClaimsPrincipal og ClaimsPrincipalUtility i MetadataEditoren. BaseController-klassen benytter flere av disse for å gi gode metoder som kan benyttes av de ordinære Controller-klassene. 

### GeonorgeRoles

**Geonorge.AuthLib.Common.GenorgeRoles** inneholder konstanter med rollenavn vi mottar fra **Baat**. Disse skal benyttes istedenfor "magiske strenger" rundt om i de ulike applikasjonene.

**Geonorge.AuthLib.Common.GeonorgeClaims** inneholder konstanter med navn på claims som ligger på brukeren. Benytt disse dersom du må hente ut claims - men vurder om ikke det er en hjelpemetode som kan benyttes isteden, evt implementer en ny hjelpemetode.
