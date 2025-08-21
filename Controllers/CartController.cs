using Microsoft.AspNetCore.Mvc;
using Notifico.Data;
using Notifico.Models;

namespace Notifico.Controllers
{
    public class CartController : Controller
    {
        
        private readonly ApplicationDbContext _context;
        public CartController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        public ActionResult AddToCart(int productId) 
        {
            var UserName = HttpContext.Session.GetString("UserName");
            if(string.IsNullOrEmpty(UserName))
            {
                return RedirectToAction("Login","Account");
            }

            if(UserName != null)
            {
                var user = _context.Users.FirstOrDefault(x => x.UserName == UserName);
                var cart = _context.Carts.FirstOrDefault(x => x.UserId == user.Id);
                

                if(cart == null)
                {
                    cart = new Cart();
                    cart.UserId = user.Id;
                    _context.Carts.Add(cart);
                    _context.SaveChanges();
                }

                var cartItem = _context.CartItems.FirstOrDefault(x => x.CartId == cart.Id && x.ProductId == productId);
                if(cartItem != null) 
                {
                    cartItem.Quantity++;
                }
                else
                {
                    cartItem = new CartItem();
                    cartItem.CartId = cart.Id;
                    cartItem.ProductId = productId;
                    cartItem.Quantity = 1;

                    _context.CartItems.Add(cartItem);
                }
                    
                _context.SaveChanges();
                
                return RedirectToAction("Index","Product");
            }

            return RedirectToAction("Index","Product");
        }

    }
}
