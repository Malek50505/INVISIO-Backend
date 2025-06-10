using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace INVISIO.Models
{
    public class Suggestion
    {
        [BsonId] 
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        public string Headline { get; set; }
        public string Description { get; set; }

        public DateTimeOffset Timestamp { get; set; }

        public string UserId { get; set; }

        public bool IsPublic { get; set; } = false; 

        public bool IsArchived { get; set; } = false; 
    }
}