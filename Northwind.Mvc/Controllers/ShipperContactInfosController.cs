using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Northwind.Mvc.Models;

namespace Northwind.Mvc.Controllers
{
    public class ShipperContactInfosController : Controller
    {
        private readonly NorthwindContext _context;

        public ShipperContactInfosController(NorthwindContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var list = await _context.ShipperContactInfos
                .Include(x => x.Shipper)
                .ToListAsync();

            return View(list);
        }

        public IActionResult Create()
        {
            ViewBag.Shippers = new SelectList(_context.Shippers, "ShipperId", "CompanyName");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ShipperContactInfo shipperContactInfo)
        {
            if (shipperContactInfo.Shipper == null)
            {
                ModelState.Remove("Shipper");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Shippers = new SelectList(_context.Shippers, "ShipperId", "CompanyName", shipperContactInfo.ShipperId);
                return View(shipperContactInfo);
            }

            _context.ShipperContactInfos.Add(shipperContactInfo);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        public async Task<IActionResult> Delete(int id)
{
    var item = await _context.ShipperContactInfos
        .Include(x => x.Shipper)
        .FirstOrDefaultAsync(x => x.ContactInfoId == id);

    if (item == null)
    {
        return NotFound();
    }

    return View(item);
}
[HttpPost, ActionName("Delete")]
[ValidateAntiForgeryToken]
public async Task<IActionResult> DeleteConfirmed(int id)
{
    var item = await _context.ShipperContactInfos.FindAsync(id);

    if (item != null)
    {
        _context.ShipperContactInfos.Remove(item);
        await _context.SaveChangesAsync();
    }

    return RedirectToAction(nameof(Index));
}
public async Task<IActionResult> Edit(int id)
{
    var item = await _context.ShipperContactInfos.FindAsync(id);

    if (item == null)
    {
        return NotFound();
    }

    ViewBag.Shippers = new SelectList(_context.Shippers, "ShipperId", "CompanyName", item.ShipperId);
    return View(item);
}
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Edit(int id, ShipperContactInfo shipperContactInfo)
{
    if (id != shipperContactInfo.ContactInfoId)
    {
        return NotFound();
    }

    if (shipperContactInfo.Shipper == null)
    {
        ModelState.Remove("Shipper");
    }

    if (!ModelState.IsValid)
    {
        ViewBag.Shippers = new SelectList(_context.Shippers, "ShipperId", "CompanyName", shipperContactInfo.ShipperId);
        return View(shipperContactInfo);
    }

    _context.Update(shipperContactInfo);
    await _context.SaveChangesAsync();

    return RedirectToAction(nameof(Index));
}
    }
}