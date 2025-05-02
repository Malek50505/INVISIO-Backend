using MongoDB.Bson; // functionality ?
using MongoDB.Bson.Serialization.Attributes; // functionality ?, why did we import it alone . is not it involved in the MongoDB.Bson ?

namespace INVISIO.Models
{
    public class User
    {
        [BsonId] // what is this 
        [BsonRepresentation(BsonType.ObjectId)] // what is this
        public string Id { get; set; } // there is "2 references" above each public string ,what are references ,what do they mean?

        public string FullName { get; set; }

        public string Email { get; set; }

        public string PasswordHash { get; set; } // what is {get; set;} ??
    }
}