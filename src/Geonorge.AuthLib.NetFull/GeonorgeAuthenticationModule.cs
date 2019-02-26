using System.Collections.Generic;
using System.Configuration;
using Autofac;
using Autofac.Core;
using Geonorge.AuthLib.Common;

namespace Geonorge.AuthLib.NetFull
{
    /// <summary>
    /// Autofac module for setting up dependencies for Geonorge authentication
    /// </summary>
    public class GeonorgeAuthenticationModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<GeonorgeAuthorizationService>().As<IGeonorgeAuthorizationService>();
            builder.RegisterType<BaatAuthzApi>()
                .As<IBaatAuthzApi>()
                .WithParameters(new List<Parameter>
                {
                    new NamedParameter("apiUrl", ConfigurationManager.AppSettings["GeoID:BaatAuthzApiUrl"]),
                    new NamedParameter("apiCredentials", ConfigurationManager.AppSettings["GeoID:BaatAuthzApiCredentials"])
                });
        }
    }
}