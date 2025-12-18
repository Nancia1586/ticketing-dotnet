using Microsoft.AspNetCore.Identity;

namespace Ticketing.Core.Models
{
    public class ApplicationUser : IdentityUser
    {
        // Custom property for Organizers
        public int? OrganizationId { get; set; }
        public Organizer? Organization { get; set; }
        
        // Full name for profile
        public string? FullName { get; set; }
    }
}
