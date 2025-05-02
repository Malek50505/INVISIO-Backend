using System.ComponentModel.DataAnnotations; // what is system, what is componentModels, what is DataAnnotations, what are thier functionalites in my code

namespace INVISIO.Models
{
    public class UserSignupDto
    {
        [Required] // what does this mean
        public string FullName { get; set; }

        [Required, EmailAddress]
        public string Email { get; set; }

        [Required, MinLength(6)]
        public string Password { get; set; }
    }
}
