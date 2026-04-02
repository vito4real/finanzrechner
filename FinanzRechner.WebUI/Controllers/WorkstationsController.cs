using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using FinanzRechner.Domain.Entities;
using FinanzRechner.Infrastructure;

namespace FinanzRechner.WebUI.Controllers
{
    public class WorkstationsController : Controller
    {
        private readonly FinancialCalcDbContext _context;

        public WorkstationsController(FinancialCalcDbContext context)
        {
            _context = context;
        }

        // GET: Workstations
        public async Task<IActionResult> Index()
        {
            return View(await _context.Workstations.ToListAsync());
        }

        // GET: Workstations/Details/5
        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var workstation = await _context.Workstations
                .FirstOrDefaultAsync(m => m.Id == id);
            if (workstation == null)
            {
                return NotFound();
            }

            return View(workstation);
        }

        // GET: Workstations/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Workstations/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Name,Code,Description,OperatorRatePerHour,EnergyKwhPerHour,EnergyPricePerKwh,CoolantLitersPerHour,CoolantPricePerLiter")] Workstation workstation)
        {
            if (ModelState.IsValid)
            {
                workstation.Id = Guid.NewGuid();
                _context.Add(workstation);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(workstation);
        }

        // GET: Workstations/Edit/5
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var workstation = await _context.Workstations.FindAsync(id);
            if (workstation == null)
            {
                return NotFound();
            }
            return View(workstation);
        }

        // POST: Workstations/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, [Bind("Id,Name,Code,Description,OperatorRatePerHour,EnergyKwhPerHour,EnergyPricePerKwh,CoolantLitersPerHour,CoolantPricePerLiter")] Workstation workstation)
        {
            if (id != workstation.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(workstation);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!WorkstationExists(workstation.Id))
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
            return View(workstation);
        }

        // GET: Workstations/Delete/5
        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var workstation = await _context.Workstations
                .FirstOrDefaultAsync(m => m.Id == id);
            if (workstation == null)
            {
                return NotFound();
            }

            return View(workstation);
        }

        // POST: Workstations/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var workstation = await _context.Workstations.FindAsync(id);
            if (workstation != null)
            {
                _context.Workstations.Remove(workstation);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool WorkstationExists(Guid id)
        {
            return _context.Workstations.Any(e => e.Id == id);
        }
    }
}
