using Mailo.Data.Enums;
using Mailo.Data;
using Mailo.IRepo;
using Mailo.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Mailo.Controllers
{
    public class UserController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly AppDbContext _context;
        private readonly ISearchRepo _search;

        public UserController(IUnitOfWork unitOfWork, AppDbContext context, ISearchRepo search)
        {
            _unitOfWork = unitOfWork;
            _context = context;
            _search = search;

        }
        public async Task<IActionResult> Search(string text)
        {
            User? user = await _unitOfWork.userRepo.GetUser(User.Identity.Name);
            if(user==null || user.UserType==UserType.Client)
            return View("Index_U",await _search.Search(text));
            return View("Index_A",await _search.Search(text));

        }

        #region Admin 
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            ViewBag.Categories = new SelectList(await _context.Categories.ToListAsync(), "Id", "Name");
            return View();
        }

        public async Task<IActionResult> Index_A()
        {
            return View(await _unitOfWork.productRepo.GetAll());
        }
        public async Task<IActionResult> New()
        {
            ViewBag.Categories = new SelectList(await _context.Categories.ToListAsync(), "Id", "Name");


            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> New(Product product)
        {
            if (ModelState.IsValid)
            {
                    if (product.clientFile != null)
                    {
                        MemoryStream stream = new MemoryStream();
                        product.clientFile.CopyTo(stream);
                        product.dbImage = stream.ToArray();
                    }
                   _unitOfWork.products.Insert(product);
                    TempData["Success"] = "product Has Been Added Successfully";


                return RedirectToAction("Index_A");
            }
            else
            return View(product);

            

        }



        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            if (id != 0)
            {
                return View(await _unitOfWork.products.GetByID(id));
            }
            return NotFound();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(Product product)
        {
            if (product != null)
            {
                _unitOfWork.products.Delete(product);
                TempData["Success"] = "product Has Been Deleted Successfully";
                return RedirectToAction("Index_A");
            }
            return View(product);

        }
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id)
        {
            if (id != 0)
            {
                var product = await _unitOfWork.products.GetByID(id);
                if (product == null)
                {
                    return NotFound();
                }
                return View(product);
            }
            return NotFound();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(Product product)
        {
            var existingProduct = await _unitOfWork.products.GetByID(product.ID);
            if (existingProduct == null)
            {
                return NotFound();
            }

            // Handle the file upload, if there is a file
            if (product.clientFile != null && product.clientFile.Length > 0)
            {
                // Save the uploaded file to a specific folder, e.g., wwwroot/images
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images", product.clientFile.FileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await product.clientFile.CopyToAsync(stream);
                }

                // Update the image path in the existing product
                existingProduct.imageSrc = "~/images/" + product.clientFile.FileName;
            }

            if (ModelState.IsValid)
            {
                existingProduct.Name = product.Name;
                existingProduct.Price = product.Price;
                existingProduct.Description = product.Description;
                existingProduct.CategoryId = product.CategoryId;
                existingProduct.Discount = product.Discount;
                existingProduct.LQuantity= product.LQuantity;
                existingProduct.SQuantity= product.SQuantity;
                existingProduct.MQuantity= product.MQuantity;

                // Now update the existing product in the database
                _unitOfWork.products.Update(existingProduct);

                TempData["Success"] = "Product Has Been Updated Successfully";
               
                
                return RedirectToAction("Index_A");
            }

            return View(product);
        }

        #endregion



        #region Client
        [Authorize(Roles = "Client")]
        public async Task<IActionResult> Index_U()
        {
            return View(await _unitOfWork.products.GetAll());
        }

        #endregion














    }
}
