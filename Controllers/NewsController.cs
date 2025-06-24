using INVISIO.Models;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using Microsoft.AspNetCore.Authorization;
using System.Text;
using System.Text.Json;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq; 

namespace INVISIO.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class NewsController : ControllerBase
    {
        private readonly IMongoCollection<NewsItem> _newsCollection;
        private readonly IHttpClientFactory _httpClientFactory; // Keep this injection

        private const string OllamaApiUrl = "http://localhost:11434/api/generate";
        private const string OllamaModelName = "tinyllama:latest";

        public NewsController(IMongoClient client, IHttpClientFactory httpClientFactory)
        {
            var db = client.GetDatabase("INVISIODb");
            _newsCollection = db.GetCollection<NewsItem>("News");
            _httpClientFactory = httpClientFactory;
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

            string prompt = $@"
    You are a Business Insight Assistant. Analyze the following news text and the user's specific request.

    Provide:
    1.  A concise summary of the key business insights derived from the news, directly addressing the user's request.
    2.  Key entities (companies, people, events) and topics discussed, highlighting important connections.
    3.  Actionable recommendations for investors, business owners, and analysts, based on your analysis of the news and the user's request.

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

                        return Ok(new { code = 2000, data = aiRawTextResponse, message = "AI analysis provided as plain text. For structured JSON and charts, use a more capable model on a different machine." });
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
