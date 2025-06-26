// Controllers/NewsController.cs
using INVISIO.Models;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using Microsoft.AspNetCore.Authorization;
using System.Text;
using System.Text.Json;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq; // Ensure System.Linq is included for .Any()
using System; // Ensure System is included for DateTimeOffset
using INVISIO.Services; // Add this using statement to access SuggestionsService
using System.Security.Claims; // Required for User.FindFirst(ClaimTypes.NameIdentifier)

namespace INVISIO.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class NewsController : ControllerBase
    {
        private readonly IMongoCollection<NewsItem> _newsCollection;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly SuggestionsService _suggestionsService; // Declare SuggestionsService
        private const string OllamaApiUrl = "http://localhost:11434/api/generate";
        private const string OllamaModelName = "tinyllama:latest"; // Or "mistral:latest" on other machines

        public NewsController(IMongoClient client, IHttpClientFactory httpClientFactory, SuggestionsService suggestionsService) // Inject SuggestionsService
        {
            var db = client.GetDatabase("INVISIODb");
            _newsCollection = db.GetCollection<NewsItem>("News");
            _httpClientFactory = httpClientFactory;
            _suggestionsService = suggestionsService; // Assign the injected service
        }

        [HttpPost("submit")]
        [Authorize]
        public async Task<IActionResult> SubmitNews([FromBody] List<NewsItem> newsItems)
        {
            // --- DEBUGGING START (Keep these if you want, remove for production) ---
            Console.WriteLine("--- DEBUG: Entering SubmitNews endpoint ---");
            Console.WriteLine($"--- DEBUG: newsItems parameter is null: {newsItems == null}");
            if (newsItems != null)
            {
                Console.WriteLine($"--- DEBUG: newsItems count: {newsItems.Count}");
                if (newsItems.Any())
                {
                    Console.WriteLine($"--- DEBUG: First item Headline: {newsItems.First().Headline}");
                }
            }
            // --- DEBUGGING END ---

            if (newsItems == null || !newsItems.Any())
                return BadRequest(new { code = 4001, message = "No news items received." });

            await _newsCollection.InsertManyAsync(newsItems);
            return Ok(new { code = 2000, message = "News items saved successfully.", count = newsItems.Count });
        }

        [HttpGet("all")]
        public async Task<IActionResult> GetAllNews()
        {
            var newsItems = await _newsCollection.Find(_ => true).ToListAsync();
            return Ok(new { code = 2000, data = newsItems });
        }

        [HttpPost("analyze")]
        [Authorize]
        public async Task<IActionResult> AnalyzeNews([FromBody] UserAnalyzeRequestDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { code = 4001, message = "Invalid request data. Please provide a valid 'userRequest'." });
            }

            var newsItems = await _newsCollection.Find(_ => true).ToListAsync();
            if (!newsItems.Any())
            {
                return NotFound(new { code = 4004, message = "No news items found in the database to analyze. Please submit news first." });
            }

            var newsTextBuilder = new StringBuilder();
            foreach (var item in newsItems)
            {
                newsTextBuilder.AppendLine($"Headline: {item.Headline}");
                newsTextBuilder.AppendLine($"Date: {item.Timestamp:yyyy-MM-dd}");
                newsTextBuilder.AppendLine($"Description: {item.Description}");
                newsTextBuilder.AppendLine();
            }
            string combinedNewsText = newsTextBuilder.ToString();

            // Corrected multiline string for prompt using @""
            string prompt = $@"
You are a Business Insight Assistant. Analyze the following news text and the user's specific request.
Provide:
1.  A concise summary of the key business insights derived from the news, directly addressing the user's request.
2.  Key entities (companies, people, events) and topics discussed, highlighting important connections.
3.  Actionable recommendations for investors, business owners, and analysts, based on your analysis of the news and the user's request.
4.  Finally, provide a very concise, one-sentence ""Suggestion Headline"" and a brief ""Suggestion Description"" (2-3 sentences) based on the most important actionable recommendation.

Format your response as plain text, ensuring the ""Suggestion Headline"" and ""Suggestion Description"" are clearly labeled at the end.
Do NOT include any JSON, markdown code blocks, or conversational intros like 'Response:'. Just provide the analysis directly.
---
Provided News Text:
{combinedNewsText}
---
User's Specific Request:
{dto.UserRequest}
---
";

            var httpClient = _httpClientFactory.CreateClient("OllamaClient");
            var requestPayload = new
            {
                model = OllamaModelName,
                prompt = prompt,
                stream = false
            };

            var jsonPayload = JsonSerializer.Serialize(requestPayload, new JsonSerializerOptions { WriteIndented = false });
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            try
            {
                var response = await httpClient.PostAsync(OllamaApiUrl, content);
                response.EnsureSuccessStatusCode();
                var responseBody = await response.Content.ReadAsStringAsync();
                using (JsonDocument doc = JsonDocument.Parse(responseBody))
                {
                    JsonElement root = doc.RootElement;
                    if (root.TryGetProperty("response", out JsonElement ollamaResponseElement) &&
                        ollamaResponseElement.ValueKind == JsonValueKind.String)
                    {
                        string aiRawTextResponse = ollamaResponseElement.GetString();
                        Console.WriteLine("--- RAW AI RESPONSE (Plain Text) ---");
                        Console.WriteLine(aiRawTextResponse);
                        Console.WriteLine("------------------------------------");

                        // --- Extract Suggestion Headline and Description from AI response ---
                        string suggestionHeadline = "AI Generated Suggestion"; // Default
                        string suggestionDescription = "No specific suggestion description extracted."; // Default

                        var lines = aiRawTextResponse.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                        bool capturingDescription = false;
                        StringBuilder descriptionBuilder = new StringBuilder();

                        foreach (var line in lines)
                        {
                            if (line.TrimStart().StartsWith("Suggestion Headline:"))
                            {
                                suggestionHeadline = line.TrimStart().Replace("Suggestion Headline:", "").Trim();
                                capturingDescription = false; // Stop capturing if a new headline is found
                            }
                            else if (line.TrimStart().StartsWith("Suggestion Description:"))
                            {
                                suggestionDescription = line.TrimStart().Replace("Suggestion Description:", "").Trim();
                                capturingDescription = true; // Start capturing after description tag
                            }
                            else if (capturingDescription)
                            {
                                // Continue appending lines if still capturing description (for multiline descriptions)
                                if (!string.IsNullOrWhiteSpace(line.Trim())) // Only add non-empty lines
                                {
                                    descriptionBuilder.AppendLine(line.Trim());
                                }
                            }
                        }
                        if (descriptionBuilder.Length > 0)
                        {
                            suggestionDescription = descriptionBuilder.ToString().Trim();
                        }
                        // --- End Extraction ---

                        // --- Create and Save the Suggestion ---
                        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                        if (string.IsNullOrEmpty(userId))
                        {
                            return Unauthorized(new { code = 5005, message = "User ID not found in token for creating suggestion." });
                        }

                        var newSuggestion = new Suggestion
                        {
                            Headline = suggestionHeadline,
                            Description = suggestionDescription,
                            Timestamp = DateTimeOffset.UtcNow,
                            UserId = userId,
                            IsPublic = false, // Set as false by default, can be toggled later
                            IsArchived = false
                        };

                        await _suggestionsService.CreateSuggestionAsync(newSuggestion);
                        // --- End Create Suggestion ---

                        return Ok(new
                        {
                            code = 2000,
                            message = "AI analysis complete and suggestion created.",
                            suggestionId = newSuggestion.Id, // The ID of the newly created suggestion
                            date = newSuggestion.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"), // Date of the suggestion
                            aiAnalysis = aiRawTextResponse // The full raw AI response
                        });
                    }
                }
                return StatusCode(500, new { code = 5000, message = "Unexpected Ollama AI response structure. 'response' field not found or not a string." });
            }
            catch (HttpRequestException e)
            {
                return StatusCode(500, new
                {
                    code = 5002,
                    message = $"Error communicating with local Ollama AI model. Please ensure Ollama is running at {OllamaApiUrl} and the model '{OllamaModelName}' is available. Error: {e.Message}"
                });
            }
            catch (Exception e)
            {
                return StatusCode(500, new
                {
                    code = 5003,
                    message = $"An unexpected server error occurred during news analysis: {e.Message}"
                });
            }
        }
    }
}
