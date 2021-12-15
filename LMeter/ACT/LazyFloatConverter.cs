
using System;
using Newtonsoft.Json;

namespace LMeter.ACT
{
    public class LazyFloatConverter : JsonConverter
    {
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

            if (reader.TokenType != JsonToken.String)
            {
                return new LazyFloat(0f);
            }

            return new LazyFloat(serializer.Deserialize(reader, typeof(string))?.ToString());
        }

        public override bool CanRead
        {
            get { return true; }
        }

        public override bool CanWrite
        {
            get { return false; }
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(LazyFloat);
        }
    }
}