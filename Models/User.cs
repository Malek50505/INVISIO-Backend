using MongoDB.Bson; 
using MongoDB.Bson.Serialization.Attributes; 

namespace INVISIO.Models
{
    public class User
    {
        [BsonId] 
        [BsonRepresentation(BsonType.ObjectId)] 
        public string Id { get; set; } 

        public string FullName { get; set; }

        public string Email { get; set; }

        public string PasswordHash { get; set; } 
    }
}