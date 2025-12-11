using System.Text.Json;
using Ticketing.FrontOffice.Mvc.Models;

namespace Ticketing.FrontOffice.Mvc.Services
{
    public class CartService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private const string SessionKey = "Cart";

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

        public void AddItem(CartItem item)
        {
            var cart = GetCart();
            cart.Items.Add(item);
            SaveCart(cart);
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
