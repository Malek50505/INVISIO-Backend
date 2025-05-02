﻿using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace INVISIO.Models
{
    public class BlacklistedToken
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        public string Token { get; set; }

        public DateTime Expiry { get; set; }
    }
}

