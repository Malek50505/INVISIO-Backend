using System.ComponentModel.DataAnnotations; 

namespace INVISIO.Models
{
    public class UserSignupDto
    {
        [Required] 
        public string FullName { get; set; }

        [Required, EmailAddress]
        public string Email { get; set; }

        [Required, MinLength(6)]
        public string Password { get; set; }
    }
}
