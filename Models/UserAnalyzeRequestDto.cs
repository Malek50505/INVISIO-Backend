
using System.ComponentModel.DataAnnotations;

namespace INVISIO.Models
{
    public class UserAnalyzeRequestDto
    {
        [Required(ErrorMessage = "User request is required.")] 
        [StringLength(1000, ErrorMessage = "User request cannot be longer than 1000 characters.")] 
        public string UserRequest { get; set; }
    }
}
