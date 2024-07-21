using System;
using Newtonsoft.Json;

namespace LMeter.Act
{
    public class LazyFloatConverter : JsonConverter
    {
        public override bool CanRead => true;

        public override bool CanWrite => false;

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(LazyFloat);
        }

        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            throw new NotImplementedException("Write not supported.");
        }

        public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            if (objectType != typeof(LazyFloat))
            {
                return serializer.Deserialize(reader, objectType);
            }

            return reader.TokenType switch
            {
                JsonToken.Float or JsonToken.Integer => new LazyFloat(serializer.Deserialize<float>(reader)),
                JsonToken.String => new LazyFloat(serializer.Deserialize<string?>(reader)),
                _ => new LazyFloat(0f)
            };
        }
    }
}