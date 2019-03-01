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

### Tilrettelegge for bruk av netstandard2.0

Hvis du får feilmelding om at *System.Object is not found* må du legge til følgende i web.config:

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
  <add key="GeoID:RedirectUri" value="" />
  <add key="GeoID:MetadataAddress" value="" />
  <add key="GeoID:BaatAuthzApiUrl" value=""/>
  <add key="GeoID:BaatAuthzApiCredentials" value=""/>
```

### Oppsett av innlogging og utlogging

Vi må deretter sette opp Controller-metoder for innlogging og utlogging. 

```
    public void SignIn()
    {
        var redirectUrl = Url.Action(nameof(HomeController.Index), "Home");
        HttpContext.GetOwinContext().Authentication.Challenge(new AuthenticationProperties { RedirectUri = redirectUrl },
            OpenIdConnectAuthenticationDefaults.AuthenticationType);
    }

    public void SignOut()
    {
        HttpContext.GetOwinContext().Authentication.SignOut(
            OpenIdConnectAuthenticationDefaults.AuthenticationType,
            CookieAuthenticationDefaults.AuthenticationType);
    }
```

## Hjelpeklasser


### ClaimsPrincipal

Biblioteket inneholder noen utvidelser av ClaimsPrincipal-klassen for å gjøre uthenting av Claims enklere.

Eksempel: 
```
string username = ClaimsPrincipal.Current.GetUsername();
string organization = ClaimsPrincipal.Current.GetOrganizationName();
```

Se eksempel på bruk av ClaimsPrincipal og ClaimsPrincipalUtility i MetadataEditoren. 

### GeonorgeRoles

**Geonorge.AuthLib.Common.GenorgeRoles** inneholder konstanter med rollenavn vi mottar fra **Baat**. Disse skal benyttes istedenfor "magiske strenger" rundt om i de ulike applikasjonene.
