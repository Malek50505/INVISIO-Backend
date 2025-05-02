using INVISIO.Models;
using MongoDB.Driver;

namespace INVISIO.Services
{
    public class BlacklistService
    {
        private readonly IMongoCollection<BlacklistedToken> _blacklist;

        public BlacklistService(IMongoClient client)
        {
            var db = client.GetDatabase("INVISIODb");
            _blacklist = db.GetCollection<BlacklistedToken>("BlacklistedTokens");
        }

        public async Task AddToBlacklistAsync(string token, DateTime expiry)
        {
            var entry = new BlacklistedToken
            {
                Token = token,
                Expiry = expiry
            };
            await _blacklist.InsertOneAsync(entry);
        }

        public async Task<bool> IsBlacklistedAsync(string token)
        {
            var exists = await _blacklist.Find(x => x.Token == token).FirstOrDefaultAsync();
            return exists != null;
        }
    }
}
