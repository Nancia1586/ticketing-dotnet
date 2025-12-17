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
        [CreditCard] // Simple validation
        [Display(Name = "Card Number")]
        public string CardNumber { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Valid Until (MM/YY)")]
        public string ExpiryDate { get; set; } = string.Empty;

        [Required]
        public string CVV { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Payment Method")]
        public string PaymentMethod { get; set; } = "Credit Card";
        
        public decimal TotalAmount { get; set; }
    }
}
