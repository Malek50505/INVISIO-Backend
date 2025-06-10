// Services/FavoriteService.cs
using INVISIO.Models;
using MongoDB.Driver;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace INVISIO.Services
{
    public class FavoriteService
    {
        private readonly IMongoCollection<UserFavoriteSuggestion> _userFavorites;
        private readonly IMongoCollection<Suggestion> _suggestions;

        public FavoriteService(IMongoClient client)
        {
            var database = client.GetDatabase("INVISIODb");
            _userFavorites = database.GetCollection<UserFavoriteSuggestion>("UserFavorites");
            _suggestions = database.GetCollection<Suggestion>("Suggestions");
        }

        public async Task<bool> ToggleFavoriteAsync(string userId, string suggestionId)
        {
            var filter = Builders<UserFavoriteSuggestion>.Filter.And(
                Builders<UserFavoriteSuggestion>.Filter.Eq(f => f.UserId, userId),
                Builders<UserFavoriteSuggestion>.Filter.Eq(f => f.SuggestionId, suggestionId)
            );
            var existingFavorite = await _userFavorites.Find(filter).FirstOrDefaultAsync();

            if (existingFavorite != null)
            {
                var deleteResult = await _userFavorites.DeleteOneAsync(filter);
                return false;
            }
            else
            {
                var newFavorite = new UserFavoriteSuggestion
                {
                    UserId = userId,
                    SuggestionId = suggestionId
                };
                await _userFavorites.InsertOneAsync(newFavorite);
                return true;
            }
        }

        public async Task<bool> IsFavoritedAsync(string userId, string suggestionId)
        {
            var filter = Builders<UserFavoriteSuggestion>.Filter.And(
                Builders<UserFavoriteSuggestion>.Filter.Eq(f => f.UserId, userId),
                Builders<UserFavoriteSuggestion>.Filter.Eq(f => f.SuggestionId, suggestionId)
            );
            var existingFavorite = await _userFavorites.Find(filter).FirstOrDefaultAsync();
            return existingFavorite != null;
        }

        // Updated for no pagination
        public async Task<List<Suggestion>> GetFavoriteSuggestionsForUserAsync(string userId)
        {
            var favoriteSuggestionIds = await _userFavorites.Find(f => f.UserId == userId)
                                                             .Project(f => f.SuggestionId)
                                                             .ToListAsync();

            if (!favoriteSuggestionIds.Any())
            {
                return new List<Suggestion>();
            }

            // Removed Skip and Limit for no pagination
            return await _suggestions.Find(s => favoriteSuggestionIds.Contains(s.Id))
                                     .ToListAsync();
        }
    }
}