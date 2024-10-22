using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TaxAppTest.Data;
using TaxAppTest.Models;

namespace TaxAppTest.Controllers
{
    public class CarsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CarsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Cars
        public async Task<IActionResult> Index()
        {
            return View(await _context.Cars.ToListAsync());
        }
        [Authorize]
        // GET: Cars/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var cars = await _context.Cars
                .FirstOrDefaultAsync(m => m.Id == id);
            if (cars == null)
            {
                return NotFound();
            }

            return View(cars);
        }

        // GET: Cars/Create
        [Authorize]
        public IActionResult Create()
        {
            return View();
        }

        // POST: Cars/Create
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,CarPlate,CarType,CarDatetime,CarTax")] Cars cars)
        {
            if (ModelState.IsValid)
            {
                _context.Add(cars);
                CalculateTax(cars);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(cars);
        }
        [Authorize]
        // GET: Cars/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var cars = await _context.Cars.FindAsync(id);
            if (cars == null)
            {
                return NotFound();
            }
            return View(cars);
        }

        // POST: Cars/Edit/5
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,CarPlate,CarType,CarDatetime,CarTax")] Cars cars)
        {
            if (id != cars.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(cars);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CarsExists(cars.Id))
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
            return View(cars);
        }
        [Authorize]
        // GET: Cars/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var cars = await _context.Cars
                .FirstOrDefaultAsync(m => m.Id == id);
            if (cars == null)
            {
                return NotFound();
            }

            return View(cars);
        }

        // POST: Cars/Delete/5
        [Authorize]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var cars = await _context.Cars.FindAsync(id);
            if (cars != null)
            {
                _context.Cars.Remove(cars);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool CarsExists(int id)
        {
            return _context.Cars.Any(e => e.Id == id);
        }

        public void CalculateTax(Cars car)
        {
            double _OnetimeCharge = 60;
            var _CarTaxs = _context.Cars
                .Where(b => b.CarPlate == car.CarPlate && b.CarDatetime.Date == car.CarDatetime.Date)
                .OrderBy(b => b.CarDatetime.TimeOfDay).ToList();

            Cars[] _SametimeCarTaxs = new Cars[_CarTaxs.Count];
            _CarTaxs.CopyTo(_SametimeCarTaxs, 0);
            if(_CarTaxs.Count > 1)
            {
                double first_last = (_CarTaxs[_CarTaxs.Count - 1].CarDatetime - _CarTaxs[0].CarDatetime).TotalMinutes;
                double distance = (first_last % _OnetimeCharge);
                int counter = 0;
                foreach (var cartax in _CarTaxs)
                {
                    if (cartax.CarDatetime.AddMinutes(distance) >= car.CarDatetime)
                    {
                        _SametimeCarTaxs[counter] = cartax;
                        counter++;
                    }
                }
                int biggestTax = 0;
                foreach (var taxItem in _SametimeCarTaxs)
                {
                    if (taxItem.CarTax > biggestTax)
                    {
                        biggestTax = taxItem.CarTax;
                    }
                }
                foreach (var taxItem in _SametimeCarTaxs)
                {
                    if (taxItem.CarTax != biggestTax)
                    {
                        taxItem.CarTax = 0;
                    }
                    _context.Cars.Update(taxItem);
                }
            }
        }

        public bool isTaxFree(Cars car)
        {
            return _context.Cars.Any(c => c.Id == car.Id);
        }
    }
}
