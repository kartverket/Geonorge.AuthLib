using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace Kartverket.MetadataEditor.Util
{
    public class GeonorgeGeoidProtocolValidator : OpenIdConnectProtocolValidator
    {
        /// <summary>
        /// GeoID does not return at_hash when using responsetype=code id_token token. Skipping validation of hash.
        /// </summary>
        /// <param name="validationContext"></param>
        protected override void ValidateAtHash(OpenIdConnectProtocolValidationContext validationContext)
        {
            //base.ValidateAtHash(validationContext);
        }

        /// <summary>
        /// GeoID does not return c_hash when using responsetype=code id_token token. Skipping validation of hash.
        /// </summary>
        /// <param name="validationContext"></param>
        protected override void ValidateCHash(OpenIdConnectProtocolValidationContext validationContext)
        {
            //base.ValidateCHash(validationContext);
        }
    }
}