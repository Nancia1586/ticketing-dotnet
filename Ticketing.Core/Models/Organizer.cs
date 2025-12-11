using System.ComponentModel.DataAnnotations;

namespace Ticketing.Core.Models
{
    public class Organizer
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        public string OrganizationName { get; set; } = string.Empty;

        // In a real app this should be hashed, but for this "minimal" requirement we keep it simple or assume it's hashed by service
        [Required]
        public string Password { get; set; } = string.Empty; 
    }
}
