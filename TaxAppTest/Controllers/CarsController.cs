using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Configuration;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Humanizer.Configuration;
using Humanizer.Localisation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.DotNet.Scaffolding.Shared.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using NuGet.Packaging.Signing;
using TaxAppTest.Data;
using TaxAppTest.Models;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace TaxAppTest.Controllers
{
    public class CarsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;

        public CarsController(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        [Authorize]
        // GET: Cars
        public async Task<IActionResult> Index()
        {
            return View(await _context.Cars.ToListAsync());
        }
        public async Task<IActionResult> SearchCarPlate()
        {
            return View();
        }
        public async Task<IActionResult> ShowSearchResult(String CarPlateNumber)
        {
            return View("Index", await _context.Cars.Where(x => x.CarPlate == CarPlateNumber).ToListAsync());
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
            try
            {
                List<string> _CarTypes = _configuration.GetSection("TaxAppRules:CarTypes")?.GetChildren()?.Select(x => x.Value)?.ToList();
                ViewBag.CarTypes = new SelectList(_CarTypes);

                return View();
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message, ex.InnerException);
            }
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

                cars.CarTax = CalculateTax(cars);
                ApplyOneChargeRule(cars);

                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(cars);
        }
        [Authorize]
        // GET: Cars/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            try
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
                List<string> _CarTypes = _configuration.GetSection("TaxAppRules:CarTypes")?.GetChildren()?.Select(x => x.Value)?.ToList();
                ViewBag.CarTypes = new SelectList(_CarTypes);

                return View(cars);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message, ex.InnerException);
            }
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

        public void ApplyOneChargeRule(Cars car)
        {
            try
            {
                if (_configuration["TaxAppRules:EnableOneTimeCharge"] == "false")
                {
                    return;
                }
                int _OnetimeCharge = Convert.ToInt32(_configuration["TaxAppRules:OnetimeChargePeriod"]);
                var _CarTaxs = _context.Cars
                    .Where(b => b.CarPlate == car.CarPlate && b.CarDatetime.Date == car.CarDatetime.Date)
                    .OrderBy(b => b.CarDatetime.TimeOfDay).ToList();

                Cars[] _SametimeCarTaxs = new Cars[_CarTaxs.Count];
                _CarTaxs.CopyTo(_SametimeCarTaxs, 0);
                if (_CarTaxs.Count > 1)
                {
                    int first_last = (int)(_CarTaxs[_CarTaxs.Count - 1].CarDatetime - _CarTaxs[0].CarDatetime).TotalMinutes;
                    int distance = (first_last % _OnetimeCharge);
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
            catch (Exception ex)
            {
                throw new Exception(ex.Message, ex.InnerException);
            }
        }

        public bool isFreeCarType(Cars car)
        {
            try
            {
                string _CarType = car.CarType;
                List<string> _FreeCarTypes = _configuration.GetSection("TaxAppRules:FreeCarTypes")?.GetChildren()?.Select(x => x.Value)?.ToList();

                return _FreeCarTypes.Contains(_CarType);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message, ex.InnerException);
            }
        }

        public bool isFreeDay(Cars car)
        {
            try
            {
                string _CarDate = car.CarDatetime.Date.ToString("yyyy/MM/dd");
                List<string> _FreeDays = _configuration.GetSection("TaxAppRules:FreeDays")?.GetChildren()?.Select(x => x.Value)?.ToList();

                return _FreeDays.Contains(_CarDate);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message, ex.InnerException);
            }
        }

        public int TaxTimeCost(Cars car)
        {
            try
            {
                int _TaxTimeCost = 0;
                int _CarTaxSumToday = 0;
                int _MaxTaxPerDay = Convert.ToInt32(_configuration["TaxAppRules:MaxTaxPerDay"]);
                _CarTaxSumToday = _context.Cars
                            .Where(x => (x.CarPlate == car.CarPlate) && (x.CarDatetime.Date == car.CarDatetime.Date)).Sum(i => i.CarTax);
                if (_CarTaxSumToday >= _MaxTaxPerDay)
                {
                    return 0;
                }
                else
                {
                    string _CarTime = car.CarDatetime.ToString("HH:mm");
                    var _TimeCost = _configuration.GetSection("TaxAppRules:TimeCost").GetChildren();
                    foreach (var item in _TimeCost)
                    {
                        var _item = item.Get<Dictionary<string, string>>();
                        if (_CarTime == _item.GetValueOrDefault("StartTime") || _CarTime == _item.GetValueOrDefault("EndTime"))
                        {
                            _TaxTimeCost = Convert.ToInt32(_item.GetValueOrDefault("Cost"));
                            break;
                        }
                        if (DateTime.ParseExact(_CarTime, "HH:mm", CultureInfo.InvariantCulture) > DateTime.ParseExact(_item.GetValueOrDefault("StartTime"), "HH:mm", CultureInfo.InvariantCulture)
                            && DateTime.ParseExact(_CarTime, "HH:mm", CultureInfo.InvariantCulture) < DateTime.ParseExact(_item.GetValueOrDefault("EndTime"), "HH:mm", CultureInfo.InvariantCulture))
                        {
                            _TaxTimeCost = Convert.ToInt32(_item.GetValueOrDefault("Cost"));
                            break;
                        }
                    }
                }
                if (_CarTaxSumToday + _TaxTimeCost >= _MaxTaxPerDay)
                {
                    _TaxTimeCost = _MaxTaxPerDay - _CarTaxSumToday;
                }

                return _TaxTimeCost;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message, ex.InnerException);
            }
        }

        public int CalculateTax(Cars car)
        {
            try
            {
                int tax = 0;
                if (!isFreeCarType(car) && !isFreeDay(car))
                {
                    tax = TaxTimeCost(car);
                }

                return tax;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message, ex.InnerException);
            }
        }
    }
}
