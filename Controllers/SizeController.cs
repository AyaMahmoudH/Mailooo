using Mailo.Data;
using Mailoo.Models;
using Microsoft.AspNetCore.Mvc;

namespace Mailoo.Controllers
{
    public class SizeController : Controller
    {
        private readonly AppDbContext _context;

        public SizeController(AppDbContext context)
        {
            _context = context;
        }
            public IActionResult Index()
            {
                var sizes = _context.Sizes.ToList();
                return View(sizes);
            }

            public IActionResult Create()
            {
                return View();
            }

            [HttpPost]
            public IActionResult Create(Size size)
            {
                if (ModelState.IsValid)
                {
                    _context.Sizes.Add(size);
                    _context.SaveChanges();
                    return RedirectToAction("Index");
                }
                return View(size);
            }

            public IActionResult Edit(int id)
            {
                var size = _context.Sizes.Find(id);
                if (size == null)
                    return NotFound();

                return View(size);
            }

            [HttpPost]
            public IActionResult Edit(Size size)
            {
                if (ModelState.IsValid)
                {
                    _context.Sizes.Update(size);
                    _context.SaveChanges();
                    return RedirectToAction("Index");
                }
                return View(size);
            }

        public IActionResult Delete(int id)
        {
            var size = _context.Sizes.Find(id);
            if (size == null)
                return NotFound();

            return View(size);
        }

        [HttpPost]
        public IActionResult DeleteConfirmed(int id)
        {
            var size = _context.Sizes.Find(id);
            if (size == null)
                return NotFound();

            _context.Sizes.Remove(size);
            _context.SaveChanges();

            return RedirectToAction("Index");
        }


    }

}
