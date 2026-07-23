using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using App.Models;
using App.Helpers;
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
        var user = HttpContext.GetUser();

        var lot = await model.GetById(id);
        if (lot == null) {
            return NotFound();
        }

        var data = await model.GetLotDetailsDataAsync(lot, user);

        return View("lot_details", data);
    }

    [HttpPost("{id}/bid")]
    [Authorize, GetUserStrict]
    public async Task<IActionResult> MakeBid([FromRoute] uint id, [FromForm] decimal new_price)
    {
        var user = HttpContext.GetUser()!;
        var (lot, error) = await model.MakeBidAsync(id, new_price, user.login);

        if (lot == null) {
            return NotFound();
        }

        var data = await model.GetLotDetailsDataAsync(lot, user);

        if (error != LotsModel.Error.None) {
            ViewData.SetResults(ViewMessage.Error(LotsModel.ErrorToString(error)));
        } else {
            data.price_changed = true;
        }

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

        var data = await model.GetLotDetailsDataAsync(lot, user);

        if (error != LotsModel.Error.None) {
            ViewData.SetResults(ViewMessage.Error(LotsModel.ErrorToString(error)));
        } else {
            data.price_changed = true;
        }

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
            ViewData.SetResults(ViewMessage.MapCollectionError(errors, LotsModel.ErrorToString));
            return View("create_form", form_data);
        }

        return Redirect("/user");
    }

    [HttpDelete("{lot_id}")]
    public async Task<IActionResult> DeleteByid(uint lot_id)
    {
        var (deleted_lot, err) = await model.DeleteById(lot_id);
        if (err != LotsModel.Error.None || deleted_lot == null) {
            Response.StatusCode = 400;
            return Content(LotsModel.ErrorToString(err));
        }
        return Ok();
    }

    [HttpPatch("{lot_id}")]
    public async Task<IActionResult> UpdateByid([FromRoute] uint lot_id, [FromForm] Lot.EditRequest req)
    {
        var (updated_lot, errors) = await model.UpdateById(lot_id, req);
        if (errors.Count > 0 || updated_lot == null) {
            ViewData.SetResults(ViewMessage.MapCollectionError(errors, LotsModel.ErrorToString));
            return View("results");
        }
        ViewData.SetResults(ViewMessage.Good("Лот успешно обновлен!"));
        return View("results");
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

    [HttpGet("{lot_id}/edit")]
    public async Task<IActionResult> EditForm([FromRoute] uint lot_id)
    {
        var lot = await model.GetById(lot_id);
        if (lot == null) {
            return NotFound();
        }
        var tags = await db.tags.ToListAsync();

        var data = new EditFormData {
            tags = tags,
            current_lot = lot,
        };

        return View("edit_form", data);
    }
}
