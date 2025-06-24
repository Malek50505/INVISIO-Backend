using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace INVISIO.Models
{
    public class UserFavoriteSuggestion
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [BsonElement("userId")]
        public string UserId { get; set; }

        [BsonElement("suggestionId")]
        public string SuggestionId { get; set; }
    }
}
