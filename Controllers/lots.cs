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
        var seller = lot.user;
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
    public async Task<IActionResult> MakeBid([FromRoute] uint id, [FromForm] decimal new_price)
    {
        var user = HttpContext.GetUser()!;
        var (lot, error) = await model.MakeBid(id, new_price, user.login);

        if (lot == null) {
            return NotFound();
        }

        var data = new LotDetailsViewModel {
            lot = lot,
            seller = lot.user,
            images = lot.images.ToList(),
            is_owner = user?.login == lot.user_login,
        };

        if (error != null) {
            ViewData["errors"] = error;
            return View("bid_maker", data);
        }

        data.price_changed = true;

        return View("bid_maker", data);
    }

    [HttpDelete("{lot_id}/bid")]
    public async Task<IActionResult> CancelLastBid([FromRoute] uint lot_id)
    {
        var user = HttpContext.GetUser()!;
        var (lot, error) = await model.CancelLastBid(lot_id, user.login);

        if (lot == null) {
            return NotFound();
        }

        var data = new LotDetailsViewModel {
            lot = lot,
            seller = lot.user,
            images = lot.images.ToList(),
            is_owner = user?.login == lot.user_login,
        };

        if (error != null || lot == null) {
            ViewData["errors"] = error;
            return View("bid_maker", data);
        }

        data.price_changed = true;

        return View("bid_maker", data);
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

    [HttpGet("{lot_id}/purchase")]
    public async Task<IActionResult> PurchaseForm([FromRoute] uint lot_id)
    {
        var lot = await model.GetById(lot_id);
        if (lot == null) {
            return NotFound();
        }
        return View("purchase_form", lot);
    }

    [HttpPost("{lot_id}/purchase-submit")]
    public async Task<IActionResult> PurchaseSubmit([FromRoute] uint lot_id)
    {
        var user = HttpContext.GetUser()!;
        var lot = await model.GetById(lot_id);
        if (lot == null) {
            return NotFound();
        }

        var purchase = new Purchase {
            user_login = user.login,
            lot_id = lot_id,
            locked_price = lot.current_price,
        };
        db.purchases.Add(purchase);

        lot.status = LotStatus.Purchased.ToString();

        if (!await db.TrySaveChangesAsync()) {
            ViewData["errors"] = "Ошибка сохранения.";
            return View("purchase_form", lot);
        }
        return Redirect("/user");
    }

    [HttpPost("{lot_id}/purchase-deny")]
    public async Task<IActionResult> PurchaseDeny([FromRoute] uint lot_id)
    {
        var lot = await model.GetById(lot_id);
        if (lot == null) {
            return NotFound();
        }

        lot.status = LotStatus.Denied.ToString();

        if (!await db.TrySaveChangesAsync()) {
            ViewData["errors"] = "Ошибка сохранения.";
            return View("purchase_form", lot);
        }

        return Redirect("/user");
    }
}
