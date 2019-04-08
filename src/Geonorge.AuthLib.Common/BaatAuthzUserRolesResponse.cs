using System.Collections.Generic;
using Newtonsoft.Json;

namespace Geonorge.AuthLib.Common
{
    public class BaatAuthzUserRolesResponse
    {
        public static readonly BaatAuthzUserRolesResponse Empty = new BaatAuthzUserRolesResponse();
        
        [JsonProperty("services")]
        public List<string> Services = new List<string>();
    }
}