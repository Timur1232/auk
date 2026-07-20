using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using App.Models;
using App.Extentions;
using App.Attributes;
namespace App.Controllers;


[ApiController]
[Route("lots")]
[AddViewData, HtmxServe]
[GetUser]
public class PublicLotsController(AuctionDbContext db, IWebHostEnvironment env) : Controller
{
    public LotsModel model = new(db, env);

    [HttpGet("{id}")]
    public async Task<IActionResult> LotDetails([FromRoute] uint id)
    {
        var lot = await model.GetById(id);
        if (lot == null) return NotFound();

        var images = lot.images.ToList();
        var seller = await db.GetUserByLogin(lot.user_login);
        var current_user = HttpContext.GetUser();
        var is_owner = current_user?.login == lot.user_login;

        var vm = new LotDetailsViewModel {
            lot = lot,
            images = images,
            seller = seller,
            is_owner = is_owner
        };
        return View("lot_details", vm);
    }

    [HttpPost("{id}/bid")]
    [Authorize, GetUserStrict]
    public async Task<IActionResult> MakeBid([FromRoute] uint id, [FromForm] Lot.BidForm req)
    {
        var user = HttpContext.GetUser()!;
        var (new_price, error) = await model.MakeBid(id, req, user.login);

        if (error != null) {
            Response.StatusCode = 400;
            ViewData["errors"] = error;
            return View("bid_form");
        }

        return View("bid_form", new BidFormData{ lot_id = id, price = new_price, price_changed = true });
    }
}

[ApiController]
[Route("lots/my")]
[AddViewData, HtmxServe]
[Authorize, GetUserStrict]
public class UserLotsController(AuctionDbContext db, IWebHostEnvironment env) : Controller
{
    public LotsModel model = new(db, env);

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

    [HttpGet("create")]
    public async Task<IActionResult> CreateForm()
    {
        var tags = await db.tags.ToListAsync();
        return View("create_form", new CreateFormData{ tags = tags });
    }

    [HttpPost("create")]
    public async Task<IActionResult> Create([FromForm] Lot.CreateRequest req)
    {
        var user = HttpContext.GetUser()!;
        var (lot, errors) = await model.CreateLot(req, user.login);

        if (errors.Count > 0) {
            var tags = await db.tags.ToListAsync();
            var form_data = new CreateFormData { tags = tags };
            ViewData["errors"] = errors;
            return View("create_form", form_data);
        }

        return Redirect("/user");
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteByid(uint id)
    {
        var (deleted_lot, err) = await model.DeleteById(id);
        if (err != ModelError.None || deleted_lot == null) {
            Response.StatusCode = 400;
            return Content(err.GetMessage());
        }
        return Ok();
    }

    // [HttpPatch("{id}")]
    // public async Task<IActionResult> UpdateById(uint id, [FromForm] Lot.CreateRequest req)
    // {
    //     var (updated_lot, err) = await model.UpdateById(id, req);
    //     if (err != ModelError.None || updated_lot == null) {
    //         Response.StatusCode = 400;
    //         return Content(err.GetMessage());
    //     }
    //     return Ok(updated_lot);
    // }

}
