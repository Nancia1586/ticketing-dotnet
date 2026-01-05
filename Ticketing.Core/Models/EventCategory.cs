namespace Ticketing.Core.Models
{
    public class Category
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        
        [System.Text.Json.Serialization.JsonIgnore]
        public ICollection<Event> Events { get; set; } = new List<Event>();
    }
}

