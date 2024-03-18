using System;
using Dalamud.Utility;
using LMeter.Helpers;
using Newtonsoft.Json;

namespace LMeter.Act
{
    public class JobConverter : JsonConverter
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
                return Job.UKN;
            }

            string? value = serializer.Deserialize(reader, typeof(string))?.ToString();
            if (value.IsNullOrEmpty() || value.Equals("Limit Break"))
            {
                return Job.UKN;
            }

            return Enum.Parse<Job>(value.ToUpper());
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(Job);
        }
    }
}