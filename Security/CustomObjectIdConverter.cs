using MongoDB.Bson;
using Newtonsoft.Json;
using System;

public class CustomObjectIdConverter : JsonConverter<ObjectId>
{
    public override void WriteJson(JsonWriter writer, ObjectId value, JsonSerializer serializer)
    {
        // Khi serialize (Gửi đi): ObjectId -> String
        writer.WriteValue(value.ToString());
    }

    public override ObjectId ReadJson(JsonReader reader, Type objectType, ObjectId existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        // Khi deserialize (Nhận về): String -> ObjectId
        if (reader.TokenType == JsonToken.String && ObjectId.TryParse((string)reader.Value!, out var objectId))
        {
            return objectId;
        }
        throw new JsonSerializationException($"Cannot convert value '{reader.Value}' to MongoDB.Bson.ObjectId.");
    }
}