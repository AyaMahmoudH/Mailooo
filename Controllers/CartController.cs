using Mailo.Data;

using Mailo.Data.Enums;

using Mailo.IRepo;

using Mailo.Models;

using Mailo.Repo;

using Microsoft.AspNetCore.Authorization;

using Microsoft.AspNetCore.Identity;

using Microsoft.AspNetCore.Mvc;

using Microsoft.CodeAnalysis;

using Microsoft.EntityFrameworkCore;

using Microsoft.EntityFrameworkCore.Metadata.Internal;



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

            if (user == null)

            {

                return RedirectToAction("Login", "Account");

            }



            Order? cart = await _order.GetOrCreateCart(user);

            if (cart == null || cart.OrderProducts == null)

            {

                return View();



            }
            ViewBag.TotalPrice = cart.OrderPrice;

            return View(cart.OrderProducts.Select(op => op.product).ToList());

        }



        [HttpPost]

        [ValidateAntiForgeryToken]

        public async Task<IActionResult> ClearCart()

        {

            User? user = await _unitOfWork.userRepo.GetUser(User.Identity.Name);

            var cart = await _order.GetOrCreateCart(user);

            if (cart != null)

            {

                cart.OrderProducts.Clear();

                _unitOfWork.orders.Delete(cart);

            }

            else

            {

                return View("Index");



            }



            return RedirectToAction("Index");

        }

        [HttpPost]

        [ValidateAntiForgeryToken]

        public async Task<IActionResult> AddProduct(Product product, Sizes size, int quantity = 1)

        {

            User? user = await _unitOfWork.userRepo.GetUser(User.Identity.Name);

            if (user == null)

            {

                return RedirectToAction("Login", "Account");

            }

            if (product == null)

            {

                TempData["ErrorMessage"] = "Product not found";

                return BadRequest(TempData["ErrorMessage"]);



            }



            var cart = await _order.GetOrCreateCart(user);



            if (cart == null)

            {

                cart = new Order

                {

                    UserID = user.ID,

                    OrderPrice = product.TotalPrice*quantity,

                    OrderAddress = user.Address,

                    OrderProducts = new List<OrderProduct>()

                };



                _unitOfWork.orders.Insert(cart);

                await _unitOfWork.CommitChangesAsync();

                cart.OrderProducts.Add(new OrderProduct

                {

                    ProductID = product.ID,

                    Sizes = size,

                    Quantity = quantity,

                    OrderID = cart.ID

                });



                await _unitOfWork.CommitChangesAsync();

            }

            else

            {

                bool? existingOrderProduct = cart.OrderProducts

                    .Where(op => op.ProductID == product.ID && op.Sizes == size).Any();



                if (existingOrderProduct == true)

                {

                    TempData["ErrorMessage"] = "Product is already in cart";

                    return BadRequest(TempData["ErrorMessage"]);



                }

                else

                {

                    cart.OrderPrice += product.TotalPrice * quantity;



                    cart.OrderProducts.Add(new OrderProduct

                    {

                        ProductID = product.ID,

                        Sizes = size,

                        OrderID = cart.ID,

                        Quantity = quantity

                    });



                    _unitOfWork.orders.Update(cart);

                    await _unitOfWork.CommitChangesAsync();

                }

            }



            return RedirectToAction("Index_U", "User");

        }

        [HttpPost]

        [ValidateAntiForgeryToken]

        public async Task<IActionResult> RemoveProduct(int productId)

        {

            User? user = await _unitOfWork.userRepo.GetUser(User.Identity.Name);

            var cart = await _order.GetOrCreateCart(user);

            if (cart == null)

            {

                return View("Index");

            }

            else

            {

                var orderProduct = cart.OrderProducts.FirstOrDefault(op => op.ProductID == productId);

                if (orderProduct != null)

                {

                    var product = await _unitOfWork.products.GetByID(productId);

                    cart.OrderPrice -= product.TotalPrice * orderProduct.Quantity;

                    cart.OrderProducts.Remove(orderProduct);

                    if (cart.OrderProducts == null || !cart.OrderProducts.Any())

                    {

                        await ClearCart();

                    }

                    await _unitOfWork.CommitChangesAsync();

                }

                else

                {

                    TempData["ErrorMessage"] = "Product not found";

                    return BadRequest(TempData["ErrorMessage"]);





                }

            }

            return RedirectToAction("Index");

        }



        [HttpPost]

        [ValidateAntiForgeryToken]

        public async Task<IActionResult> NewOrder()

        {

            User? user = await _unitOfWork.userRepo.GetUser(User.Identity.Name);

            if (user == null)

            {

                return RedirectToAction("Login", "Account");

            }

            var existingOrderItem = await _order.GetOrCreateCart(user);



            if (existingOrderItem == null || (existingOrderItem.OrderStatus != OrderStatus.New))

            {

                TempData["ErrorMessage"] = "Cart is already ordered";

                return BadRequest(TempData["ErrorMessage"]);

            }

            if (existingOrderItem.OrderProducts != null && existingOrderItem.OrderProducts.Any())

            {



                foreach (var op in existingOrderItem.OrderProducts)

                {

                    Product product = await _unitOfWork.products.GetByID(op.ProductID);

                    if (op.Sizes == Sizes.L)

                        product.LQuantity -= op.Quantity;

                    else if (op.Sizes == Sizes.M)

                        product.MQuantity -= op.Quantity;

                    else if (op.Sizes == Sizes.S)

                        product.SQuantity -= op.Quantity;

                }



                existingOrderItem.OrderStatus = OrderStatus.Pending;

                _unitOfWork.orders.Update(existingOrderItem);

                Payment payment = new Payment

                {

                    PaymentMethod = PaymentMethod.Cash_On_Delivery,

                    UserID = user.ID,

                    OrderId = existingOrderItem.ID,

                    PaymentStatus = PaymentStatus.Pending,

                    TotalPrice = existingOrderItem.TotalPrice

                };

                _unitOfWork.payments.Insert(payment);

                existingOrderItem.Payment = payment;

                _unitOfWork.orders.Update(existingOrderItem);

                TempData["Success"] = "Cart Has Been Ordered Successfully";

                return RedirectToAction("GetOrders");

            }

            return View("Index");

        }

        public async Task<IActionResult> ChoosePaymentMethod(Order order)

        {

            return View(order.Payment);

        }

        [HttpPost]

        [ValidateAntiForgeryToken]

        public async Task<IActionResult> ChoosePaymentMethod(Payment payment)

        {



            if (payment.PaymentMethod == PaymentMethod.Paypal)

            {

                _unitOfWork.payments.Update(payment);

                return RedirectToAction("PaymentWithPayPal", "Payment");

            }

            return RedirectToAction("GetOrders");

        }

        [HttpGet]

        public async Task<IActionResult> GetOrders()

        {

            User? user = await _unitOfWork.userRepo.GetUser(User.Identity.Name);

            if (user == null)

            {

                return RedirectToAction("Login", "Account");

            }

            var orders = await _order.GetOrders(user);



            if (orders != null)

            {

                return View(orders);

            }

            else

            {

                return View("Index");



            }

        }

        [HttpPost]

        [ValidateAntiForgeryToken]

        public async Task<IActionResult> CancelOrder(int OrderId)

        {

            User? user = await _unitOfWork.userRepo.GetUser(User.Identity.Name);

            if (user == null)

            {

                return RedirectToAction("Login", "Account");

            }

            var order = await _unitOfWork.orders.GetByIDWithIncludes(OrderId,

                order => order.employee,

                order => order.Payment,

                order => order.user,

                order => order.OrderProducts

             );

            if (order != null)

            {

                var orderProducts = await _db.OrderProducts.Include(op => op.product).Where(op => op.OrderID == order.ID).ToListAsync();

                if (orderProducts != null && orderProducts.Any())

                {

                    foreach (var op in orderProducts)

                    {

                        Product product = await _unitOfWork.products.GetByID(op.ProductID);

                        if (op.Sizes == Sizes.L)

                            product.LQuantity -= op.Quantity;

                        else if (op.Sizes == Sizes.M)

                            product.MQuantity -= op.Quantity;

                        else

                            product.SQuantity -= op.Quantity;

                    }

                    _db.OrderProducts.RemoveRange(orderProducts);

                    _db.SaveChanges();

                }



                if (order.Payment != null)

                {

                    _unitOfWork.payments.Delete(order.Payment);

                }

                _unitOfWork.orders.Delete(order);

                return RedirectToAction("GetOrders");

            }

            else

            {

                return View("Index");

            }

        }

        public async Task<IActionResult> ApplyPromoCode(string promoCode)

        {

            User? user = await _unitOfWork.userRepo.GetUser(User.Identity.Name);

            if (user == null)

            {

                return RedirectToAction("Login", "Account");

            }



            var cart = await _order.GetOrCreateCart(user);

            if (cart == null || !cart.OrderProducts.Any())

            {

                TempData["PromoMessage"] = "Your cart is empty.";

                TempData["PromoStatus"] = "Error";

                return RedirectToAction("Index");

            }



            if (!string.IsNullOrEmpty(cart.PromoCodeUsed))

            {

                if (cart.PromoCodeUsed == promoCode)

                {

                    TempData["PromoMessage"] = "This promo code has already been applied to your order.";

                }

                else

                {

                    TempData["PromoMessage"] = "A promo code has already been applied to this order. You cannot apply another one.";

                }

                TempData["PromoStatus"] = "Error";

                return RedirectToAction("Index");

            }



            if (string.IsNullOrWhiteSpace(promoCode))

            {

                TempData["PromoMessage"] = "Please enter a promo code.";

                TempData["PromoStatus"] = "Error";

                return RedirectToAction("Index");

            }



            var promo = await _db.PromoCodes.FirstOrDefaultAsync(p => p.Code == promoCode && p.ExpiryDate >= DateTime.Now);

            if (promo == null)

            {

                TempData["PromoMessage"] = "Invalid or expired promo code.";

                TempData["PromoStatus"] = "Error";

                return RedirectToAction("Index");

            }



            double discountAmount = cart.OrderPrice * (promo.DiscountPercentage / 100);

            cart.OrderPrice -= discountAmount;

            cart.PromoCodeUsed = promo.Code;



            _unitOfWork.orders.Update(cart);

            await _unitOfWork.CommitChangesAsync();



            TempData["PromoMessage"] = $"Promo code applied successfully! You saved {discountAmount:C}.";

            TempData["PromoStatus"] = "Success";



            return RedirectToAction("Index");

        }



    }

}