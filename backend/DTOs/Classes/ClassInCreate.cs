using System.ComponentModel.DataAnnotations;

namespace api.DTOs.Classes
{
    // What the client sends to POST /classes.
    public class ClassInCreate
    {
        [Required(ErrorMessage = "name is required.")]
        [StringLength(200, ErrorMessage = "name must be at most 200 characters.")]
        public string Name { get; set; } = string.Empty;

        [Range(1, 100000, ErrorMessage = "totalSeats must be a whole number between 1 and 100000.")]
        public int TotalSeats { get; set; }
    }
}
