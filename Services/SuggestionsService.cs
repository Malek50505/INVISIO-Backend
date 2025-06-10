// Services/SuggestionsService.cs
using INVISIO.Models;
using MongoDB.Driver;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace INVISIO.Services
{
    public class SuggestionsService
    {
        private readonly IMongoCollection<Suggestion> _suggestions;

        public SuggestionsService(IMongoClient client)
        {
            var database = client.GetDatabase("INVISIODb");
            _suggestions = database.GetCollection<Suggestion>("Suggestions");
        }

        public async Task CreateSuggestionAsync(Suggestion suggestion) =>
            await _suggestions.InsertOneAsync(suggestion);

        public async Task<Suggestion> GetSuggestionByIdAsync(string id) =>
            await _suggestions.Find(s => s.Id == id).FirstOrDefaultAsync();

        // Updated for no pagination
        public async Task<List<Suggestion>> GetPublicSuggestionsAsync() =>
            await _suggestions.Find(s => s.IsPublic).ToListAsync();

        // Method to get archived suggestions for a specific user (UPDATED FOR NO PAGINATION)
        public async Task<List<Suggestion>> GetArchivedSuggestionsAsync(string userId) =>
            await _suggestions.Find(s => s.UserId == userId && s.IsArchived).ToListAsync();

        public async Task<bool> UpdateIsPublicStatusAsync(string id, bool isPublic)
        {
            var filter = Builders<Suggestion>.Filter.Eq(s => s.Id, id);
            var update = Builders<Suggestion>.Update.Set(s => s.IsPublic, isPublic);
            var result = await _suggestions.UpdateOneAsync(filter, update);
            return result.IsAcknowledged && result.ModifiedCount > 0;
        }

        public async Task<bool> UpdateIsArchivedStatusAsync(string id, bool isArchived)
        {
            var filter = Builders<Suggestion>.Filter.Eq(s => s.Id, id);
            var update = Builders<Suggestion>.Update.Set(s => s.IsArchived, isArchived);
            var result = await _suggestions.UpdateOneAsync(filter, update);
            return result.IsAcknowledged && result.ModifiedCount > 0;
        }

        public async Task<bool> DeleteSuggestionAsync(string id)
        {
            var result = await _suggestions.DeleteOneAsync(s => s.Id == id);
            return result.IsAcknowledged && result.DeletedCount > 0;
        }
    }
}