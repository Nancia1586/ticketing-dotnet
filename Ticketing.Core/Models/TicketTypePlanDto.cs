namespace Ticketing.Core.Models
{
    public class TicketTypePlanDto
    {
        public int TicketTypeId { get; set; } 
        public string Name { get; set; } = string.Empty; 
        public decimal Price { get; set; }
        public string Color { get; set; } = "#cccccc"; 

        
        public string SelectedSeatsJson { get; set; } = "[]"; 
    }
}
