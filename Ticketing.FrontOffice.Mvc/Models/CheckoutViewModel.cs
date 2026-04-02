using System.ComponentModel.DataAnnotations;

namespace Ticketing.FrontOffice.Mvc.Models
{
    public class CheckoutViewModel
    {
        [Required]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Payment Method")]
        public string PaymentMethod { get; set; } = "Credit Card";

        [Display(Name = "Phone Number")]
        [Phone]
        public string? PhoneNumber { get; set; }
        
        public decimal TotalAmount { get; set; }

        public List<CartItem> Items { get; set; } = new();
    }
}
