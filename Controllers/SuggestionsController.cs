// Controllers/SuggestionsController.cs
using INVISIO.Models;
using INVISIO.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http; // Required for StatusCodes

namespace INVISIO.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SuggestionsController : ControllerBase
    {
        private readonly SuggestionsService _suggestionsService;
        private readonly FavoriteService _favoriteService;
        public SuggestionsController(SuggestionsService suggestionsService, FavoriteService favoriteService)
        {
            _suggestionsService = suggestionsService;
            _favoriteService = favoriteService;
        }

        private string? GetUserId()
        {
            return User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }

        /// <summary>
        /// Submits a new suggestion. Requires authentication.
        /// Endpoint: POST /api/suggestions/submit
        /// </summary>
        [HttpPost("submit")]
        [Authorize]
        public async Task<IActionResult> SubmitSuggestion([FromBody] SubmitSuggestionDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { code = 4001, message = "Invalid suggestion data." });
            }
            var userId = GetUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { code = 5005, message = "User not authenticated or ID not found in token." });
            }
            var newSuggestion = new Suggestion
            {
                Headline = dto.Headline,
                Description = dto.Description,
                Timestamp = DateTimeOffset.UtcNow,
                UserId = userId,
                IsPublic = dto.IsPublic,
                IsArchived = false
            };
            await _suggestionsService.CreateSuggestionAsync(newSuggestion);
            return Ok(new { code = 2000, message = "Suggestion submitted successfully.", suggestionId = newSuggestion.Id });
        }

        /// <summary>
        /// Toggles the favorite status of a suggestion for the authenticated user. Requires authentication.
        /// Endpoint: POST /api/suggestions/{suggestionId}/toggle-favorite
        /// </summary>
        [HttpPost("{suggestionId}/toggle-favorite")]
        [Authorize]
        public async Task<IActionResult> ToggleFavorite(string suggestionId)
        {
            var userId = GetUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { code = 5005, message = "User not authenticated or ID not found in token." });
            }
            var suggestionExists = await _suggestionsService.GetSuggestionByIdAsync(suggestionId);
            if (suggestionExists == null)
            {
                return NotFound(new { code = 4004, message = "Suggestion not found." });
            }
            var isNowFavorited = await _favoriteService.ToggleFavoriteAsync(userId, suggestionId);
            return Ok(new { code = 2000, message = isNowFavorited ? "Suggestion favorited." : "Suggestion unfavorited.", isFavorited = isNowFavorited });
        }

        /// <summary>
        /// Toggles the public visibility of a suggestion. Requires authentication and user must own the suggestion.
        /// Endpoint: PUT /api/suggestions/{suggestionId}/toggle-public
        /// </summary>
        [HttpPut("{suggestionId}/toggle-public")]
        [Authorize]
        public async Task<IActionResult> TogglePublic(string suggestionId)
        {
            var userId = GetUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { code = 5005, message = "User not authenticated or ID not found in token." });
            }
            var suggestion = await _suggestionsService.GetSuggestionByIdAsync(suggestionId);
            if (suggestion == null)
            {
                return NotFound(new { code = 4004, message = "Suggestion not found." });
            }
            // Ensure only the owner can toggle public status
            if (suggestion.UserId != userId)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { code = 4006, message = "You are not authorized to modify this suggestion." });
            }
            var updated = await _suggestionsService.UpdateIsPublicStatusAsync(suggestionId, !suggestion.IsPublic);
            if (!updated)
            {
                return StatusCode(500, new { code = 5000, message = "Failed to update public status." });
            }
            return Ok(new { code = 2000, message = suggestion.IsPublic ? "Suggestion is now private." : "Suggestion is now public.", isPublic = !suggestion.IsPublic });
        }

        /// <summary>
        /// Gets all public suggestions. No authentication required.
        /// Endpoint: GET /api/suggestions/getpublic
        /// </summary>
        [HttpGet("getpublic")]
        public async Task<IActionResult> GetPublicSuggestions() // Removed skip & limit
        {
            var publicSuggestions = await _suggestionsService.GetPublicSuggestionsAsync(); // Removed skip & limit
            return Ok(new { code = 2000, data = publicSuggestions });
        }

        /// <summary>
        /// Gets all favorite suggestions for the authenticated user. Requires authentication.
        /// Endpoint: GET /api/suggestions/getMyFavSugg
        /// </summary>
        [HttpGet("getMyFavSugg")]
        [Authorize]
        public async Task<IActionResult> GetMyFavoriteSuggestions() // Removed skip & limit
        {
            var userId = GetUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { code = 5005, message = "User not authenticated or ID not found in token." });
            }
            var favoriteSuggestions = await _favoriteService.GetFavoriteSuggestionsForUserAsync(userId); // Removed skip & limit
            return Ok(new { code = 2000, data = favoriteSuggestions });
        }


        /// <summary>
        /// Deletes a suggestion. Requires authentication and user must own the suggestion.
        /// Endpoint: DELETE /api/suggestions/{suggestionId}
        /// </summary>
        [HttpDelete("{suggestionId}")]
        [Authorize]
        public async Task<IActionResult> DeleteSuggestion(string suggestionId)
        {
            var userId = GetUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { code = 5005, message = "User not authenticated or ID not found in token." });
            }
            var suggestion = await _suggestionsService.GetSuggestionByIdAsync(suggestionId);
            if (suggestion == null)
            {
                return NotFound(new { code = 4004, message = "Suggestion not found." });
            }
            // Ensure only the owner can delete the suggestion
            if (suggestion.UserId != userId)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { code = 4006, message = "You are not authorized to delete this suggestion." });
            }
            var deleted = await _suggestionsService.DeleteSuggestionAsync(suggestionId);
            if (!deleted)
            {
                return StatusCode(500, new { code = 5000, message = "Failed to delete suggestion." });
            }
            return Ok(new { code = 2000, message = "Suggestion deleted successfully." });
        }

        /// <summary>
        /// Archives (soft-deletes) a suggestion. Requires authentication and user must own the suggestion.
        /// Endpoint: PUT /api/suggestions/{suggestionId}/archive
        /// </summary>
        [HttpPut("{suggestionId}/archive")]
        [Authorize]
        public async Task<IActionResult> ArchiveSuggestion(string suggestionId)
        {
            var userId = GetUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { code = 5005, message = "User not authenticated or ID not found in token." });
            }
            var suggestion = await _suggestionsService.GetSuggestionByIdAsync(suggestionId);
            if (suggestion == null)
            {
                return NotFound(new { code = 4004, message = "Suggestion not found." });
            }
            // Ensure only the owner can archive the suggestion
            if (suggestion.UserId != userId)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { code = 4006, message = "You are not authorized to archive this suggestion." });
            }
            // Set IsArchived to true
            var updated = await _suggestionsService.UpdateIsArchivedStatusAsync(suggestionId, true);
            if (!updated)
            {
                return StatusCode(500, new { code = 5000, message = "Failed to archive suggestion." });
            }
            return Ok(new { code = 2000, message = "Suggestion archived successfully." });
        }

        /// <summary>
        /// Gets all archived suggestions for the authenticated user. Requires authentication.
        /// Endpoint: GET /api/suggestions/archived
        /// </summary>
        [HttpGet("archived")]
        [Authorize]
        public async Task<IActionResult> GetArchivedSuggestions() // Removed skip & limit
        {
            var userId = GetUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { code = 5005, message = "User not authenticated or ID not found in token." });
            }
            var archivedSuggestions = await _suggestionsService.GetArchivedSuggestionsAsync(userId); // Removed skip & limit
            return Ok(new { code = 2000, data = archivedSuggestions });
        }
    }
}