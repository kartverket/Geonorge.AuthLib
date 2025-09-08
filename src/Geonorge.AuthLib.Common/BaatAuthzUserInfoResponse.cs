using System.Text.Json.Serialization;

namespace Geonorge.AuthLib.Common
{
    public class BaatAuthzUserInfoResponse
    {
        public static readonly BaatAuthzUserInfoResponse Empty = new BaatAuthzUserInfoResponse();

        public string User { get; set; }

        public BaatAuthzUserInfoOrganization Organization { get; set; }

        public string Name { get; set; }

        public string Email { get; set; }

        [JsonPropertyName("authorized_from")]
        public string AuthorizedFrom { get; set; }

        [JsonPropertyName("authorized_until")]
        public string AuthorizedUntil { get; set; }
    }

    public class BaatAuthzUserInfoOrganization
    {
        public string Name { get; set; }

        public string Orgnr { get; set; }

        [JsonPropertyName("contact_name")]
        public string ContactName { get; set; }

        [JsonPropertyName("contact_email")]
        public string ContactEmail { get; set; }

        [JsonPropertyName("contact_phone")]
        public string ContactPhone { get; set; }
    }
}