using System;
using LMeter.Helpers;
using Newtonsoft.Json;

namespace LMeter.Act
{
    public class JobConverter : JsonConverter
    {
        public override bool CanRead => true;
        public override bool CanWrite => false;

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(Job);
        }

        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            throw new NotImplementedException("Write not supported.");
        }

        public override object? ReadJson(
            JsonReader reader,
            Type objectType,
            object? existingValue,
            JsonSerializer serializer
        )
        {
            if (!objectType.IsEnum)
            {
                return serializer.Deserialize(reader, objectType);
            }

            if (reader.TokenType != JsonToken.String)
            {
                return Job.UKN;
            }

            string? value = serializer.Deserialize<string>(reader)?.ToString();
            if (value is not null && Enum.TryParse(value, true, out Job job))
            {
                return job;
            }

            return Job.UKN;
        }
    }
}
