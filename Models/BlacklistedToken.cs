using MongoDB.Bson;
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

/*
 * << Sessions >>
 * Id 
 * userId
 * Expity
 * 
 * << User >>
 * Id
 * FullName
 * Email
 * PasswordHash
 * 
 * << UserLoginDto >>
 * Email
 * Password
 * 
 * << UserSignupDto >>
 * FullName
 * Email
 * Password
 * 
 * << BlacklistedToken >>
 * Id
 * Token
 * Expiry
 */