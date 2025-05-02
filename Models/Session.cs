namespace INVISIO.Models
{
    public class Session
    {
        public string Id { get; set; }
        public string UserId { get; set; }
        public DateTime Expiry { get; set; }
    }
}

