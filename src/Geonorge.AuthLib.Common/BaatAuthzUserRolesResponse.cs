using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Geonorge.AuthLib.Common
{
    public class BaatAuthzUserRolesResponse
    {
        public static readonly BaatAuthzUserRolesResponse Empty = new BaatAuthzUserRolesResponse();

        [JsonPropertyName("services")]
        public List<string> Services { get; set; } = new List<string>();
    }
}