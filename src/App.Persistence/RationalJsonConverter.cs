using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using VideoDetailTransfer.Core;

namespace VideoDetailTransfer.Persistence;

public sealed class RationalJsonConverter : JsonConverter<Rational>
{
    public override Rational Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
            return Rational.Parse(reader.GetString());

        if (reader.TokenType == JsonTokenType.StartObject)
        {
            long num = 0, den = 0;
            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject) break;
                if (reader.TokenType != JsonTokenType.PropertyName) continue;

                string? name = reader.GetString();
                reader.Read();
                if (string.Equals(name, "num", StringComparison.OrdinalIgnoreCase))
                    num = reader.GetInt64();
                else if (string.Equals(name, "den", StringComparison.OrdinalIgnoreCase))
                    den = reader.GetInt64();
            }
            return Rational.Reduce(num, den);
        }

        // Fallback
        return Rational.Invalid;
    }

    public override void Write(Utf8JsonWriter writer, Rational value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString()); // e.g., "30000/1001" or "0/0"
    }
}