using INVISIO.Models;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace INVISIO.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class NewsController : ControllerBase
    {
        private readonly IMongoCollection<NewsItem> _newsCollection;

        public NewsController(IMongoClient client)
        {
            var db = client.GetDatabase("INVISIODb");
            _newsCollection = db.GetCollection<NewsItem>("News");
        }

        [HttpPost("submit")]
        public async Task<IActionResult> SubmitNews([FromBody] List<NewsItem> newsItems)
        {
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
    }
}
