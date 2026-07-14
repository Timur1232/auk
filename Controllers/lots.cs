using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using App.Models;
using App.Extentions;
using App.Attributes;
namespace App.Controllers;


[ApiController]
[Route("lots")]
[AddViewData, HtmxServe]
[GetUser]
public class PublicLotsController(AuctionDbContext db) : Controller
{
    public LotsModel model = new(db);

    [HttpGet("{id}")]
    public async Task<IActionResult> LotDetails([FromRoute] uint id)
    {
        var lot = await model.GetById(id);
        if (lot == null) {
            return NotFound();
        }
        return View("lot_details", lot);
    }
}

[ApiController]
[Route("lots/my")]
[AddViewData, HtmxServe]
[Authorize, GetUserStrict]
public class UserLotsController(AuctionDbContext db) : Controller
{
    public LotsModel model = new(db);

    [HttpGet("create")]
    public async Task<IActionResult> CreateForm()
    {
        return View("create_form");
    }

    [HttpGet]
    public async Task<IActionResult> GetUserLots([FromQuery] int? page, [FromQuery] int? page_size, [FromQuery] uint? tag_id)
    {
        var lots = model.GetPage(tag_id, page ?? 0, page_size ?? LotsModel.DEFAULT_PAGE_SIZE);
        return Ok(lots);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetLot(uint id)
    {
        var lot = await db.lots.FindAsync(id);
        if (lot == null) {
            return NotFound();
        }
        return Ok(lot);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromForm] Lot.CreateRequest req)
    {
        var user = HttpContext.GetUser()!;
        var entry = db.lots.Add(Lot.From(req, user.login));
        if (!await db.TrySaveChangesAsync()) {
            Response.StatusCode = 400;
            return Content("Unable to create lot");
        }
        return Ok(entry.Entity);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteByid(uint id)
    {
        var (deleted_lot, err) = await model.DeleteById(id);
        if (err != ModelError.None || deleted_lot == null) {
            Response.StatusCode = 400;
            return Content(err.GetMessage());
        }
        return Ok(deleted_lot);
    }

    [HttpPatch("{id}")]
    public async Task<IActionResult> UpdateById(uint id, [FromForm] Lot.CreateRequest req)
    {
        var (updated_lot, err) = await model.UpdateById(id, req);
        if (err != ModelError.None || updated_lot == null) {
            Response.StatusCode = 400;
            return Content(err.GetMessage());
        }
        return Ok(updated_lot);
    }
}
