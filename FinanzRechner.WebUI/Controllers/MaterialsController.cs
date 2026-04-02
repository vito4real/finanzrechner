using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using FinanzRechner.Domain.Entities;
using FinanzRechner.Infrastructure;
using FinanzRechner.WebUI.ViewModels.Products.Material;
using System.Diagnostics;

namespace FinanzRechner.WebUI.Controllers
{
    public class MaterialsController : Controller
    {
        private readonly FinancialCalcDbContext _context;

        public MaterialsController(FinancialCalcDbContext context)
        {
            _context = context;
        }

        // GET: Materials
        public async Task<IActionResult> Index()
        {
            return View(await _context.Materials.ToListAsync());
        }

        // GET: Materials/Details/5
        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var material = await _context.Materials
                .AsNoTracking()
                .Include(m => m.ProductMaterials)
                .ThenInclude(pm => pm.Product)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (material == null)
            {
                return NotFound();
            }

            var vm = new MaterialDetailsViewModel
            {
                Id = material.Id,
                Name = material.Name,
                UnitPrice = material.UnitPrice,
                UsedInProducts = material.ProductMaterials
                .Select(pm => new MaterialUsageViewModel
                {
                    ProductId = pm.ProductId,
                    ProductDesignation = pm.Product.Designation,
                    ProductName = pm.Product.Name,
                    Quantity = pm.Quantity
                })
                .OrderBy(x => x.ProductDesignation)
                .ToList()
            };
            return View(vm);
        }

        // GET: Materials/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Materials/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Name,UnitPrice")] Material material)
        {
            if (ModelState.IsValid)
            {
                material.Id = Guid.NewGuid();
                _context.Add(material);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(material);
        }

        // GET: Materials/Edit/5
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var material = await _context.Materials.FindAsync(id);
            if (material == null)
            {
                return NotFound();
            }
            return View(material);
        }

        // POST: Materials/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, [Bind("Id,Name,UnitPrice")] Material material)
        {
            if (id != material.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(material);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!MaterialExists(material.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(material);
        }

        // GET: Materials/Delete/5
        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var material = await _context.Materials
                .FirstOrDefaultAsync(m => m.Id == id);
            if (material == null)
            {
                return NotFound();
            }

            return View(material);
        }

        // POST: Materials/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var material = await _context.Materials.FindAsync(id);
            if (material != null)
            {
                _context.Materials.Remove(material);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool MaterialExists(Guid id)
        {
            return _context.Materials.Any(e => e.Id == id);
        }
    }
}
