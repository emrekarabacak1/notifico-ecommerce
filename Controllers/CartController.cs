using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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

        [HttpGet]
        public IActionResult MyCart()
        {
            var UserName = HttpContext.Session.GetString("UserName");
            if (string.IsNullOrEmpty(UserName))
            {
                return RedirectToAction("Login", "Account");
            }

            var user = _context.Users.FirstOrDefault(u => u.UserName == UserName);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var cart = _context.Carts.FirstOrDefault(x => x.UserId == user.Id);
            if (cart == null)
            {
                return View(new List<CartItem>());
            }

            
            var cartItems = _context.CartItems
                .Where(ci => ci.CartId == cart.Id)
                .Include(ci => ci.Product)   
                .ToList();

            if (cartItems == null || !cartItems.Any())
            {
                return View(new List<CartItem>());
            }
            else
            {
                return View(cartItems);
            }
        }

        public IActionResult RemoveFromCart(int id)
        {
            var UserName = HttpContext.Session.GetString("UserName");
            if (string.IsNullOrEmpty(UserName))
            {
                return RedirectToAction("Login", "Account");
            }

            var cartItem = _context.CartItems.FirstOrDefault(x => x.Id == id);
            if(cartItem != null)
            {
                _context.CartItems.Remove(cartItem);
                _context.SaveChanges();
            }

            
            return RedirectToAction("MyCart", "Cart");
        }
    }
}
