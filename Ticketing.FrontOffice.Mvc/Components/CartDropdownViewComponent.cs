using Microsoft.AspNetCore.Mvc;
using Ticketing.FrontOffice.Mvc.Services;

namespace Ticketing.FrontOffice.Mvc.Components
{
    public class CartDropdownViewComponent : ViewComponent
    {
        private readonly CartService _cartService;

        public CartDropdownViewComponent(CartService cartService)
        {
            _cartService = cartService;
        }

        public IViewComponentResult Invoke()
        {
            var cart = _cartService.GetCart();
            return View(cart);
        }
    }
}