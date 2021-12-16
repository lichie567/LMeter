
using System;
using Newtonsoft.Json;

namespace LMeter.ACT
{
    public class EnumConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            throw new NotImplementedException("Write not supported.");
        }

        public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            if (!objectType.IsEnum)
            {
                return serializer.Deserialize(reader, objectType);
            }

            if (reader.TokenType != JsonToken.String)
            {
                return 0;
            }

            string? value = serializer.Deserialize(reader, typeof(string))?.ToString();
            return Enum.TryParse(objectType, value, true, out object? result) ? result : 0;
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
            return objectType.IsEnum;
        }
    }
}