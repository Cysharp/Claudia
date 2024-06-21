using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Claudia;

public class DictionaryJsonConverter : JsonConverter<Dictionary<string, string>>
{
    public override Dictionary<string, string> Read(ref Utf8JsonReader reader, Type _, JsonSerializerOptions __)
    {
        var dictionary = new Dictionary<string, string>();

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.PropertyName)
            {
                var key = Encoding.UTF8.GetString(reader.ValueSpan);
                reader.Read();
                var value = Encoding.UTF8.GetString(reader.ValueSpan);
                dictionary[key] = value;
            }
            else if (reader.TokenType == JsonTokenType.EndObject)
            {
                break;
            }
        }

        return dictionary;
    }

    public override void Write(Utf8JsonWriter writer, Dictionary<string, string> value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value, options);
    }
}