using System.ComponentModel.DataAnnotations;

namespace INVISIO.Models
{
    public class SubmitSuggestionDto
    {
        [Required(ErrorMessage = "Headline is required.")]
        [StringLength(100, ErrorMessage = "Headline cannot be longer than 100 characters.")]
        public string Headline { get; set; }

        [Required(ErrorMessage = "Description is required.")]
        [StringLength(1000, ErrorMessage = "Description cannot be longer than 1000 characters.")]
        public string Description { get; set; }

        public bool IsPublic { get; set; }
    }
}
