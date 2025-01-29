using Mailo.Data;
using Mailo.Data.Enums;
using Mailo.IRepo;
using Mailo.Models;
using Mailo.Repo;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Mailo.Controllers
{
    public class CartController : Controller
    {
        private readonly ICartRepo _order;
        private readonly IUnitOfWork _unitOfWork;
        private readonly AppDbContext _db;

        public CartController(ICartRepo order, IUnitOfWork unitOfWork, AppDbContext db)
        {
            _order = order;
            _unitOfWork = unitOfWork;
            _db = db;
        }

        public async Task<IActionResult> Index()
        {
            User? user = await _unitOfWork.userRepo.GetUser(User.Identity.Name);
            if (user == null) return RedirectToAction("Login", "Account");

            Order? cart = await _order.GetOrCreateCart(user);
            if (cart == null || cart.OrderProducts == null) return View();

            ViewBag.TotalPrice = cart.OrderPrice;
            return View(cart.OrderProducts.Select(op => op.product).ToList());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddProduct(int productId, int variantId, int quantity = 1)
        {
            User? user = await _unitOfWork.userRepo.GetUser(User.Identity.Name);
            if (user == null) return RedirectToAction("Login", "Account");

            var product = await _unitOfWork.products.GetByID(productId);
            if (product == null)
            {
                TempData["ErrorMessage"] = "Product not found";
                return BadRequest(TempData["ErrorMessage"]);
            }

            var variant = await _db.ProductVariants.Include(v => v.Color).Include(v => v.Size).FirstOrDefaultAsync(v => v.Id == variantId);
            if (variant == null)
            {
                TempData["ErrorMessage"] = "Variant not found";
                return BadRequest(TempData["ErrorMessage"]);
            }

            var cart = await _order.GetOrCreateCart(user);
            if (cart == null)
            {
                cart = new Order
                {
                    UserID = user.ID,
                    OrderPrice = product.TotalPrice * quantity,
                    OrderAddress = user.Address,
                    OrderProducts = new List<OrderProduct>()
                };
                _unitOfWork.orders.Insert(cart);
                await _unitOfWork.CommitChangesAsync();
            }

            if (cart.OrderProducts.Any(op => op.ProductID == productId && op.VariantID == variantId))
            {
                TempData["ErrorMessage"] = "Product with this variant is already in cart";
                return BadRequest(TempData["ErrorMessage"]);
            }

            cart.OrderPrice += product.TotalPrice * quantity;
            cart.OrderProducts.Add(new OrderProduct
            {
                ProductID = productId,
                VariantID = variantId,
                OrderID = cart.ID,
                Quantity = quantity
            });

            _unitOfWork.orders.Update(cart);
            await _unitOfWork.CommitChangesAsync();

            return RedirectToAction("Index_U", "User");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveProduct(int productId, int variantId)
        {
            User? user = await _unitOfWork.userRepo.GetUser(User.Identity.Name);
            var cart = await _order.GetOrCreateCart(user);
            if (cart == null) return View("Index");

            var orderProduct = cart.OrderProducts.FirstOrDefault(op => op.ProductID == productId && op.VariantID == variantId);
            if (orderProduct != null)
            {
                var product = await _unitOfWork.products.GetByID(productId);
                cart.OrderPrice -= product.TotalPrice * orderProduct.Quantity;
                cart.OrderProducts.Remove(orderProduct);

                if (!cart.OrderProducts.Any()) //await ClearCart();

                await _unitOfWork.CommitChangesAsync();
            }
            else
            {
                TempData["ErrorMessage"] = "Product not found";
                return BadRequest(TempData["ErrorMessage"]);
            }

            return RedirectToAction("Index");
        }
    }
}