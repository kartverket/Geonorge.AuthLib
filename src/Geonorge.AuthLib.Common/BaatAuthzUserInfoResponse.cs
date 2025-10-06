using System.Text.Json;
using System;
using System.Text.Json.Serialization;

namespace Geonorge.AuthLib.Common
{
    public class BaatAuthzUserInfoResponse
    {
        public static readonly BaatAuthzUserInfoResponse Empty = new BaatAuthzUserInfoResponse();

        [JsonPropertyName("user")]
        public string User { get; set; }

        [JsonPropertyName("organization")]
        public BaatAuthzUserInfoOrganization Organization { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("email")]
        public string Email { get; set; }

        [JsonPropertyName("authorized_from")]
        [JsonConverter(typeof(IntToStringJsonConverter))]
        public string AuthorizedFrom { get; set; }

        [JsonPropertyName("authorized_until")]
        [JsonConverter(typeof(IntToStringJsonConverter))]
        public string AuthorizedUntil { get; set; }
    }

    public class BaatAuthzUserInfoOrganization
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("orgnr")]
        public string Orgnr { get; set; }

        [JsonPropertyName("contact_name")]
        public string ContactName { get; set; }

        [JsonPropertyName("contact_email")]
        public string ContactEmail { get; set; }

        [JsonPropertyName("contact_phone")]
        public string ContactPhone { get; set; }
    }

    public sealed class IntToStringJsonConverter : JsonConverter<string>
    {
        public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.Number:
                    long num;
                    if (reader.TryGetInt64(out num))
                    {
                        return num.ToString();
                    }
                    else
                    {
                        throw new JsonException("Expected integer number for string conversion.");
                    }

                case JsonTokenType.String:
                    return reader.GetString() ?? string.Empty;

                case JsonTokenType.Null:
                    return string.Empty;

                default:
                    throw new JsonException("Unexpected token " + reader.TokenType + " when parsing string.");
            }
        }

        public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value);
        }
    }
}