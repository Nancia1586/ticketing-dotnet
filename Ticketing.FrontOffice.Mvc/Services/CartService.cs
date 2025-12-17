using System.Text.Json;
using Ticketing.FrontOffice.Mvc.Models;
using System.Collections.Concurrent;

namespace Ticketing.FrontOffice.Mvc.Services
{
    public class CartService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private const string SessionKey = "Cart";
        private static readonly ConcurrentDictionary<string, object> _locks = new();

        public CartService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public Cart GetCart()
        {
            var session = _httpContextAccessor.HttpContext?.Session;
            var json = session?.GetString(SessionKey);
            return json == null ? new Cart() : JsonSerializer.Deserialize<Cart>(json) ?? new Cart();
        }

        public void SaveCart(Cart cart)
        {
            var session = _httpContextAccessor.HttpContext?.Session;
            var json = JsonSerializer.Serialize(cart);
            session?.SetString(SessionKey, json);
        }
        
        private object GetSessionLock()
        {
            var sessionId = _httpContextAccessor.HttpContext?.Session?.Id ?? "default";
            return _locks.GetOrAdd(sessionId, _ => new object());
        }

        public void AddItem(CartItem item)
        {
            // Lock by session to prevent concurrent modifications
            lock (GetSessionLock())
            {
                var cart = GetCart();
                
                // Helper method to compare two seat lists - more robust comparison
                bool AreSeatsEqual(List<SeatSelection> seats1, List<SeatSelection> seats2)
                {
                    if (seats1 == null && seats2 == null) return true;
                    if (seats1 == null || seats2 == null) return false;
                    if (seats1.Count != seats2.Count) return false;
                    
                    // Create sets for comparison to handle order differences
                    var set1 = seats1.Select(s => new { s.Row, s.Col }).ToHashSet();
                    var set2 = seats2.Select(s => new { s.Row, s.Col }).ToHashSet();
                    
                    return set1.SetEquals(set2);
                }
                
                // Check if an identical item already exists
                // For items with seats, check if the same seats are already in cart
                if (item.Seats != null && item.Seats.Any())
                {
                    // Check ALL items with same event and ticket type
                    var candidateItems = cart.Items.Where(i => 
                        i.EventId == item.EventId && 
                        i.TicketTypeId == item.TicketTypeId &&
                        i.Seats != null &&
                        i.Seats.Any() // Must have seats
                    ).ToList();
                    
                    // Check if any candidate has the exact same seats (using set comparison)
                    foreach (var existingItem in candidateItems)
                    {
                        if (AreSeatsEqual(existingItem.Seats, item.Seats))
                        {
                            // Item with same seats already exists, don't add duplicate
                            SaveCart(cart);
                            return;
                        }
                    }
                }
                else
                {
                    // For items without specific seats, check if same event and ticket type exists
                    var existingItem = cart.Items.FirstOrDefault(i => 
                        i.EventId == item.EventId && 
                        i.TicketTypeId == item.TicketTypeId &&
                        (i.Seats == null || !i.Seats.Any()) // Only match items without specific seats
                    );
                    
                    if (existingItem != null)
                    {
                        // Update quantity instead of adding duplicate
                        existingItem.Quantity += item.Quantity;
                        SaveCart(cart);
                        return;
                    }
                }
                
                // No duplicate found, add the new item
                cart.Items.Add(item);
                SaveCart(cart);
            }
        }

        public void RemoveItem(Guid itemId)
        {
            var cart = GetCart();
            var item = cart.Items.FirstOrDefault(i => i.Id == itemId);
            if (item != null)
            {
                cart.Items.Remove(item);
                SaveCart(cart);
            }
        }

        public void Clear()
        {
            var session = _httpContextAccessor.HttpContext?.Session;
            session?.Remove(SessionKey);
        }
    }
}
