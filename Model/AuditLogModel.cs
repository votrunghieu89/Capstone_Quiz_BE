using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Serializers;
using Newtonsoft.Json; // Cần thiết
using Newtonsoft.Json.Converters;

namespace Capstone.Model
{
    public class AuditLogModel
    {
        [BsonId]
        [JsonConverter(typeof(CustomObjectIdConverter))]
        public ObjectId AuditLogId { get; set; } = ObjectId.GenerateNewId();
        public  int AccountId { get; set; }
        public string Action { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime CreatAt { get; set; }
        public string IpAddress { get; set; } = string.Empty;
    }
}
