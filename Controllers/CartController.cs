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
            var userName = HttpContext.Session.GetString("UserName");
            if(string.IsNullOrEmpty(userName))
            {
                return RedirectToAction("Login","Account");
            }

            if(userName != null)
            {
                var user = _context.Users.FirstOrDefault(x => x.UserName == userName);
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
            var userName = HttpContext.Session.GetString("UserName");
            if (string.IsNullOrEmpty(userName))
            {
                return RedirectToAction("Login", "Account");
            }

            var user = _context.Users.FirstOrDefault(u => u.UserName == userName);
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
            var userName = HttpContext.Session.GetString("UserName");
            if (string.IsNullOrEmpty(userName))
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

        public IActionResult DecreaseQuantity(int id)
        {

            var userName = HttpContext.Session.GetString("UserName");
            if (string.IsNullOrEmpty(userName))
            {
                return RedirectToAction("Login", "Account");
            }

            var cartItem = _context.CartItems.FirstOrDefault(x => x.Id == id);
            if(cartItem == null ) 
            {
                return RedirectToAction("MyCart", "Cart");
            }
            
            
            if(cartItem.Quantity > 1)
            {
                cartItem.Quantity--;
                _context.SaveChanges();
            }
            else
            {
                _context.SaveChanges();
                _context.CartItems.Remove(cartItem);
            }
            return RedirectToAction("MyCart", "Cart");
        }

        public IActionResult IncreaseQuantity(int id) 
        {
            var userName = HttpContext.Session.GetString("UserName");
            if (string.IsNullOrEmpty(userName))
            {
                return RedirectToAction("Login", "Account");
            }

            var cartItem = _context.CartItems.FirstOrDefault(x=>x.Id == id);
            if(cartItem == null)
            {
                return RedirectToAction("Login", "Account");
            }
            
            
            var product = _context.Products.FirstOrDefault(p => p.Id == cartItem.ProductId);
            if(product == null)
            {
                return RedirectToAction("MyCart", "Cart");
            }
            
            if(cartItem.Quantity < product.Stock) 
            {
                cartItem.Quantity++;
                _context.SaveChanges();
            }

            return RedirectToAction("MyCart", "Cart");
            
        }

        [HttpPost]
        public IActionResult ClearCart()
        {
            var userName = HttpContext.Session.GetString("UserName");
            if (string.IsNullOrEmpty(userName))
            {
                return RedirectToAction("Login", "Account");
            }

            var user = _context.Users.FirstOrDefault(p => p.UserName == userName);
            if (user == null) return RedirectToAction("Login", "Account");

            var cart = _context.Carts.FirstOrDefault(x => x.UserId == user.Id);
            if (cart == null) return RedirectToAction("MyCart", "Cart");

            var cartItems = _context.CartItems.Where(ci => ci.CartId == cart.Id).ToList();
            if (cartItems.Any())
            {
                _context.CartItems.RemoveRange(cartItems);
                _context.SaveChanges();
            }

            return RedirectToAction("MyCart", "Cart");
        }

        [HttpPost]
        public IActionResult Checkout()
        {
            var userName = HttpContext.Session.GetString("UserName");
            if (string.IsNullOrEmpty(userName))
            {
                return RedirectToAction("Login", "Account");
            }

            var user = _context.Users.FirstOrDefault(x => x.UserName == userName);
            
            if(user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var cart = _context.Carts.FirstOrDefault(c => c.UserId == user.Id);
            if(cart == null)
            {
                return RedirectToAction("MyCart", "Cart");
            }

            var cartItems = _context.CartItems.Where(ci=>ci.CartId == cart.Id).Include(ci=>ci.Product).ToList();
            if (cartItems==null || !cartItems.Any()) 
            {
                return RedirectToAction("MyCart", "Cart");
            }

            var order = new Order
            {
                UserId = user.Id,
                OrderDate = DateTime.UtcNow,
                Status = "Beklemede",
                TotalAmount = cartItems.Sum(i => i.Product.Price * i.Quantity)
            };
            _context.Orders.Add(order);
            _context.SaveChanges();

            foreach (var item in cartItems)
            {
                var orderItem = new OrderItem
                {
                    OrderId = order.Id,
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    UnitPrice = item.Product.Price
                };
                _context.OrderItems.Add(orderItem);

                if(item.Product.Stock >= item.Quantity)
                {
                    item.Product.Stock -= item.Quantity;
                }
                else
                {
                    return RedirectToAction("MyCart", "Cart");
                }
            }

            _context.CartItems.RemoveRange(cartItems);

            _context.SaveChanges();

            TempData["Success"] = "Siparişiniz başarıyla oluşturuldu!";
            return RedirectToAction("OrderSuccess", "Cart");
        }

        public IActionResult OrderSuccess()
        {
            return View();
        }

        [HttpGet]
        public IActionResult MyOrders()
        {
            var userName = HttpContext.Session.GetString("UserName");
            if(string.IsNullOrEmpty(userName))
            {
                return RedirectToAction("Login", "Account");
            }

            var user = _context.Users.FirstOrDefault(x => x.UserName == userName);
            if(user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var orders = _context.Orders.Where(o => o.UserId == user.Id).OrderByDescending(o => o.OrderDate).ToList();
            
            return View(orders);
        }

        [HttpGet]
        public IActionResult OrderDetail(int id)
        {
            var userName = HttpContext.Session.GetString("UserName");
            if (string.IsNullOrEmpty(userName))
            {
                return RedirectToAction("Login", "Account");
            }

            var user = _context.Users.FirstOrDefault(o => o.UserName == userName);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var order = _context.Orders
                .Include(x => x.OrderItems)
                .ThenInclude(oi => oi.Product)
                .FirstOrDefault(o => o.Id == id && o.UserId == user.Id);

            if (order == null)
            {
                return NotFound(); 
            }

            return View(order);
        }

    }
}
