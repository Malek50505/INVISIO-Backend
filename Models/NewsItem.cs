using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace INVISIO.Models
{
    public class NewsItem
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        [BsonIgnoreIfDefault]
        public string? Id { get; set; }

        public string Headline { get; set; }

        public DateTimeOffset Timestamp { get; set; }

        public string Description { get; set; }
    }
}
