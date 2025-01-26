using Mailo.Models;
using Microsoft.AspNetCore.Mvc;
using Mailo.Repo;
using Mailo.IRepo;
using Mailo.Data;
using Mailo.Data.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering;
namespace Mailo.Controllers
{
    public class ProductController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly AppDbContext _context;
       
        public ProductController(IUnitOfWork unitOfWork, AppDbContext context)
        {
            _unitOfWork = unitOfWork;
            _context = context;
        }
        public async Task<IActionResult> Index()
        {
            return View(await _unitOfWork.products.GetAll());
        }
        #region Aya
        public async Task<IActionResult> GetProductsByCategory(int categoryId)
        {
            var products = await _context.Products
                .Where(p => p.CategoryId == categoryId)
                .Include(p => p.Categorys)
                .ToListAsync();

           var cc = await _context.Categories.ToListAsync();
            return View("Index", products);
        }
        #endregion
   
    }
}












